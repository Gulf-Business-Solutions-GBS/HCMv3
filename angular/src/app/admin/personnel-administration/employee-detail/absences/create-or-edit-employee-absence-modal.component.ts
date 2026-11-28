import { Component, DestroyRef, computed, inject, output, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    AbsenceTypeDto,
    AbsenceTypesServiceProxy,
    CreateOrEditEmployeeAbsenceDto,
    DeductionUnit,
    EmployeeAbsenceFormSettingsDto,
    EmployeeAbsenceLeaveTypeDto,
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
import { Observable, Subject, firstValueFrom, of } from 'rxjs';
import { catchError, debounceTime, switchMap } from 'rxjs/operators';
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
 * The fields the server's calculation reads. A change to any of them refreshes the read-only figures -- legacy's
 * XAF form recalculated the same figures as the user typed (ImmediatePostData).
 */
const PREVIEW_FIELDS: ReadonlyArray<keyof CreateOrEditEmployeeAbsenceDto> = [
    'employeeId',
    'absenceTypeId',
    'startDate',
    'endDate',
    'halfdayType',
    'startTime',
    'endTime',
    'paymentDate',
    'returnDate',
    'paidStartDate',
    'paidEndDate',
    'unpaidStartDate',
    'unpaidEndDate',
    'extStartDate',
    'extEndDate',
];

/** Legacy shows the Sick Leave Unpaid Information tab only for this leave type code (controller lines 97-101). */
const SICK_LEAVE_UNPAID_CODE = '5';

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
    readonly stagedAttachments = viewChild<AttachmentsComponent>('stagedAttachments');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeAbsenceDto>(new CreateOrEditEmployeeAbsenceDto());

    /** Every type, only to name the Extra Unpaid leave type (which the system chooses, not the user). */
    readonly absenceTypes = signal<AbsenceTypeDto[]>([]);

    /** The Leave Type drop-down: the types this employee may take (legacy EmpAbsenceType). */
    readonly leaveTypes = signal<EmployeeAbsenceLeaveTypeDto[]>([]);

    readonly settings = signal<EmployeeAbsenceFormSettingsDto>(new EmployeeAbsenceFormSettingsDto());

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

    /** Pay Full With Payroll is offered only when the time settings enable it (the save refuses it otherwise). */
    readonly leavePaymentTypes = computed(() =>
        [
            { value: LeavePaymentType.None, name: 'None' },
            { value: LeavePaymentType.PayWithPayroll, name: 'PayWithPayroll' },
            { value: LeavePaymentType.PayInAdvance, name: 'PayInAdvance' },
            { value: LeavePaymentType.PayFullWithPayroll, name: 'PayFullWithPayroll' },
        ].filter(
            (o) =>
                o.value !== LeavePaymentType.PayFullWithPayroll ||
                this.settings().enablePayFullWithPayroll ||
                this.record().leavePaymentType === LeavePaymentType.PayFullWithPayroll
        )
    );

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

    readonly selectedType = computed(() => this.leaveTypes().find((t) => t.id === this.record().absenceTypeId));

    /** Partial leave (half day, times) only for a type that allows it (legacy controller lines 237-333). */
    readonly allowsPartial = computed(() => !!this.selectedType()?.allowPartialLeave);

    /** Half Day Type only when the time settings enable it (controller lines 84-87). */
    readonly showHalfDay = computed(() => this.settings().enableAutoHalfDayLeave);

    readonly isHalfDay = computed(() => (this.record().halfdayType ?? HalfdayType.None) !== HalfdayType.None);

    /** Payment Date only for an advance or full payment (controller lines 128-140, 334-348). */
    readonly showPaymentDate = computed(
        () =>
            this.record().leavePaymentType === LeavePaymentType.PayInAdvance ||
            this.record().leavePaymentType === LeavePaymentType.PayFullWithPayroll
    );

    readonly showSickTab = computed(() => this.selectedType()?.code === SICK_LEAVE_UNPAID_CODE);

    /** The Extra Unpaid leave type's name; legacy kept it read-only and filled it itself. */
    readonly extAbsenceTypeName = computed(() => {
        const id = this.record().extAbsenceTypeId;
        return id ? (this.absenceTypes().find((t) => t.id === id)?.nameEn ?? '') : '';
    });

    constructor() {
        super();

        // Debounced so typing a date does not fire one request per keystroke; switchMap drops a stale answer.
        this._previewRequests
            .pipe(
                debounceTime(300),
                switchMap((record) => this.fetchPreview(record)),
                takeUntilDestroyed(this._destroyRef)
            )
            .subscribe((preview) => this.applyPreview(preview));
    }

    show(employeeId: number | undefined, id?: number): void {
        this.active.set(true);
        this.preview.set(undefined);
        this.activeTab.set('return');
        this.choosesEmployee.set(!employeeId);
        this.currentEmployee.set(undefined);
        this.leaveTypes.set([]);

        if (this.choosesEmployee()) {
            this._service
                .getEmployeesForLookup(new GetEmployeeAbsenceEmployeesInput({ filter: undefined }))
                .subscribe((result) => this.employees.set(this.withCurrentEmployee(result.items)));
        }

        this._service.getLeaveFormSettings().subscribe((result) => {
            this.settings.set(result);
            // Legacy defaulted a new leave's payment type from the payroll settings.
            if (!this.record().id && this.record().leavePaymentType === LeavePaymentType.None) {
                this.updateField('leavePaymentType', result.defaultLeavePaymentType ?? LeavePaymentType.None);
            }
        });

        this._absenceTypesService
            .getAll(new GetAllAbsenceTypesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.absenceTypes.set(result.items));

        if (!id) {
            // Legacy AfterConstruction (lines 1209-1210): a new leave starts and ends today.
            const today = DateTime.now().startOf('day');
            const record = new CreateOrEditEmployeeAbsenceDto();
            record.employeeId = employeeId;
            record.startDate = today;
            record.endDate = today;
            record.halfdayType = HalfdayType.None;
            record.leavePaymentType = LeavePaymentType.None;
            this.record.set(record);
            this.loadLeaveTypes(employeeId);
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
                this.loadLeaveTypes(result.employeeAbsence.employeeId);
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

    /** The types this employee may take; the leave's own type stays selectable on edit even if it no longer suits. */
    private loadLeaveTypes(employeeId: number | undefined): void {
        if (!employeeId) {
            this.leaveTypes.set([]);
            return;
        }

        this._service.getLeaveTypesForEmployee(employeeId).subscribe((result) => {
            const items = result.items ?? [];
            const currentId = this.record().absenceTypeId;
            const current = this.absenceTypes().find((t) => t.id === currentId);
            if (currentId && !items.some((t) => t.id === currentId) && current) {
                items.unshift(
                    new EmployeeAbsenceLeaveTypeDto({
                        id: current.id,
                        code: current.code,
                        nameEn: current.nameEn,
                        nameAr: current.nameAr,
                        allowPartialLeave: true,
                        leaveDurationType: undefined as any,
                    })
                );
            }
            this.leaveTypes.set(items);
            this.ensureSickTabValid();
        });
    }

    onShown(): void {
        document.getElementById(this.choosesEmployee() ? 'EmployeeAbsence_EmployeeId' : 'EmployeeAbsence_AbsenceTypeId')?.focus();
    }

    async save(): Promise<void> {
        this.saving.set(true);
        try {
            // Legacy CheckQuota on commit (controller 365-858): more days than available → offer the unpaid split.
            // Proceed puts the cut-back end date and the Extra Unpaid fields on the form; the user then saves
            // again, exactly as the legacy popup did. Asked once: a leave already split is saved as it is.
            if (!this.record().extAbsenceTypeId && (await this.offerUnpaidSplit())) {
                return;
            }

            // Files chosen before the first save go up first; the server attaches them once the leave exists.
            if (!this.record().id) {
                const tokens = (await this.stagedAttachments()?.uploadPendingToTokens()) ?? [];
                this.record().attachmentFileTokens = tokens;
            }

            await firstValueFrom(this._service.createOrEdit(this.record()));
            this.notify.info(this.l('SavedSuccessfully'));
            this.stagedAttachments()?.clearPending();
            this.close();
            this.modalSave.emit();
        } catch {
            // The server's message is already shown by the global error handler.
        } finally {
            this.saving.set(false);
        }
    }

    close(): void {
        this.active.set(false);
        this.modal().hide();
    }

    /**
     * True when the save must stop: the split was offered (and applied or declined), or it is needed but no unpaid
     * leave type exists to take the extra days.
     */
    private async offerUnpaidSplit(): Promise<boolean> {
        const proposal = await firstValueFrom(
            this._service.getUnpaidSplitProposal(this.buildPreviewInput(this.record()))
        ).catch(() => undefined);

        if (!proposal?.needsSplit) {
            return false;
        }

        if (proposal.unpaidLeaveTypeMissing) {
            this.message.warn(proposal.message);
            return true;
        }

        const proceed = await new Promise<boolean>((resolve) =>
            this.message.confirm(proposal.message, this.l('AreYouSure'), (isConfirmed) => resolve(!!isConfirmed))
        );
        if (!proceed) {
            return true;
        }

        const next = new CreateOrEditEmployeeAbsenceDto();
        Object.assign(next, this.record());
        next.endDate = proposal.endDate ?? next.endDate;
        next.extAbsenceTypeId = proposal.extAbsenceTypeId;
        next.extStartDate = proposal.extStartDate;
        next.extEndDate = proposal.extEndDate;
        this.record.set(next);
        this.requestPreview();
        this.notify.info(this.l('UnpaidSplitAppliedReviewAndSave'));
        return true;
    }

    onEmployeeChanged(employeeId: number | undefined): void {
        this.updateField('employeeId', employeeId);
        this.updateField('absenceTypeId', undefined as any);
        this.loadLeaveTypes(employeeId);
    }

    /**
     * Legacy controller lines 261-330: a type that does not allow partial leave clears the half day and the times.
     */
    onLeaveTypeChanged(absenceTypeId: number | undefined): void {
        this.updateField('absenceTypeId', absenceTypeId);
        if (!this.allowsPartial()) {
            this.updateField('halfdayType', HalfdayType.None);
            this.updateField('startTime', undefined as any);
            this.updateField('endTime', undefined as any);
        }
        this.ensureSickTabValid();
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

    /** A date as the date input wants it; blank for the server's unset 0001-01-01. */
    dateValue(value: DateTime | undefined): string | undefined {
        return value?.isValid && value.year > 1 ? (value.toISODate() ?? undefined) : undefined;
    }

    getTimeValue(field: keyof CreateOrEditEmployeeAbsenceDto): string | undefined {
        // A half day's times are the duty's, worked out by the server (legacy RecalculateDutyTimes).
        if (this.isHalfDay()) {
            const derived = field === 'startTime' ? this.preview()?.startTime : this.preview()?.endTime;
            return derived?.toFormat('HH:mm:ss') ?? undefined;
        }

        const value = (this.record() as any)[field] as DateTime | undefined;
        return value?.isValid && value.year > 1 ? value.toFormat('HH:mm:ss') : undefined;
    }

    updateTimeField(field: keyof CreateOrEditEmployeeAbsenceDto, value: string): void {
        if (!value) {
            this.updateField(field, undefined as any);
            return;
        }
        const [hour, minute, second] = value.split(':').map((part) => Number(part));
        const day = this.record().startDate ?? DateTime.now();
        this.updateField(field, day.set({ hour, minute, second: second || 0, millisecond: 0 }));
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

    private ensureSickTabValid(): void {
        if (!this.showSickTab() && this.activeTab() === 'sick') {
            this.activeTab.set('return');
        }
    }

    /**
     * Legacy OnChanged (lines 1172-1176): an empty Payment Date fills itself from the start date. Set directly,
     * not through updateField, so it does not trigger another preview round.
     */
    private applyPreview(preview: EmployeeAbsencePreviewDto | undefined): void {
        this.preview.set(preview);

        const paymentDate = this.record().paymentDate;
        const isEmpty = !paymentDate || !paymentDate.isValid || paymentDate.year <= 1;
        if (preview?.paymentDate && isEmpty) {
            const next = new CreateOrEditEmployeeAbsenceDto();
            Object.assign(next, this.record());
            next.paymentDate = preview.paymentDate;
            this.record.set(next);
        }
    }

    private requestPreview(): void {
        this._previewRequests.next(this.record());
    }

    private fetchPreview(record: CreateOrEditEmployeeAbsenceDto): Observable<EmployeeAbsencePreviewDto | undefined> {
        if (!record.employeeId || !record.absenceTypeId || !record.startDate || !record.endDate) {
            return of(undefined);
        }

        // An explanation of the leave, not part of it: failing to work it out must not stop anybody saving.
        return this._service.getAbsencePreview(this.buildPreviewInput(record)).pipe(catchError(() => of(undefined)));
    }

    private buildPreviewInput(record: CreateOrEditEmployeeAbsenceDto): GetEmployeeAbsencePreviewInput {
        const input = new GetEmployeeAbsencePreviewInput();
        input.employeeAbsenceId = record.id;
        input.employeeId = record.employeeId;
        input.absenceTypeId = record.absenceTypeId;
        input.startDate = record.startDate;
        input.endDate = record.endDate;
        input.halfdayType = record.halfdayType ?? HalfdayType.None;
        input.startTime = record.startTime;
        input.endTime = record.endTime;
        input.paymentDate = record.paymentDate;
        input.returnDate = record.returnDate;
        input.paidStartDate = record.paidStartDate;
        input.paidEndDate = record.paidEndDate;
        input.unpaidStartDate = record.unpaidStartDate;
        input.unpaidEndDate = record.unpaidEndDate;
        input.extStartDate = record.extStartDate;
        input.extEndDate = record.extEndDate;
        return input;
    }
}
