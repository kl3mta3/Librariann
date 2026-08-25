export interface InviteRequest {
  id: number;
  email: string;
  name: string | null;
  requestedUtc: string;
}
