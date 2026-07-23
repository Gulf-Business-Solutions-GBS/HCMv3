import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditEmployeeSubGroupBonusDto,
    EmployeeSubGroupBonusesServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';

@Component({
    selector: 'createOrEditEmployeeSubGroupBonusModal',
    templateUrl: './create-or-edit-employee-sub-group-bonus-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditEmployeeSubGroupBonusModalComponent extends AppComponentBase {
    private _service = inject(EmployeeSubGroupBonusesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeSubGroupBonusDto>(new CreateOrEditEmployeeSubGroupBonusDto());

    show(employeeSubGroupId: number, id?: number): void {
        this.active.set(true);

        if (!id) {
            const record = new CreateOrEditEmployeeSubGroupBonusDto();
            record.employeeSubGroupId = employeeSubGroupId;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getEmployeeSubGroupBonusForEdit(id).subscribe((result) => {
                this.record.set(result.employeeSubGroupBonus);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('EmployeeSubGroupBonus_StartDate')?.focus();
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

    updateField(field: keyof CreateOrEditEmployeeSubGroupBonusDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeSubGroupBonusDto();
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
