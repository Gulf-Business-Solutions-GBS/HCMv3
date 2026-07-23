import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditEmployeeSubGroupBenefitDetailDto,
    DependantTypeDto,
    DependantTypesServiceProxy,
    EmployeeSubGroupBenefitDetailsServiceProxy,
    GetAllDependantTypesInput,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';

@Component({
    selector: 'createOrEditEmployeeSubGroupBenefitDetailModal',
    templateUrl: './create-or-edit-employee-sub-group-benefit-detail-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditEmployeeSubGroupBenefitDetailModalComponent extends AppComponentBase {
    private _service = inject(EmployeeSubGroupBenefitDetailsServiceProxy);
    private _dependantTypesService = inject(DependantTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeSubGroupBenefitDetailDto>(new CreateOrEditEmployeeSubGroupBenefitDetailDto());
    readonly dependantTypes = signal<DependantTypeDto[]>([]);

    show(employeeSubGroupBenefitId: number, id?: number): void {
        this.active.set(true);

        this._dependantTypesService
            .getAll(new GetAllDependantTypesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.dependantTypes.set(result.items));

        if (!id) {
            const record = new CreateOrEditEmployeeSubGroupBenefitDetailDto();
            record.employeeSubGroupBenefitId = employeeSubGroupBenefitId;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getEmployeeSubGroupBenefitDetailForEdit(id).subscribe((result) => {
                this.record.set(result.employeeSubGroupBenefitDetail);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('EmployeeSubGroupBenefitDetail_StartDate')?.focus();
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

    updateField(field: keyof CreateOrEditEmployeeSubGroupBenefitDetailDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeSubGroupBenefitDetailDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateStartDate(value: string): void {
        this.updateField('startDate', value ? DateTime.fromISO(value) : (undefined as any));
    }

    updateEndDate(value: string): void {
        this.updateField('endDate', value ? DateTime.fromISO(value) : (undefined as any));
    }
}
