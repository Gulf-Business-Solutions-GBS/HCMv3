import { Component, DestroyRef, computed, inject, output, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    AbsenceTypeDto,
    AbsenceTypesServiceProxy,
    CreateOrEditEmployeeAbsenceDto,
    DeductionUnit,
    EmployeeAbsencePreviewDto,
    EmployeeAbsencesServiceProxy,
    GetAllAbsenceTypesInput,
    GetEmployeeAbsenceEmployeesInput,
    GetEmployeeAbsencePreviewInput,
    HalfdayType,
    LeavePaymentType,
    SimpleLookupDto,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { Observable, Subject, of } from 'rxjs';
import { catchError, debounceTime, finalize, switchMap } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';
import { DateInputDirective } from '@shared/common/dates/date-input.directive';
import { RecordDirectlyCheckComponent } from '@app/shared/common/record-directly/record-directly-check.component';
import { AttachmentsComponent } from '@app/shared/common/attachments/attachments.component';
import { SelectModule } from 'primeng/select';

/**
 * The fields the server's calculation reads. A change to any of them refreshes the read-only panel -- legacy's
 * XAF form recalculated the same figures as the user typed (ImmediatePostData).
 */
const PREVIEW_FIELDS: ReadonlyArray<keyof CreateOrEditEmployeeAbsenceDto> = [
    'employeeId',
    'absenceTypeId',
    'startDate',
    'endDate',
    'startTime',
    'endTime',
    'returnDate',
    'paidStartDate',
    'paidEndDate',
    'unpaidStartDate',
    'unpaidEndDate',
];

@Component({
    selector: 'createOrEditEmployeeAbsenceModal',
    templateUrl: './create-or-edit-employee-absence-modal.component.html',
    imports: [
        RecordDirectlyCheckComponent,
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
        AttachmentsComponent,
        SelectModule,
    ],
})
export class CreateOrEditEmployeeAbsenceModalComponent extends AppComponentBase {
    private _service = inject(EmployeeAbsencesServiceProxy);
    private _absenceTypesService = inject(AbsenceTypesServiceProxy);
    private _destroyRef = inject(DestroyRef);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeAbsenceDto>(new CreateOrEditEmployeeAbsenceDto());

    readonly absenceTypes = signal<AbsenceTypeDto[]>([]);

    /** The calculated, read-only figures for the leave as currently entered; undefined until they can be worked out. */
    readonly preview = signal<EmployeeAbsencePreviewDto | undefined>(undefined);

    readonly DeductionUnitPercentage = DeductionUnit.Percentage;

    /** The legacy form's bottom tab group: Return Information / Sick Leave Unpaid Information. */
    readonly activeTab = signal<'return' | 'sick'>('return');

    private readonly _previewRequests = new Subject<CreateOrEditEmployeeAbsenceDto>();

    readonly halfdayTypes = [
        { value: HalfdayType.None, name: 'None' },
        { value: HalfdayType.StartOfDuty, name: 'StartOfDuty' },
        { value: HalfdayType.EndOfDuty, name: 'EndOfDuty' },
    ];

    readonly leavePaymentTypes = [
        { value: LeavePaymentType.None, name: 'None' },
        { value: LeavePaymentType.PayWithPayroll, name: 'PayWithPayroll' },
        { value: LeavePaymentType.PayInAdvance, name: 'PayInAdvance' },
        { value: LeavePaymentType.PayFullWithPayroll, name: 'PayFullWithPayroll' },
    ];

    constructor() {
        super();

        // Debounced so typing a date does not fire one request per keystroke; switchMap drops a stale answer.
        this._previewRequests
            .pipe(
                debounceTime(300),
                switchMap((record) => this.fetchPreview(record)),
                takeUntilDestroyed(this._destroyRef)
            )
            .subscribe((preview) => this.preview.set(preview));
    }

    /**
     * True when the dialog was opened with no employee given -- from the Employee Leaves list rather than from
     * inside an employee's record -- so the employee is chosen here (legacy's first field, Employee).
     */
    readonly choosesEmployee = signal<boolean>(false);

    readonly employees = signal<SimpleLookupDto[]>([]);

    /** The dropdown's entries, "code - name", searchable inside the dropdown itself. */
    readonly employeeOptions = computed(() =>
        this.employees().map((e) => ({ id: e.id, label: `${e.code} - ${e.nameEn}` }))
    );

    /** The leave's employee as loaded for edit; always offered in the dropdown even before the lookup returns. */
    readonly currentEmployee = signal<SimpleLookupDto | undefined>(undefined);

    show(employeeId: number | undefined, id?: number): void {
        this.active.set(true);
        this.preview.set(undefined);
        this.activeTab.set('return');
        this.choosesEmployee.set(!employeeId);
        this.currentEmployee.set(undefined);

        if (this.choosesEmployee()) {
            this._service
                .getEmployeesForLookup(new GetEmployeeAbsenceEmployeesInput({ filter: undefined }))
                .subscribe((result) => this.employees.set(this.withCurrentEmployee(result.items)));
        }

        this._absenceTypesService
            .getAll(
                new GetAllAbsenceTypesInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.absenceTypes.set(result.items));

        if (!id) {
            const record = new CreateOrEditEmployeeAbsenceDto();
            record.employeeId = employeeId;
            record.halfdayType = HalfdayType.None;
            record.leavePaymentType = LeavePaymentType.None;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getEmployeeAbsenceForEdit(id).subscribe((result) => {
                this.record.set(result.employeeAbsence);
                this.currentEmployee.set(
                    new SimpleLookupDto({
                        id: result.employeeAbsence.employeeId,
                        code: result.employeeCode,
                        nameEn: result.employeeFullNameEn,
                        nameAr: undefined,
                    })
                );
                this.employees.set(this.withCurrentEmployee(this.employees()));
                this.requestPreview();
                this.modal().show();
            });
        }
    }

    private withCurrentEmployee(items: SimpleLookupDto[]): SimpleLookupDto[] {
        const current = this.currentEmployee();
        if (!current || items.some((e) => e.id === current.id)) {
            return items;
        }

        return [current, ...items];
    }

    onShown(): void {
        document.getElementById('EmployeeAbsence_AbsenceTypeId')?.focus();
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

    updateField(field: keyof CreateOrEditEmployeeAbsenceDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeAbsenceDto();
        Object.assign(next, current);
        this.record.set(next);

        if (PREVIEW_FIELDS.includes(field)) {
            this.requestPreview();
        }
    }

    updateDateField(field: keyof CreateOrEditEmployeeAbsenceDto, value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }

    getTimeValue(field: keyof CreateOrEditEmployeeAbsenceDto): string | undefined {
        const value = (this.record() as any)[field] as DateTime | undefined;
        return value?.toFormat('HH:mm:ss') ?? undefined;
    }

    updateTimeField(field: keyof CreateOrEditEmployeeAbsenceDto, value: string): void {
        if (!value) {
            this.updateField(field, undefined as any);
            return;
        }
        const [hour, minute, second] = value.split(':').map((part) => Number(part));
        const current = (this.record() as any)[field] as DateTime | undefined;
        const base = current ?? DateTime.now();
        this.updateField(field, base.set({ hour, minute, second: second || 0, millisecond: 0 }));
    }

    /**
     * Legacy's <c>ReturnDate.Year != 1</c>: the server sends an unset DateTime as 0001-01-01, which is still a
     * truthy value here.
     */
    hasReturnDate(): boolean {
        const returnDate = this.record().returnDate;
        return !!returnDate && returnDate.isValid && returnDate.year > 1;
    }

    /** A figure as the legacy form showed it: up to three decimals, no trailing zeros. */
    formatNumber(value: number | undefined): string {
        return value === undefined || value === null ? '' : `${Math.round(value * 1000) / 1000}`;
    }

    private requestPreview(): void {
        this._previewRequests.next(this.record());
    }

    private fetchPreview(record: CreateOrEditEmployeeAbsenceDto): Observable<EmployeeAbsencePreviewDto | undefined> {
        if (!record.employeeId || !record.absenceTypeId || !record.startDate || !record.endDate) {
            return of(undefined);
        }

        const input = new GetEmployeeAbsencePreviewInput();
        input.employeeId = record.employeeId;
        input.absenceTypeId = record.absenceTypeId;
        input.startDate = record.startDate;
        input.endDate = record.endDate;
        input.startTime = record.startTime;
        input.endTime = record.endTime;
        input.returnDate = record.returnDate;
        input.paidStartDate = record.paidStartDate;
        input.paidEndDate = record.paidEndDate;
        input.unpaidStartDate = record.unpaidStartDate;
        input.unpaidEndDate = record.unpaidEndDate;

        // An explanation of the leave, not part of it: failing to work it out must not stop anybody saving.
        return this._service.getAbsencePreview(input).pipe(catchError(() => of(undefined)));
    }
}
