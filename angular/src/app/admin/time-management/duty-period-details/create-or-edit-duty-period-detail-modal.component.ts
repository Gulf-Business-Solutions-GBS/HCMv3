import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditDutyPeriodDetailDto,
    DutiesServiceProxy,
    DutyDto,
    DutyPeriodDetailsServiceProxy,
    DutyPeriodDto,
    DutyPeriodsServiceProxy,
    GetAllDutiesInput,
    GetAllDutyPeriodsInput,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditDutyPeriodDetailModal',
    templateUrl: './create-or-edit-duty-period-detail-modal.component.html',
    imports: [
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditDutyPeriodDetailModalComponent extends AppComponentBase {
    private _dutyPeriodDetailsService = inject(DutyPeriodDetailsServiceProxy);
    private _dutyPeriodsService = inject(DutyPeriodsServiceProxy);
    private _dutiesService = inject(DutiesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly dutyPeriodDetail = signal<CreateOrEditDutyPeriodDetailDto>(new CreateOrEditDutyPeriodDetailDto());
    readonly dutyPeriods = signal<DutyPeriodDto[]>([]);
    readonly duties = signal<DutyDto[]>([]);

    show(dutyPeriodDetailId?: number): void {
        this.active.set(true);

        this._dutyPeriodsService
            .getAll(new GetAllDutyPeriodsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => {
                this.dutyPeriods.set(result.items);
            });

        this._dutiesService
            .getAll(new GetAllDutiesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => {
                this.duties.set(result.items);
            });

        if (!dutyPeriodDetailId) {
            this.dutyPeriodDetail.set(new CreateOrEditDutyPeriodDetailDto());
            this.modal().show();
        } else {
            this._dutyPeriodDetailsService.getDutyPeriodDetailForEdit(dutyPeriodDetailId).subscribe((result) => {
                this.dutyPeriodDetail.set(result.dutyPeriodDetail);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('DutyPeriodDetail_DutyPeriodId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._dutyPeriodDetailsService
            .createOrEdit(this.dutyPeriodDetail())
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

    updateField(field: keyof CreateOrEditDutyPeriodDetailDto, value: any): void {
        const current = this.dutyPeriodDetail();
        (current as any)[field] = value;
        const next = new CreateOrEditDutyPeriodDetailDto();
        Object.assign(next, current);
        this.dutyPeriodDetail.set(next);
    }
}
