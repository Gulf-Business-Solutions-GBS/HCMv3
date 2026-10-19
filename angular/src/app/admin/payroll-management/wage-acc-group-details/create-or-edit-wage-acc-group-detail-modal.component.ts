import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    AccountInfoDto,
    AccountInfosServiceProxy,
    CreateOrEditWageAccGroupDetailDto,
    GetAllAccountInfosInput,
    GetAllSimpleLookupsInput,
    GetAllWageTypesInput,
    OffsetAccountInfoType,
    SimpleLookupDto,
    WageAccGroupDetailsServiceProxy,
    WageAccGroupsServiceProxy,
    WageTypeDto,
    WageTypesServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { CostElementSlotFieldsComponent } from '@app/shared/common/cost-element-slots/cost-element-slot-fields.component';

@Component({
    selector: 'createOrEditWageAccGroupDetailModal',
    templateUrl: './create-or-edit-wage-acc-group-detail-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe, CostElementSlotFieldsComponent],
})
export class CreateOrEditWageAccGroupDetailModalComponent extends AppComponentBase {
    private _wageAccGroupDetailsService = inject(WageAccGroupDetailsServiceProxy);
    private _wageAccGroupsService = inject(WageAccGroupsServiceProxy);
    private _wageTypesService = inject(WageTypesServiceProxy);
    private _accountInfosService = inject(AccountInfosServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly wageAccGroupDetail = signal<CreateOrEditWageAccGroupDetailDto>(new CreateOrEditWageAccGroupDetailDto());

    readonly wageAccGroups = signal<SimpleLookupDto[]>([]);
    readonly wageTypes = signal<WageTypeDto[]>([]);
    readonly accountInfos = signal<AccountInfoDto[]>([]);

    readonly offsetAccountInfoTypes = [
        { value: OffsetAccountInfoType.Fixed, name: 'Fixed' },
        { value: OffsetAccountInfoType.EmployeeCode, name: 'EmployeeCode' },
    ];

    show(wageAccGroupDetailId?: number): void {
        this.active.set(true);

        this.loadLookups();

        if (!wageAccGroupDetailId) {
            this.wageAccGroupDetail.set(new CreateOrEditWageAccGroupDetailDto());
            this.modal().show();
        } else {
            this._wageAccGroupDetailsService.getWageAccGroupDetailForEdit(wageAccGroupDetailId).subscribe((result) => {
                this.wageAccGroupDetail.set(result.wageAccGroupDetail);
                this.modal().show();
            });
        }
    }

    private loadLookups(): void {
        this._wageAccGroupsService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.wageAccGroups.set(result.items));

        this._wageTypesService
            .getAll(new GetAllWageTypesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.wageTypes.set(result.items));

        this._accountInfosService
            .getAll(new GetAllAccountInfosInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.accountInfos.set(result.items));
    }

    onShown(): void {
        document.getElementById('WageAccGroupDetail_WageAccGroupId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._wageAccGroupDetailsService
            .createOrEdit(this.wageAccGroupDetail())
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

    updateField(field: keyof CreateOrEditWageAccGroupDetailDto, value: any): void {
        const current = this.wageAccGroupDetail();
        (current as any)[field] = value;
        const next = new CreateOrEditWageAccGroupDetailDto();
        Object.assign(next, current);
        this.wageAccGroupDetail.set(next);
    }
}
