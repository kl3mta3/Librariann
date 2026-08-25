import {AgeRestriction} from '../metadata/age-restriction';

export interface DefaultInvitePermissions {
  roles: Array<string>;
  libraries: Array<number>;
  ageRestriction: AgeRestriction;
}
