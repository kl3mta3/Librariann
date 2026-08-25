import {ChangeDetectionStrategy, Component, inject, OnInit, signal} from '@angular/core';
import {NgbActiveModal, NgbTooltip} from '@ng-bootstrap/ng-bootstrap';
import {translate, TranslocoDirective} from "@jsverse/transloco";
import {ToastrService} from 'ngx-toastr';
import {AccountService} from 'src/app/_services/account.service';
import {SettingsService} from '../../settings.service';
import {ConfirmService} from 'src/app/shared/confirm.service';
import {InviteRequest} from 'src/app/_models/auth/invite-request';
import {UtcToLocalTimePipe} from "../../../_pipes/utc-to-local-time.pipe";
import {LoadingComponent} from "../../../shared/loading/loading.component";
import {EmptyStateComponent} from "../../../shared/_components/empty-state/empty-state.component";

@Component({
  selector: 'app-pending-invites-modal',
  templateUrl: './pending-invites-modal.component.html',
  styleUrls: ['./pending-invites-modal.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslocoDirective, NgbTooltip, UtcToLocalTimePipe, LoadingComponent, EmptyStateComponent]
})
export class PendingInvitesModalComponent implements OnInit {

  protected readonly modal = inject(NgbActiveModal);
  private readonly accountService = inject(AccountService);
  private readonly settingsService = inject(SettingsService);
  private readonly toastr = inject(ToastrService);
  private readonly confirmService = inject(ConfirmService);

  requests = signal<InviteRequest[]>([]);
  isLoading = signal(true);
  isEmailSetup = signal(false);
  /** Tracks which row (by id) has an approve call in flight, to disable just that row's button. */
  processingId = signal<number | null>(null);
  isInvitingAll = signal(false);
  /** Any change here (approved/invited) is reported back to the caller so it can refresh its own badge/list. */
  changed = false;

  ngOnInit(): void {
    this.settingsService.isEmailSetup().subscribe(isSetup => this.isEmailSetup.set(isSetup));
    this.load();
  }

  private load() {
    this.isLoading.set(true);
    this.accountService.getPendingInvites().subscribe(requests => {
      this.requests.set(requests);
      this.isLoading.set(false);
    });
  }

  close() {
    this.modal.close(this.changed);
  }

  approve(request: InviteRequest) {
    this.processingId.set(request.id);
    this.accountService.approveInviteRequest(request.id).subscribe({
      next: async (response) => {
        this.processingId.set(null);
        this.requests.update(r => r.filter(x => x.id !== request.id));
        this.changed = true;

        if (response.emailSent) {
          this.toastr.info(translate('toasts.email-sent', {email: request.email}));
          return;
        }

        // Requester's own name is display-only from the request - fall back to their email if they left it blank.
        const name = request.name || request.email;
        const params = {name, email: request.email};

        await this.confirmService.alert(
          '<strong>' + translate('pending-invites-modal.email-not-setup-title') + '</strong><br/><br/>' +
          translate('pending-invites-modal.email-not-setup-instructions', params) +
          '<br/><a href="' + response.emailLink + '" target="_blank" rel="noopener noreferrer">' + response.emailLink + '</a>' +
          '<br/><br/>' + translate('pending-invites-modal.email-not-setup-note', params));
      },
      error: () => this.processingId.set(null)
    });
  }

  inviteAll() {
    this.isInvitingAll.set(true);
    this.accountService.inviteAllPending().subscribe({
      next: (count) => {
        this.isInvitingAll.set(false);
        this.changed = true;
        this.toastr.info(translate('pending-invites-modal.invited-all-success', {count}));
        this.load();
      },
      error: () => this.isInvitingAll.set(false)
    });
  }
}
