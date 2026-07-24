import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditPAQuarter3ReviewDto,
    GetAllKpiInfosInput,
    GetAllPARatingsInput,
    KpiInfoDto,
    KpiInfosServiceProxy,
    PARatingDto,
    PARatingsServiceProxy,
    PAQuarter3ReviewsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditPAQuarter3ReviewModal',
    templateUrl: './create-or-edit-pa-quarter3-review-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditPAQuarter3ReviewModalComponent extends AppComponentBase {
    private _service = inject(PAQuarter3ReviewsServiceProxy);
    private _kpiInfosService = inject(KpiInfosServiceProxy);
    private _paRatingsService = inject(PARatingsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditPAQuarter3ReviewDto>(new CreateOrEditPAQuarter3ReviewDto());
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
            const record = new CreateOrEditPAQuarter3ReviewDto();
            record.paFormId = paFormId;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getPAQuarter3ReviewForEdit(id).subscribe((result) => {
                this.record.set(result.paQuarter3Review);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('PAQuarter3Review_KpiInfoId')?.focus();
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

    updateField(field: keyof CreateOrEditPAQuarter3ReviewDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditPAQuarter3ReviewDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
