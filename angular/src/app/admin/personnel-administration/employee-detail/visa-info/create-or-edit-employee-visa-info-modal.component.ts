import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditEmployeeVisaInfoDto,
    EmployeeVisaInfosServiceProxy,
    GetAllSimpleLookupsInput,
    PassportTypesServiceProxy,
    SimpleLookupDto,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';
import { DateInputDirective } from '@shared/common/dates/date-input.directive';

/**
 * One visa record of an employee. Legacy kept a visa history per employee (renewals, transfers between
 * sponsors), so this edits a single row of that history rather than "the" visa of the employee.
 */
@Component({
    selector: 'createOrEditEmployeeVisaInfoModal',
    templateUrl: './create-or-edit-employee-visa-info-modal.component.html',
    imports: [DateInputDirective, AppBsModalDirective, FormsModule, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditEmployeeVisaInfoModalComponent extends AppComponentBase {
    private _service = inject(EmployeeVisaInfosServiceProxy);
    private _passportTypesService = inject(PassportTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeVisaInfoDto>(new CreateOrEditEmployeeVisaInfoDto());
    readonly passportTypes = signal<SimpleLookupDto[]>([]);

    show(employeeId: number, id?: number): void {
        this.active.set(true);
        this.loadLookups();

        if (!id) {
            const record = new CreateOrEditEmployeeVisaInfoDto();
            record.employeeId = employeeId;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getEmployeeVisaInfoForEdit(id).subscribe((result) => {
                this.record.set(result.employeeVisaInfo);
                this.modal().show();
            });
        }
    }

    private loadLookups(): void {
        this._passportTypesService
            .getAll(
                new GetAllSimpleLookupsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.passportTypes.set(result.items));
    }

    onShown(): void {
        document.getElementById('VisaInfo_CurrentFileNo')?.focus();
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

    updateField(field: keyof CreateOrEditEmployeeVisaInfoDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeVisaInfoDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(
        field: 'passportIssueDate' | 'passportExpiryDate' | 'entryDate' | 'terminationDate' | 'startDate' | 'endDate',
        value: string
    ): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }
}
