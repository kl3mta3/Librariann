import {ChangeDetectionStrategy, Component, computed, DestroyRef, effect, inject, signal} from '@angular/core';
import {NavigationEnd, Router} from '@angular/router';
import {filter, tap} from 'rxjs/operators';
import {ImageService} from 'src/app/_services/image.service';
import {EVENTS, MessageHubService} from 'src/app/_services/message-hub.service';
import {UtilityService} from '../../../shared/_services/utility.service';
import {Library, LibraryType} from '../../../_models/library/library';
import {AccountService} from '../../../_services/account.service';
import {ActionFactoryService} from '../../../_services/action-factory.service';
import {NavService} from '../../../_services/nav.service';
import {takeUntilDestroyed, toSignal} from "@angular/core/rxjs-interop";
import {merge, Observable, of, ReplaySubject, switchMap} from "rxjs";
import {APP_BASE_HREF} from "@angular/common";
import {SideNavItemComponent} from "../side-nav-item/side-nav-item.component";
import {TranslocoDirective} from "@jsverse/transloco";
import {CardActionablesComponent} from "../../../_single-module/card-actionables/card-actionables.component";
import {SideNavStream} from "../../../_models/sidenav/sidenav-stream";
import {SideNavStreamType} from "../../../_models/sidenav/sidenav-stream-type.enum";
import {SettingsTabId} from "../../preference-nav/preference-nav.component";
import {KeyBindService} from "../../../_services/key-bind.service";
import {KeyBindTarget} from "../../../_models/preferences/preferences";
import {BreakpointService} from "../../../_services/breakpoint.service";
import {ActionItem} from "../../../_models/actionables/action-item";
import {Action} from "../../../_models/actionables/action";
import {ActionResult} from "../../../_models/actionables/action-result";
import {FilterUtilitiesService} from "../../../shared/_services/filter-utilities.service";
import {SideNavGroupComponent} from "../side-nav-group/side-nav-group.component";

@Component({
  selector: 'app-side-nav',
  imports: [SideNavItemComponent, CardActionablesComponent, TranslocoDirective, SideNavGroupComponent],
  templateUrl: './side-nav.component.html',
  styleUrls: ['./side-nav.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SideNavComponent {
  private readonly router = inject(Router);
  protected readonly utilityService = inject(UtilityService);
  private readonly messageHub = inject(MessageHubService);
  protected readonly navService = inject(NavService);
  private readonly imageService = inject(ImageService);
  protected readonly accountService = inject(AccountService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly actionFactoryService = inject(ActionFactoryService);
  private readonly keyBindService = inject(KeyBindService);
  protected readonly breakpointService = inject(BreakpointService);
  private readonly baseUrl = inject(APP_BASE_HREF);


  cachedData: SideNavStream[] | null = null;
  actions: ActionItem<Library>[] = this.actionFactoryService.getLibraryActions();
  homeActions: ActionItem<{}>[] = this.actionFactoryService.getSideNavHomeActions();
  readingListActions: ActionItem<{}>[] = this.actionFactoryService.getSideNavReadingListActions();

  getExternalSourceLink(stream: SideNavStream): string {
    const source = stream.externalSource;
    if (!source) return '';
    return source.launchApiPath
      ? this.baseUrl.replace(/\/?$/, '/') + source.launchApiPath.replace(/^\//, '')
      : source.host;
  }

  private loadDataSubject = new ReplaySubject<void>();
  loadData$ = this.loadDataSubject.asObservable();

  loadDataOnInit$: Observable<SideNavStream[]> = this.loadData$.pipe(
    switchMap(() => {
      if (this.cachedData != null) {
        return of(this.cachedData);
      }
      return this.navService.getSideNavStreams().pipe(
        tap(data => {
          this.cachedData = data; // Cache the data after initial load
        })
      );
    })
  );

  navStreams$: Observable<SideNavStream[]> = merge(
    this.loadDataOnInit$,
    this.messageHub.messages$.pipe(
      filter(event => event.event === EVENTS.LibraryModified || event.event === EVENTS.SideNavUpdate),
      tap(() => {
        this.cachedData = null; // Reset cached data to null to get latest
      }),
      switchMap(() => this.loadDataOnInit$),
      takeUntilDestroyed(this.destroyRef),
    )
  ).pipe(
    takeUntilDestroyed(this.destroyRef),
  );

  private readonly navStreamsSignal = toSignal<SideNavStream[] | null>(this.navStreams$, {initialValue: null});

  protected readonly groupedStreams = computed(() => {
    const streams = this.navStreamsSignal();
    if (!streams) return null;

    return {
      libraries: streams.filter(s => s.streamType === SideNavStreamType.Library),
      browse: streams.filter(s =>
        s.streamType === SideNavStreamType.AllSeries || s.streamType === SideNavStreamType.BrowsePeople),
      smartFilters: streams.filter(s => s.streamType === SideNavStreamType.SmartFilter),
      collections: streams.filter(s => [
        SideNavStreamType.Collections,
        SideNavStreamType.ReadingLists,
        SideNavStreamType.Bookmarks,
        SideNavStreamType.WantToRead,
      ].includes(s.streamType)),
      externalSources: streams.filter(s => s.streamType === SideNavStreamType.ExternalSource),
    };
  });

  protected readonly showServerToolsGroup = computed(() =>
    this.accountService.canSearchIndexers() ||
    this.accountService.canManageAcquisition() ||
    this.accountService.canManageMetadata() ||
    (this.groupedStreams()?.externalSources.length ?? 0) > 0
  );

  protected readonly serverToolsOpen = this.createGroupOpenSignal('server-tools', false);
  protected readonly librariesOpen = this.createGroupOpenSignal('libraries', true);
  protected readonly browseOpen = this.createGroupOpenSignal('browse', true);
  protected readonly smartFiltersOpen = this.createGroupOpenSignal('smart-filters', false);
  protected readonly collectionsOpen = this.createGroupOpenSignal('collections', true);

  collapseSideNavOnMobileNav$ = this.router.events.pipe(
    filter(event => event instanceof NavigationEnd),
    takeUntilDestroyed(this.destroyRef),
    filter(() => this.breakpointService.isMobile() && this.navService.sideNavCollapsedSignal()),
    filter(collapsed => !collapsed)
  );


  constructor() {
    // Ensure that on mobile, we are collapsed by default
    if (this.breakpointService.isMobile()) {
      this.navService.collapseSideNav(true);
    }

    this.collapseSideNavOnMobileNav$.subscribe(() => {
      this.navService.collapseSideNav(false);
    });

    this.keyBindService.registerListener(
      this.destroyRef,
      (e) => this.router.navigate(['/settings'], { fragment: SettingsTabId.Account}),
      [KeyBindTarget.NavigateToSettings],
      {condition$: this.navService.sideNavVisibility$},
    );

    effect(() => {
      const user = this.accountService.currentUser();
      if (!user) return;
      this.loadDataSubject.next();
    })

  }

  private createGroupOpenSignal(groupId: string, defaultValue: boolean) {
    const isOpen = signal(this.navService.isSideNavGroupExpanded(groupId, defaultValue));
    effect(() => this.navService.setSideNavGroupExpanded(groupId, isOpen()));
    return isOpen;
  }

  performHomeAction(event: ActionItem<{}> | ActionResult<{}>) {
    if (event.action === Action.Edit) {
      this.router.navigate(['/settings'], { fragment: SettingsTabId.Customize });
    }
  }
  performReadingListAction(event: ActionItem<{}> | ActionResult<{}>) {
    if (event.action === Action.Navigate) {
      this.router.navigateByUrl('/settings#cbl-import');
      return;
    }
  }

  getLibraryTypeIcon(format: LibraryType) {
    switch (format) {
      case LibraryType.Book:
      case LibraryType.LightNovel:
        return 'fa-book';
      case LibraryType.Comic:
      case LibraryType.ComicVine:
      case LibraryType.Manga:
        return 'fa-book-open';
      case LibraryType.Images:
        return 'fa-images';
      case LibraryType.Audiobook:
        return 'fa-headphones';
    }
  }

  getLibraryImage(library: Library) {
    if (library.coverImage) return this.imageService.getLibraryCoverImage(library.id);
    return null;
  }


  toggleNavBar() {
    this.navService.toggleSideNav();
  }

  getSmartFilterBaseLink(item: SideNavStream) {
    return '/' + FilterUtilitiesService.getFilterLink(item.entityType, '');
  }

  protected readonly SettingsTabId = SettingsTabId;
  protected readonly SideNavStreamType = SideNavStreamType;
}
