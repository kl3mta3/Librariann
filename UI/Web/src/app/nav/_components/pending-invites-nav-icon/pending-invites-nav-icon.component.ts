import {ChangeDetectionStrategy, Component, inject, OnInit, signal} from '@angular/core';
import {RouterLink} from '@angular/router';
import {TranslocoDirective} from "@jsverse/transloco";
import {AccountService} from 'src/app/_services/account.service';
import {SettingsTabId} from '../../../sidenav/preference-nav/preference-nav.component';

/**
 * Admin-only. A user icon linking to Settings > Users, with a yellow dot when there is at least one pending
 * invite request awaiting review. Always navigates there, badge or not.
 */
@Component({
  selector: 'app-pending-invites-nav-icon',
  templateUrl: './pending-invites-nav-icon.component.html',
  styleUrls: ['./pending-invites-nav-icon.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, TranslocoDirective]
})
export class PendingInvitesNavIconComponent implements OnInit {

  private readonly accountService = inject(AccountService);

  pendingCount = signal(0);

  ngOnInit(): void {
    this.accountService.getPendingInvites().subscribe(requests => this.pendingCount.set(requests.length));
  }

  protected readonly SettingsTabId = SettingsTabId;
}
