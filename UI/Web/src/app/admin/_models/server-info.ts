export interface ServerInfoSlim {
  librariannVersion: string;
  installId: string;
  isDocker: boolean;
  firstInstallVersion?: string;
  firstInstallDate?: string;
}
