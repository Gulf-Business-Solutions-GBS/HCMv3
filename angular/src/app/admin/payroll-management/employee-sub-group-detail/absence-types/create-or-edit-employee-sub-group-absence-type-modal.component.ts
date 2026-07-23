import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    AbsenceTypeDto,
    AbsenceTypesServiceProxy,
    CreateOrEditEmployeeSubGroupAbsenceTypeDto,
    EmployeeSubGroupAbsenceTypesServiceProxy,
    GetAllAbsenceTypesInput,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditEmployeeSubGroupAbsenceTypeModal',
    templateUrl: './create-or-edit-employee-sub-group-absence-type-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditEmployeeSubGroupAbsenceTypeModalComponent extends AppComponentBase {
    private _service = inject(EmployeeSubGroupAbsenceTypesServiceProxy);
    private _absenceTypesService = inject(AbsenceTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeSubGroupAbsenceTypeDto>(new CreateOrEditEmployeeSubGroupAbsenceTypeDto());
    readonly absenceTypes = signal<AbsenceTypeDto[]>([]);

    show(employeeSubGroupId: number, id?: number): void {
        this.active.set(true);

        this._absenceTypesService
            .getAll(new GetAllAbsenceTypesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.absenceTypes.set(result.items));

        if (!id) {
            const record = new CreateOrEditEmployeeSubGroupAbsenceTypeDto();
            record.employeeSubGroupId = employeeSubGroupId;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getEmployeeSubGroupAbsenceTypeForEdit(id).subscribe((result) => {
                this.record.set(result.employeeSubGroupAbsenceType);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('EmployeeSubGroupAbsenceType_AbsenceTypeId')?.focus();
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

    updateField(field: keyof CreateOrEditEmployeeSubGroupAbsenceTypeDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeSubGroupAbsenceTypeDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
