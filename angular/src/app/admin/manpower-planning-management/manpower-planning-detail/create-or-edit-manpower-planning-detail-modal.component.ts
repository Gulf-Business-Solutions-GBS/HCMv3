import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditManpowerPlanningDetailDto,
    EmployeeDto,
    EmployeesServiceProxy,
    GetAllEmployeesInput,
    GetAllSimpleLookupsInput,
    JobTitlesServiceProxy,
    ManpowerPlanningDetailsServiceProxy,
    SimpleLookupDto,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

const MONTH_FIELDS: (keyof CreateOrEditManpowerPlanningDetailDto)[] = [
    'january',
    'february',
    'march',
    'april',
    'may',
    'june',
    'july',
    'august',
    'september',
    'october',
    'november',
    'december',
];

@Component({
    selector: 'createOrEditManpowerPlanningDetailModal',
    templateUrl: './create-or-edit-manpower-planning-detail-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditManpowerPlanningDetailModalComponent extends AppComponentBase {
    private _service = inject(ManpowerPlanningDetailsServiceProxy);
    private _jobTitlesService = inject(JobTitlesServiceProxy);
    private _employeesService = inject(EmployeesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditManpowerPlanningDetailDto>(new CreateOrEditManpowerPlanningDetailDto());

    readonly jobTitles = signal<SimpleLookupDto[]>([]);
    readonly employees = signal<EmployeeDto[]>([]);

    show(id?: number, manpowerPlanningId?: number): void {
        this.active.set(true);

        this._jobTitlesService
            .getAll(
                new GetAllSimpleLookupsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.jobTitles.set(result.items));

        this._employeesService
            .getAll(
                new GetAllEmployeesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined })
            )
            .subscribe((result) => this.employees.set(result.items));

        if (!id) {
            const record = new CreateOrEditManpowerPlanningDetailDto();
            record.manpowerPlanningId = manpowerPlanningId as any;
            for (const field of MONTH_FIELDS) {
                (record as any)[field] = 0;
            }
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getManpowerPlanningDetailForEdit(id).subscribe((result) => {
                this.record.set(result.manpowerPlanningDetail);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('ManpowerPlanningDetail_JobTitleId')?.focus();
    }

    employeeLabel(employee: EmployeeDto): string {
        return `${employee.code} - ${employee.fullNameEn}`;
    }

    /** Client-side convenience only (not a server-side feature): copies January's amount into the remaining months. */
    fillRemainingMonthsFromJanuary(): void {
        const current = this.record();
        const next = new CreateOrEditManpowerPlanningDetailDto();
        Object.assign(next, current);
        const januaryAmount = current.january ?? 0;
        for (const field of MONTH_FIELDS) {
            if (field !== 'january') {
                (next as any)[field] = januaryAmount;
            }
        }
        this.record.set(next);
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

    updateField(field: keyof CreateOrEditManpowerPlanningDetailDto, value: any): void {
        const current = this.record();
        const next = new CreateOrEditManpowerPlanningDetailDto();
        Object.assign(next, current);
        (next as any)[field] = value;
        this.record.set(next);
    }
}
