import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditPAFormDto,
    EmployeeDto,
    EmployeesServiceProxy,
    GetAllEmployeesInput,
    GetAllPAPeriodsInput,
    PAFormsServiceProxy,
    PAPeriodDto,
    PAPeriodsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditPAFormModal',
    templateUrl: './create-or-edit-pa-form-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditPAFormModalComponent extends AppComponentBase {
    private _service = inject(PAFormsServiceProxy);
    private _paPeriodsService = inject(PAPeriodsServiceProxy);
    private _employeesService = inject(EmployeesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly paForm = signal<CreateOrEditPAFormDto>(new CreateOrEditPAFormDto());
    readonly paPeriods = signal<PAPeriodDto[]>([]);
    readonly employees = signal<EmployeeDto[]>([]);

    show(id?: number): void {
        this.active.set(true);

        this._paPeriodsService
            .getAll(new GetAllPAPeriodsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.paPeriods.set(result.items));

        this._employeesService
            .getAll(new GetAllEmployeesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.employees.set(result.items));

        if (!id) {
            this.paForm.set(new CreateOrEditPAFormDto());
            this.modal().show();
        } else {
            this._service.getPAFormForEdit(id).subscribe((result) => {
                this.paForm.set(result.paForm);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('PAForm_PAPeriodId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._service
            .createOrEdit(this.paForm())
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

    employeeLabel(employee: EmployeeDto): string {
        return `${employee.code} - ${employee.fullNameEn}`;
    }

    updateField(field: keyof CreateOrEditPAFormDto, value: any): void {
        const current = this.paForm();
        (current as any)[field] = value;
        const next = new CreateOrEditPAFormDto();
        Object.assign(next, current);
        this.paForm.set(next);
    }
}
