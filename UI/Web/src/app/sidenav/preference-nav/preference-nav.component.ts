import {
  AfterViewInit,
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  DestroyRef,
  effect,
  inject,
  Signal,
  untracked
} from '@angular/core';
import {TranslocoDirective} from "@jsverse/transloco";
import {DOCUMENT, NgClass} from "@angular/common";
import {NavService} from "../../_services/nav.service";
import {AccountService, Role} from "../../_services/account.service";
import {SideNavItemComponent} from "../_components/side-nav-item/side-nav-item.component";
import {ActivatedRoute, NavigationEnd, Router} from "@angular/router";
import {takeUntilDestroyed, toObservable, toSignal} from "@angular/core/rxjs-interop";
import {SettingFragmentPipe} from "../../_pipes/setting-fragment.pipe";
import {map, of, shareReplay, switchMap, take} from "rxjs";
import {ServerService} from "../../_services/server.service";
import {User} from "../../_models/user/user";
import {filter} from "rxjs/operators";
import {BreakpointService} from "../../_services/breakpoint.service";

export enum SettingsTabId {

  // Admin
  Activity = 'admin-activity',
  General = 'admin-general',
  OpenIDConnect = 'admin-oidc',
  Email = 'admin-email',
  Media = 'admin-media',
  Tts = 'admin-tts',
  Users = 'admin-users',
  Libraries = 'admin-libraries',
  System = 'admin-system',
  PlexPatch = 'admin-plex-patch',
  Tasks = 'admin-tasks',
  Statistics = 'admin-statistics',
  MediaIssues = 'admin-media-issues',
  EmailHistory = 'admin-email-history',
  ManageMetadata = 'admin-public-metadata',
  AdminDevices = 'admin-device',
  Scrobbling = 'scrobbling',
  Acquisition = 'admin-acquisition',

  // Retained temporarily for compilation of isolated legacy components.
  // These routes are not exposed by Librariann's settings navigation.
  LibrariannPlusLicense = 'admin-librariannplus',
  ManageLibrariannPlusActivity = 'admin-manage-librariannplus-activity',

  MALStackImport = 'mal-stack-import',
  MappingsImport = 'admin-mappings-import',
  MatchedMetadata = 'admin-matched-metadata',
  ManageUserTokens = 'admin-manage-tokens',
  Metadata = 'admin-metadata',

  // Non-Admin
  Account = 'account',
  Preferences = 'preferences',
  Connections = 'scrobble-settings',
  CustomKeyBinds = 'custom-key-binds',
  ReadingProfiles = 'reading-profiles',
  Font = 'font',
  Clients = 'clients',
  Devices = 'devices',
  ScrobblingHolds = 'scrobble-holds',
  MyActivity = 'my-activity',
  Customize = 'customize',
  CBLImport = 'cbl-import',
  RemapRules = 'remap-rules',
}

export enum SettingSectionId {
  AccountSection = 'account-section-title',
  ServerSection = 'server-section-title',
  InsightsSection = 'insights-section-title',
  ImportSection = 'import-section-title',
  InfoSection = 'info-section-title',
}

interface PrefSection {
  title: SettingSectionId;
  children: SideNavItem[];
}

class SideNavItem {
  fragment: SettingsTabId;
  roles: Array<Role> = [];
  /**
   * If you have any of these, the item will be restricted
   */
  restrictRoles: Array<Role> = [];
  badgeCount?: Signal<number> | undefined;
  constructor(fragment: SettingsTabId, roles: Array<Role> = [], badgeCount: Signal<number> | undefined = undefined, restrictRoles: Array<Role> = []) {
    this.fragment = fragment;
    this.roles = roles;
    this.restrictRoles = restrictRoles;
    this.badgeCount = badgeCount;
  }

}

@Component({
    selector: 'app-preference-nav',
    imports: [
        TranslocoDirective,
        NgClass,
        SideNavItemComponent,
        SettingFragmentPipe
    ],
    templateUrl: './preference-nav.component.html',
    styleUrl: './preference-nav.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class PreferenceNavComponent implements AfterViewInit {

  private readonly destroyRef = inject(DestroyRef);
  protected readonly navService = inject(NavService);
  protected readonly accountService = inject(AccountService);
  protected readonly cdRef = inject(ChangeDetectorRef);
  private readonly route = inject(ActivatedRoute);
  private readonly serverService = inject(ServerService);
  private readonly router = inject(Router);
  private readonly document = inject(DOCUMENT);
  protected readonly breakpointService = inject(BreakpointService);

  private readonly navEnd = toSignal(
    this.router.events.pipe(
      filter((event): event is NavigationEnd => event instanceof NavigationEnd)
    )
  );

  /**
   * This links to settings.component.html which has triggers on what underlying component to render out.
   */
  sections: Array<PrefSection> = [];


  private readonly mediaIssuesBadgeCount = toSignal(
    toObservable(this.accountService.hasAdminRole).pipe(
      take(1),
      switchMap(isAdmin => {
        if (!isAdmin) return of(-1);
        return this.serverService.getMediaErrors().pipe(
          takeUntilDestroyed(this.destroyRef),
          map(d => d.length),
          shareReplay({ bufferSize: 1, refCount: true })
        );
      })
    ),
    { initialValue: -1 }
  );

  constructor() {
    effect(() => {
      const navEvent = this.navEnd();
      if (!navEvent) return;

      if (this.breakpointService.isAboveMobile()) return;

      const isCollapsed = untracked(() => this.navService.sideNavCollapsedSignal());
      if (isCollapsed) return;

      this.navService.collapseSideNav(true);
    });

    // Ensure that on mobile, we are collapsed by default
    if (this.breakpointService.isMobileOrBelow()) {
      this.navService.collapseSideNav(true);
    }

    this.sections = [
      {
        title: SettingSectionId.AccountSection,
        children: [
          new SideNavItem(SettingsTabId.Account),
          new SideNavItem(SettingsTabId.Preferences),
          new SideNavItem(SettingsTabId.CustomKeyBinds),
          new SideNavItem(SettingsTabId.ReadingProfiles),
          new SideNavItem(SettingsTabId.Customize, [], undefined, [Role.ReadOnly]),
          new SideNavItem(SettingsTabId.Clients),
          new SideNavItem(SettingsTabId.Font),
          new SideNavItem(SettingsTabId.Devices),
          new SideNavItem(SettingsTabId.RemapRules, [], undefined, [Role.ReadOnly]),
        ]
      },
      {
        title: SettingSectionId.InsightsSection,
        children: [
          new SideNavItem(SettingsTabId.Activity, [Role.Admin]),
          new SideNavItem(SettingsTabId.AdminDevices, [Role.Admin]),
          new SideNavItem(SettingsTabId.Statistics, [Role.Admin]),
        ]
      },
      {
        title: SettingSectionId.ServerSection,
        children: [
          new SideNavItem(SettingsTabId.General, [Role.Admin]),
          new SideNavItem(SettingsTabId.ManageMetadata, [Role.Admin]),
          new SideNavItem(SettingsTabId.Acquisition, [Role.ManageAcquisition, Role.Admin]),
          new SideNavItem(SettingsTabId.OpenIDConnect, [Role.Admin]),
          new SideNavItem(SettingsTabId.Media, [Role.Admin]),
          new SideNavItem(SettingsTabId.Tts, [Role.Admin]),
          new SideNavItem(SettingsTabId.Email, [Role.Admin]),
          new SideNavItem(SettingsTabId.Users, [Role.Admin]),
          new SideNavItem(SettingsTabId.Libraries, [Role.Admin]),
          new SideNavItem(SettingsTabId.Tasks, [Role.Admin]),
        ]
      },
      {
        title: SettingSectionId.ImportSection,
        children: [
          new SideNavItem(SettingsTabId.MappingsImport, [Role.Admin]),
          new SideNavItem(SettingsTabId.CBLImport, [], undefined, [Role.ReadOnly]),
        ]
      },
      {
        title: SettingSectionId.InfoSection,
        children: [
          new SideNavItem(SettingsTabId.System, [Role.Admin]),
          new SideNavItem(SettingsTabId.PlexPatch, [Role.Admin]),
          new SideNavItem(SettingsTabId.MediaIssues, [Role.Admin], this.mediaIssuesBadgeCount),
          new SideNavItem(SettingsTabId.EmailHistory, [Role.Admin]),
        ]
      }
    ];

    this.scrollToActiveItem();
    this.cdRef.markForCheck();

  }

  ngAfterViewInit() {
    this.scrollToActiveItem();
  }

  scrollToActiveItem() {
    const activeFragment = this.route.snapshot.fragment;
    if (activeFragment) {
      const element = this.document.getElementById('nav-item-' + activeFragment);
      if (element) {
        element.scrollIntoView({behavior: 'smooth', block: 'center'});
      }
    }
  }

  getVisibleChildren(user: User, section: PrefSection) {
    return section.children.filter(item => this.isItemVisible(user, item));
  }

  isItemVisible(user: User, item: SideNavItem) {
    return this.accountService.hasAnyRole(user, item.roles, item.restrictRoles)
  }

  collapse() {
    this.navService.toggleSideNav();
  }
}
