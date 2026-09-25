import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    ActivityType,
    CreateOrEditPATimelineExceptionDto,
    EmployeeDto,
    EmployeesServiceProxy,
    GetAllEmployeesInput,
    GetAllPAPeriodsInput,
    PAPeriodDto,
    PAPeriodsServiceProxy,
    PATimelineExceptionsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';
import { DateInputDirective } from '@shared/common/dates/date-input.directive';

@Component({
    selector: 'createOrEditPATimelineExceptionModal',
    templateUrl: './create-or-edit-pa-timeline-exception-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditPATimelineExceptionModalComponent extends AppComponentBase {
    private _service = inject(PATimelineExceptionsServiceProxy);
    private _paPeriodsService = inject(PAPeriodsServiceProxy);
    private _employeesService = inject(EmployeesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly paTimelineException = signal<CreateOrEditPATimelineExceptionDto>(new CreateOrEditPATimelineExceptionDto());
    readonly paPeriods = signal<PAPeriodDto[]>([]);
    readonly employees = signal<EmployeeDto[]>([]);

    readonly activityTypes = [
        { value: ActivityType.Plan, key: 'ActivityTypePlan' },
        { value: ActivityType.MidYearReview, key: 'ActivityTypeMidYearReview' },
        { value: ActivityType.AnnualReview, key: 'ActivityTypeAnnualReview' },
        { value: ActivityType.Quarter1Review, key: 'ActivityTypeQuarter1Review' },
        { value: ActivityType.Quarter3Review, key: 'ActivityTypeQuarter3Review' },
    ];

    show(id?: number): void {
        this.active.set(true);

        this._paPeriodsService
            .getAll(new GetAllPAPeriodsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.paPeriods.set(result.items));

        this._employeesService
            .getAll(new GetAllEmployeesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.employees.set(result.items));

        if (!id) {
            this.paTimelineException.set(new CreateOrEditPATimelineExceptionDto());
            this.modal().show();
        } else {
            this._service.getPATimelineExceptionForEdit(id).subscribe((result) => {
                this.paTimelineException.set(result.paTimelineException);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('PATimelineException_PAPeriodId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._service
            .createOrEdit(this.paTimelineException())
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

    updateField(field: keyof CreateOrEditPATimelineExceptionDto, value: any): void {
        const current = this.paTimelineException();
        (current as any)[field] = value;
        const next = new CreateOrEditPATimelineExceptionDto();
        Object.assign(next, current);
        this.paTimelineException.set(next);
    }

    updateExtensionDate(value: string): void {
        this.updateField('extensionDate', value ? DateTime.fromISO(value) : (undefined as any));
    }
}
