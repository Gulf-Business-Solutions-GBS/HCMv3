import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditPAEmployeeReviewerDto,
    EmployeeDto,
    EmployeesServiceProxy,
    GetAllEmployeesInput,
    PAEmployeeReviewersServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditPAEmployeeReviewerModal',
    templateUrl: './create-or-edit-pa-employee-reviewer-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditPAEmployeeReviewerModalComponent extends AppComponentBase {
    private _service = inject(PAEmployeeReviewersServiceProxy);
    private _employeesService = inject(EmployeesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditPAEmployeeReviewerDto>(new CreateOrEditPAEmployeeReviewerDto());
    readonly employees = signal<EmployeeDto[]>([]);

    show(paEmployeeId: number, id?: number): void {
        this.active.set(true);

        this._employeesService
            .getAll(new GetAllEmployeesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.employees.set(result.items));

        if (!id) {
            const record = new CreateOrEditPAEmployeeReviewerDto();
            record.paEmployeeId = paEmployeeId;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getPAEmployeeReviewerForEdit(id).subscribe((result) => {
                this.record.set(result.paEmployeeReviewer);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('PAEmployeeReviewer_EmployeeId')?.focus();
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

    employeeLabel(employee: EmployeeDto): string {
        return `${employee.code} - ${employee.fullNameEn}`;
    }

    updateField(field: keyof CreateOrEditPAEmployeeReviewerDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditPAEmployeeReviewerDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
