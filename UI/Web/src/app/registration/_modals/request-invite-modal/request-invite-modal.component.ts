import {ChangeDetectionStrategy, ChangeDetectorRef, Component, inject} from '@angular/core';
import {FormControl, FormGroup, ReactiveFormsModule, Validators} from '@angular/forms';
import {NgbActiveModal} from '@ng-bootstrap/ng-bootstrap';
import {TranslocoDirective} from "@jsverse/transloco";
import {AccountService} from 'src/app/_services/account.service';

@Component({
  selector: 'app-request-invite-modal',
  templateUrl: './request-invite-modal.component.html',
  styleUrls: ['./request-invite-modal.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, TranslocoDirective]
})
export class RequestInviteModalComponent {

  protected readonly modal = inject(NgbActiveModal);
  private readonly accountService = inject(AccountService);
  private readonly cdRef = inject(ChangeDetectorRef);

  isSending = false;
  submitted = false;
  autoAccepted = false;

  requestForm: FormGroup = new FormGroup({
    email: new FormControl<string>('', [Validators.required, Validators.email]),
    name: new FormControl<string>('', [Validators.required]),
  });

  close() {
    this.modal.close();
  }

  submit() {
    if (!this.requestForm.valid) return;

    this.isSending = true;
    this.accountService.requestInvite(this.requestForm.getRawValue()).subscribe({
      next: (response) => {
        this.isSending = false;
        this.submitted = true;
        this.autoAccepted = response.autoAccepted;
        this.cdRef.markForCheck();
      },
      error: () => {
        this.isSending = false;
        this.cdRef.markForCheck();
      }
    });
  }
}
