import { Component, computed, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    AdjustmentType,
    CreateOrEditEmployeeTktAdjustmentDto,
    EmployeeTktAdjustmentQuotaDto,
    EmployeeTktAdjustmentsServiceProxy,
    GetAllTicketTypesInput,
    GetEmployeeTktAdjustmentEmployeesInput,
    GetEmployeeTktAdjustmentQuotaInput,
    SimpleLookupDto,
    TicketPaymentType,
    TicketTypeDto,
    TicketTypesServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { DecimalPipe } from '@angular/common';
import { SelectModule } from 'primeng/select';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import { DateTime } from 'luxon';
import { TKT_ADJUSTMENT_TYPES, TKT_PAYMENT_TYPES } from './tkt-adjustment-labels';

/**
 * Ticket adjustment form (legacy EmployeeTktAdjustment_DetailView): Employee / Ticket Type / Adjustment Type /
 * Effective Date, then Quota Accrued / Quota Adjusted / Ticket Payment Type, then Comments.
 *
 * Below the fields it shows the employee's ticket quota whose entitlement period covers the effective date
 * (accrued, used, adjusted, balance), refreshed as employee, ticket type or date change. Saving takes Quota
 * Adjusted from that quota and refuses more than its balance, exactly as legacy did.
 */
@Component({
    selector: 'createOrEditEmployeeTktAdjustmentModal',
    templateUrl: './create-or-edit-employee-tkt-adjustment-modal.component.html',
    imports: [
        AppBsModalDirective,
        FormsModule,
        DecimalPipe,
        SelectModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
        LuxonFormatPipe,
    ],
})
export class CreateOrEditEmployeeTktAdjustmentModalComponent extends AppComponentBase {
    private _service = inject(EmployeeTktAdjustmentsServiceProxy);
    private _ticketTypesService = inject(TicketTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeTktAdjustmentDto>(new CreateOrEditEmployeeTktAdjustmentDto());

    readonly ticketTypes = signal<TicketTypeDto[]>([]);

    readonly adjustmentTypes = TKT_ADJUSTMENT_TYPES;
    readonly ticketPaymentTypes = TKT_PAYMENT_TYPES;

    /**
     * True when opened from the Employee Ticket Adjustments list (no employee given): the employee is chosen
     * here, as on legacy's own Ticket Adjustment screen. False inside an employee's record.
     */
    readonly choosesEmployee = signal<boolean>(false);
    readonly employees = signal<SimpleLookupDto[]>([]);
    readonly employeeOptions = computed(() =>
        this.employees().map((e) => ({ id: e.id, label: `${e.code} - ${e.nameEn}` }))
    );

    /** The record's employee as loaded for edit; shown read-only inside an employee's record. */
    readonly currentEmployee = signal<SimpleLookupDto | undefined>(undefined);

    /** Quota covering the effective date; undefined until employee, ticket type and date are all filled. */
    readonly quota = signal<EmployeeTktAdjustmentQuotaDto | undefined>(undefined);

    /** Quota Accrued last filled in from the quota, so a value the user typed is not overwritten. */
    private _autoQuotaAccrued: number | undefined;

    /** Quota Adjusted is more than the quota can give: saving would be refused. */
    readonly exceedsBalance = computed(() => {
        const q = this.quota();
        const adjusted = this.record().quotaAdjusted ?? 0;
        return !!q && q.quotaFound && adjusted !== 0 && adjusted > q.availableBalance;
    });

    /** A non-zero adjustment with no quota covering the date: saving would be refused. */
    readonly missingQuota = computed(() => {
        const q = this.quota();
        return !!q && !q.quotaFound && (this.record().quotaAdjusted ?? 0) !== 0;
    });

    show(employeeId: number | undefined, id?: number): void {
        this.choosesEmployee.set(!employeeId);
        this.currentEmployee.set(undefined);
        this.quota.set(undefined);
        this._autoQuotaAccrued = undefined;
        this.active.set(true);

        if (this.choosesEmployee()) {
            this._service
                .getEmployeesForLookup(new GetEmployeeTktAdjustmentEmployeesInput({ filter: undefined }))
                .subscribe((result) => this.employees.set(this.withCurrentEmployee(result.items)));
        }

        this._ticketTypesService
            .getAll(
                new GetAllTicketTypesInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.ticketTypes.set(result.items));

        if (!id) {
            const record = new CreateOrEditEmployeeTktAdjustmentDto();
            record.employeeId = employeeId;
            // Legacy defaults: EffectiveDate = today (AfterConstruction); both enums at their first value.
            record.effectiveDate = DateTime.now().startOf('day');
            record.adjustmentType = AdjustmentType.Encashment;
            record.ticketPaymentType = TicketPaymentType.PayWithPayroll;
            record.quotaAccrued = 0;
            record.quotaAdjusted = 0;
            this._autoQuotaAccrued = 0;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getEmployeeTktAdjustmentForEdit(id).subscribe((result) => {
                this.record.set(result.employeeTktAdjustment);
                this.currentEmployee.set(
                    new SimpleLookupDto({
                        id: result.employeeTktAdjustment.employeeId,
                        code: result.employeeCode,
                        nameEn: result.employeeFullNameEn,
                        nameAr: undefined,
                    })
                );
                this.employees.set(this.withCurrentEmployee(this.employees()));
                this.loadQuota();
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document
            .getElementById(this.choosesEmployee() ? 'EmployeeTktAdjustment_EmployeeId' : 'EmployeeTktAdjustment_TicketTypeId')
            ?.focus();
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

    updateField(field: keyof CreateOrEditEmployeeTktAdjustmentDto, value: any): void {
        const next = new CreateOrEditEmployeeTktAdjustmentDto();
        Object.assign(next, this.record());
        (next as any)[field] = value;
        this.record.set(next);

        if (field === 'employeeId' || field === 'ticketTypeId' || field === 'effectiveDate') {
            this.loadQuota();
        }
    }

    updateDateField(field: keyof CreateOrEditEmployeeTktAdjustmentDto, value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }

    updateNumberField(field: keyof CreateOrEditEmployeeTktAdjustmentDto, value: string): void {
        this.updateField(field, value !== null && value !== undefined && value !== '' ? Number(value) : 0);
    }

    /** Fills the whole remaining balance into Quota Adjusted (e.g. to encash or nil the quota). */
    useAvailableBalance(): void {
        const q = this.quota();
        if (q?.quotaFound) {
            this.updateField('quotaAdjusted', q.availableBalance);
        }
    }

    private loadQuota(): void {
        const record = this.record();
        if (!record.employeeId || !record.ticketTypeId || !record.effectiveDate) {
            this.quota.set(undefined);
            return;
        }

        this._service
            .getQuota(
                new GetEmployeeTktAdjustmentQuotaInput({
                    employeeId: record.employeeId,
                    ticketTypeId: record.ticketTypeId,
                    effectiveDate: record.effectiveDate,
                    id: record.id,
                })
            )
            .subscribe((result) => {
                this.quota.set(result);

                // Quota Accrued follows the quota's accrued count unless the user typed their own value.
                const current = this.record();
                const untouched = current.quotaAccrued === this._autoQuotaAccrued || !current.quotaAccrued;
                if (result.quotaFound && untouched && !current.id) {
                    const next = new CreateOrEditEmployeeTktAdjustmentDto();
                    Object.assign(next, current);
                    next.quotaAccrued = result.quotaAccrued;
                    this._autoQuotaAccrued = result.quotaAccrued;
                    this.record.set(next);
                }
            });
    }

    private withCurrentEmployee(items: SimpleLookupDto[]): SimpleLookupDto[] {
        const current = this.currentEmployee();
        if (!current || items.some((e) => e.id === current.id)) {
            return items;
        }

        return [current, ...items];
    }
}
