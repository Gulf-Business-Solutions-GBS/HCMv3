import { Component, input } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { UpdateGoogleAuthenticatorKeyOutput } from '@shared/service-proxies/service-proxies';

import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'recoveryCodesComponent',
    templateUrl: './recovery-codes.component.html',
    imports: [LocalizePipe],
})
export class RecoveryCodesComponent extends AppComponentBase {
    readonly model = input<UpdateGoogleAuthenticatorKeyOutput>(new UpdateGoogleAuthenticatorKeyOutput());
}
