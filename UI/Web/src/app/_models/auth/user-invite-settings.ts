import {DefaultInvitePermissions} from './default-invite-permissions';

export interface UserInviteSettings {
  showRequestInviteLink: boolean;
  autoAcceptInviteRequests: boolean;
  defaultInvitePermissions: DefaultInvitePermissions;
}
