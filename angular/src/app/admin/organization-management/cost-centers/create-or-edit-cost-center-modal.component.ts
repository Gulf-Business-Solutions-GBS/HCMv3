import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CostCenterGroupsServiceProxy,
    CostCentersServiceProxy,
    CreateOrEditCostCenterDto,
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
    selector: 'createOrEditCostCenterModal',
    templateUrl: './create-or-edit-cost-center-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditCostCenterModalComponent extends AppComponentBase {
    private _costCentersService = inject(CostCentersServiceProxy);
    private _costCenterGroupsService = inject(CostCenterGroupsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly costCenter = signal<CreateOrEditCostCenterDto>(new CreateOrEditCostCenterDto());
    readonly costCenterGroups = signal<SimpleLookupDto[]>([]);

    show(costCenterId?: number): void {
        this.active.set(true);

        this._costCenterGroupsService
            .getAll(
                new GetAllSimpleLookupsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => {
                this.costCenterGroups.set(result.items);
            });

        if (!costCenterId) {
            this.costCenter.set(new CreateOrEditCostCenterDto());
            this.modal().show();
        } else {
            this._costCentersService.getCostCenterForEdit(costCenterId).subscribe((result) => {
                this.costCenter.set(result.costCenter);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('CostCenter_NameEn')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._costCentersService
            .createOrEdit(this.costCenter())
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

    updateField(field: keyof CreateOrEditCostCenterDto, value: any): void {
        const current = this.costCenter();
        (current as any)[field] = value;
        const next = new CreateOrEditCostCenterDto();
        Object.assign(next, current);
        this.costCenter.set(next);
    }
}
