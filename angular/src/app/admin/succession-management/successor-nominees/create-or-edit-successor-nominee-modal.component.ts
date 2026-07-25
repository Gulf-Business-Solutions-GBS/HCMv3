import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditSuccessorNomineeDto,
    EmployeeDto,
    EmployeesServiceProxy,
    GetAllEmployeesInput,
    GetAllSimpleLookupsInput,
    RankingsServiceProxy,
    ReadinessesServiceProxy,
    SimpleLookupDto,
    SuccessorNomineesServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditSuccessorNomineeModal',
    templateUrl: './create-or-edit-successor-nominee-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditSuccessorNomineeModalComponent extends AppComponentBase {
    private _service = inject(SuccessorNomineesServiceProxy);
    private _employeesService = inject(EmployeesServiceProxy);
    private _readinessesService = inject(ReadinessesServiceProxy);
    private _rankingsService = inject(RankingsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditSuccessorNomineeDto>(new CreateOrEditSuccessorNomineeDto());

    readonly employees = signal<EmployeeDto[]>([]);
    readonly readinesses = signal<SimpleLookupDto[]>([]);
    readonly rankings = signal<SimpleLookupDto[]>([]);

    show(id?: number): void {
        this.active.set(true);

        this._employeesService
            .getAll(new GetAllEmployeesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.employees.set(result.items));

        this._readinessesService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.readinesses.set(result.items));

        this._rankingsService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.rankings.set(result.items));

        if (!id) {
            this.record.set(new CreateOrEditSuccessorNomineeDto());
            this.modal().show();
        } else {
            this._service.getSuccessorNomineeForEdit(id).subscribe((result) => {
                this.record.set(result.successorNominee);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('SuccessorNominee_EmployeeId')?.focus();
    }

    employeeLabel(employee: EmployeeDto): string {
        return `${employee.code} - ${employee.fullNameEn}`;
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

    updateField(field: keyof CreateOrEditSuccessorNomineeDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditSuccessorNomineeDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
