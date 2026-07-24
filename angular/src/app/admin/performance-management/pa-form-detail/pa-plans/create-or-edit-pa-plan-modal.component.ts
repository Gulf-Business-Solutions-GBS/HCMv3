import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditPAPlanDto,
    GetAllKpiInfosInput,
    GetAllPARatingsInput,
    KpiInfoDto,
    KpiInfosServiceProxy,
    PARatingDto,
    PARatingsServiceProxy,
    PAPlansServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditPAPlanModal',
    templateUrl: './create-or-edit-pa-plan-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditPAPlanModalComponent extends AppComponentBase {
    private _service = inject(PAPlansServiceProxy);
    private _kpiInfosService = inject(KpiInfosServiceProxy);
    private _paRatingsService = inject(PARatingsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditPAPlanDto>(new CreateOrEditPAPlanDto());
    readonly kpiInfos = signal<KpiInfoDto[]>([]);
    readonly paRatings = signal<PARatingDto[]>([]);

    show(paFormId: number, id?: number): void {
        this.active.set(true);

        this._kpiInfosService
            .getAll(new GetAllKpiInfosInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined, kpiCategoryId: undefined }))
            .subscribe((result) => this.kpiInfos.set(result.items));

        this._paRatingsService
            .getAll(new GetAllPARatingsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.paRatings.set(result.items));

        if (!id) {
            const record = new CreateOrEditPAPlanDto();
            record.paFormId = paFormId;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getPAPlanForEdit(id).subscribe((result) => {
                this.record.set(result.paPlan);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('PAPlan_KpiInfoId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._service
            .createOrEdit(this.record())
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

    updateField(field: keyof CreateOrEditPAPlanDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditPAPlanDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
