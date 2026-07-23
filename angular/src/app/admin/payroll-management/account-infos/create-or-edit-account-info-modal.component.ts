import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    AccountInfosServiceProxy,
    AccountTypesServiceProxy,
    CreateOrEditAccountInfoDto,
    GetAllSimpleLookupsInput,
    SimpleLookupDto,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditAccountInfoModal',
    templateUrl: './create-or-edit-account-info-modal.component.html',
    imports: [
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditAccountInfoModalComponent extends AppComponentBase {
    private _accountInfosService = inject(AccountInfosServiceProxy);
    private _accountTypesService = inject(AccountTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly accountInfo = signal<CreateOrEditAccountInfoDto>(new CreateOrEditAccountInfoDto());
    readonly accountTypes = signal<SimpleLookupDto[]>([]);

    show(accountInfoId?: number): void {
        this.active.set(true);

        this._accountTypesService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.accountTypes.set(result.items));

        if (!accountInfoId) {
            this.accountInfo.set(new CreateOrEditAccountInfoDto());
            this.modal().show();
        } else {
            this._accountInfosService.getAccountInfoForEdit(accountInfoId).subscribe((result) => {
                this.accountInfo.set(result.accountInfo);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('AccountInfo_NameEn')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._accountInfosService
            .createOrEdit(this.accountInfo())
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe(() => {
                this.notify.info(this.l('SavedSuccessfully'));
                this.close();
                this.modalSave.emit();
            });
    }

    close(): void {
        this.active.set(false);
        this.modal().hide();
    }

    updateField(field: keyof CreateOrEditAccountInfoDto, value: any): void {
        const current = this.accountInfo();
        (current as any)[field] = value;
        const next = new CreateOrEditAccountInfoDto();
        Object.assign(next, current);
        this.accountInfo.set(next);
    }
}
