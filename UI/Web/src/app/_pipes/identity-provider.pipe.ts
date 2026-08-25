import {Pipe, PipeTransform} from '@angular/core';
import {IdentityProvider} from "../_models/user/user";
import {translate} from "@jsverse/transloco";

@Pipe({
  name: 'identityProviderPipe'
})
export class IdentityProviderPipePipe implements PipeTransform {

  transform(value: IdentityProvider): string {
    switch (value) {
      case IdentityProvider.Librariann:
        return translate("identity-provider-pipe.librariann");
      case IdentityProvider.OpenIdConnect:
        return translate("identity-provider-pipe.oidc");
    }
  }

}
