import {
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  effect,
  HostListener,
  inject,
  OnInit,
  signal
} from '@angular/core';
import {NavigationStart, Router, RouterOutlet} from '@angular/router';
import {shareReplay, take} from 'rxjs/operators';
import {AccountService} from './_services/account.service';
import {LibraryService} from './_services/library.service';
import {NavService} from './_services/nav.service';
import {NgbModal, NgbOffcanvas, NgbOffcanvasConfig} from '@ng-bootstrap/ng-bootstrap';
import {AsyncPipe, DOCUMENT, NgClass} from '@angular/common';
import {filter} from 'rxjs';
import {ThemeService} from "./_services/theme.service";
import {SideNavComponent} from './sidenav/_components/side-nav/side-nav.component';
import {NavHeaderComponent} from "./nav/_components/nav-header/nav-header.component";
import {MiniPlayerComponent} from "./audiobook-reader/_components/mini-player/mini-player.component";
import {takeUntilDestroyed} from "@angular/core/rxjs-interop";
import {ServerService} from "./_services/server.service";
import {PreferenceNavComponent} from "./sidenav/preference-nav/preference-nav.component";
import {UtilityService} from "./shared/_services/utility.service";
import {TranslocoService} from "@jsverse/transloco";
import {LocalizationService} from "./_services/localization.service";
import {BreakpointService} from "./_services/breakpoint.service";
import {KeyBindService} from "./_services/key-bind.service";
import {KeyBindTarget} from "./_models/preferences/preferences";

@Component({
    selector: 'app-root',
    templateUrl: './app.component.html',
    styleUrls: ['./app.component.scss'],
    imports: [NgClass, SideNavComponent, RouterOutlet, AsyncPipe, NavHeaderComponent, PreferenceNavComponent, MiniPlayerComponent],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class AppComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly offcanvas = inject(NgbOffcanvas);
  protected readonly navService = inject(NavService);
  protected readonly utilityService = inject(UtilityService);
  protected readonly serverService = inject(ServerService);
  protected readonly accountService = inject(AccountService);
  private readonly libraryService = inject(LibraryService);
  private readonly ngbModal = inject(NgbModal);
  private readonly router = inject(Router);
  private readonly themeService = inject(ThemeService);
  private readonly document = inject(DOCUMENT);
  private readonly translocoService = inject(TranslocoService);
  private readonly breakpointService = inject(BreakpointService); // Needs to be injected to run background job
  private readonly localizationService = inject(LocalizationService);
  private readonly ngbCanvasConfig = inject(NgbOffcanvasConfig);
  private readonly keyBindService = inject(KeyBindService);

  transitionState = computed(() => this.accountService.userPreferences()?.noTransitions ?? false);
  protected readonly embeddedMode = signal(false);
  /**
   * Whether an /embed caller asked to keep Librariann's own chrome - side nav AND top header (?nav=1)
   * - rather than getting the default fully chrome-less single-page view. Needed by embedders that
   * want the user to actually navigate and search within the embedded panel (e.g. a docked panel with
   * real screen space to spare - the header carries the hamburger/collapse toggle, search bar, and the
   * top-right action buttons), as opposed to a bare widget embed that only ever shows one page.
   */
  protected readonly embeddedNavVisible = signal(false);


  constructor() {
    const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)');

    effect(() => {
      this.applyAnimationState(this.transitionState(), reducedMotion.matches);
    });

    reducedMotion.addEventListener('change', () => {
      this.applyAnimationState(this.transitionState(), reducedMotion.matches);
    });

    // Close any open modals when a route change occurs
    this.router.events
      .pipe(
          filter(event => event instanceof NavigationStart),
          takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(async (event) => {

        const navigation = event as NavigationStart;
        const navigationPath = navigation.url.split(/[?#]/, 1)[0];
        if (navigationPath === '/embed' || navigationPath.startsWith('/embed/')) {
          this.embeddedMode.set(true);
          this.document.documentElement.classList.add('librariann-embed');

          const queryString = navigation.url.includes('?') ? navigation.url.split('?')[1].split('#')[0] : '';
          const params = new URLSearchParams(queryString);
          this.embeddedNavVisible.set(params.get('nav') === '1' || params.get('nav') === 'true');
        }

        if (!this.ngbModal.hasOpenModals() && !this.offcanvas.hasOpenOffcanvas()) return;

        if (this.ngbModal.hasOpenModals()) {
          this.ngbModal.dismissAll();
        }

        if (this.offcanvas.hasOpenOffcanvas()) {
          this.offcanvas.dismiss();
        }

        if ((event as any).navigationTrigger === 'popstate') {
          const currentRoute = this.router.routerState;
          await this.router.navigateByUrl(currentRoute.snapshot.url, { skipLocationChange: true });
        }
      });

    this.localizationService.getLocales().subscribe(); // This will cache the localizations on startup

    this.keyBindService.registerListener(
      this.destroyRef,
      (_)=> this.router.navigateByUrl('/').catch(console.error),
      [KeyBindTarget.NavigateHome]
    );
  }

  @HostListener('window:resize', [])
  @HostListener('window:orientationchange', [])
  setDocHeight() {
    // Sets a CSS variable for the actual device viewport height. Needed for mobile dev.
    const vh = window.innerHeight * 0.01;
    this.document.documentElement.style.setProperty('--vh', `${vh}px`);
  }

  ngOnInit(): void {
    this.setDocHeight();
    this.setCurrentUser();
    this.themeService.setColorScape('');
  }


  private applyAnimationState(userDisabled: boolean, reducedMotion: boolean) {
    const shouldDisable = userDisabled || reducedMotion;
    this.ngbCanvasConfig.animation = !shouldDisable;

    if (shouldDisable) {
      document.documentElement.classList.add('animate-disabled');
    } else {
      document.documentElement.classList.remove('animate-disabled');
    }
  }

  setCurrentUser() {
    const user = this.accountService.currentUser();
    if (!user) return;

    // Bootstrap anything that's needed
    this.libraryService.getLibraryNames().pipe(take(1), shareReplay({refCount: true, bufferSize: 1})).subscribe();
  }
}
