import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DateTime } from 'luxon';
import { Tooltip } from 'primeng/tooltip';
import { finalize } from 'rxjs/operators';

import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import {
    AdminToolResultDto,
    FixForfeitQuotaInput,
    GeneratePayrollCalendarInput,
    GetAllPayrollAreasInput,
    PayrollAdminToolsServiceProxy,
    PayrollAreaDto,
    PayrollAreasServiceProxy,
    PayrollRunType,
    SalaryRegisterMaintenanceInput,
} from '@shared/service-proxies/service-proxies';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';

/**
 * Port of legacy HCM10.Web/UserControls/Administration/uscPayrollConfigPanel.ascx.
 *
 * Every panel here is destructive; legacy fired them straight off a callback panel with no confirmation.
 * This port routes each one through message.confirm() with the concrete scope spelled out.
 */
@Component({
    templateUrl: './payroll-admin-tools.component.html',
    animations: [appModuleAnimation()],
    imports: [SubHeaderComponent, BusyIfDirective, FormsModule, Tooltip, LocalizePipe, PermissionPipe],
})
export class PayrollAdminToolsComponent extends AppComponentBase implements OnInit {
    private _payrollAdminToolsService = inject(PayrollAdminToolsServiceProxy);
    private _payrollAreasService = inject(PayrollAreasServiceProxy);

    readonly payrollAreas = signal<PayrollAreaDto[]>([]);

    // ---- Generate payroll calendar (legacy line 98) -------------------------------------------------
    readonly calendarPayrollAreaId = signal<number | undefined>(undefined);
    readonly calendarStartDate = signal<string>('');
    readonly calendarEndDate = signal<string>('');

    // ---- Delete salary registers (legacy line 1253) -------------------------------------------------
    readonly deleteMonth = signal<number | undefined>(undefined);
    readonly deleteYear = signal<number | undefined>(undefined);
    readonly deletePayrollAreaId = signal<number | undefined>(undefined);
    readonly deleteEmployeeCodes = signal<string>('');
    readonly deleteRunType = signal<PayrollRunType>(PayrollRunType.Regular);

    // ---- Unlock salary registers (legacy line 1397) -------------------------------------------------
    readonly unlockMonth = signal<number | undefined>(undefined);
    readonly unlockYear = signal<number | undefined>(undefined);
    readonly unlockPayrollAreaId = signal<number | undefined>(undefined);
    readonly unlockEmployeeCodes = signal<string>('');
    readonly unlockRunType = signal<PayrollRunType>(PayrollRunType.Regular);

    // ---- Fix forfeit quota (legacy line 1171) -------------------------------------------------------
    readonly forfeitStartDate = signal<string>('');
    readonly forfeitEmployeeCodes = signal<string>('');

    // ---- Shared result panel -----------------------------------------------------------------------
    readonly running = signal<string>('');
    readonly resultTitle = signal<string>('');
    readonly result = signal<AdminToolResultDto | undefined>(undefined);
    readonly errorMessage = signal<string>('');

    readonly runTypes: PayrollRunType[] = [PayrollRunType.Regular, PayrollRunType.OffCycle];

    ngOnInit(): void {
        this._payrollAreasService
            .getAll(
                new GetAllPayrollAreasInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((r) => this.payrollAreas.set(r.items));
    }

    runTypeName(runType: PayrollRunType): string {
        return runType === PayrollRunType.OffCycle ? this.l('OffCycle') : this.l('Regular');
    }

    payrollAreaName(id: number | undefined): string {
        if (!id) {
            return this.l('ScopeAllPayrollAreas');
        }

        const area = this.payrollAreas().find((a) => a.id === id);
        return area ? area.name : String(id);
    }

    /** Splits the legacy comma-separated employee-code box into a real list. */
    parseCodes(raw: string): string[] {
        return (raw || '')
            .split(',')
            .map((c) => c.trim())
            .filter((c) => !!c);
    }

    scopeText(raw: string): string {
        const codes = this.parseCodes(raw);
        return codes.length === 0
            ? this.l('ScopeAllActiveEmployees')
            : this.l('ScopeSelectedEmployeeCodes', codes.length, codes.join(', '));
    }

    // ---- Actions -----------------------------------------------------------------------------------

    generateCalendar(): void {
        if (!this.calendarPayrollAreaId() || !this.calendarStartDate() || !this.calendarEndDate()) {
            this.message.warn(this.l('PayrollAreaAndEmployeeCodeBothCannotBeBlank'));
            return;
        }

        if (this.calendarEndDate() < this.calendarStartDate()) {
            this.message.warn(this.l('EndDateCannotBeBeforeStartDate'));
            return;
        }

        this.message.confirm(
            this.l(
                'ConfirmGeneratePayrollCalendar',
                this.payrollAreaName(this.calendarPayrollAreaId()),
                this.calendarStartDate(),
                this.calendarEndDate()
            ),
            this.l('AreYouSure'),
            (isConfirmed) => {
                if (!isConfirmed) {
                    return;
                }

                this.start('calendar', this.l('GeneratePayrollCalendar'));
                this._payrollAdminToolsService
                    .generatePayrollCalendar(
                        new GeneratePayrollCalendarInput({
                            payrollAreaId: this.calendarPayrollAreaId(),
                            startDate: DateTime.fromISO(this.calendarStartDate()),
                            endDate: DateTime.fromISO(this.calendarEndDate()),
                        })
                    )
                    .pipe(finalize(() => this.running.set('')))
                    .subscribe({
                        next: (r) => this.succeeded(r),
                        error: (e) => this.failed(e),
                    });
            }
        );
    }

    deleteSalaryRegisters(): void {
        if (!this.deleteMonth() || !this.deleteYear()) {
            this.message.warn(this.l('IncorrectPayrollPeriod'));
            return;
        }

        if (!this.deletePayrollAreaId() && this.parseCodes(this.deleteEmployeeCodes()).length === 0) {
            this.message.warn(this.l('PayrollAreaAndEmployeeCodeBothCannotBeBlank'));
            return;
        }

        this.message.confirm(
            this.l(
                'ConfirmDeleteSalaryRegisters',
                this.deleteMonth(),
                this.deleteYear(),
                this.runTypeName(this.deleteRunType()),
                this.payrollAreaName(this.deletePayrollAreaId()),
                this.scopeText(this.deleteEmployeeCodes())
            ),
            this.l('AreYouSure'),
            (isConfirmed) => {
                if (!isConfirmed) {
                    return;
                }

                this.start('delete', this.l('DeleteSalaryRegisters'));
                this._payrollAdminToolsService
                    .deleteSalaryRegisters(
                        new SalaryRegisterMaintenanceInput({
                            month: this.deleteMonth(),
                            year: this.deleteYear(),
                            payrollAreaId: this.deletePayrollAreaId(),
                            employeeCodes: this.parseCodes(this.deleteEmployeeCodes()),
                            payrollRunType: this.deleteRunType(),
                        })
                    )
                    .pipe(finalize(() => this.running.set('')))
                    .subscribe({
                        next: (r) => this.succeeded(r),
                        error: (e) => this.failed(e),
                    });
            }
        );
    }

    unlockSalaryRegisters(): void {
        if (!this.unlockMonth() || !this.unlockYear()) {
            this.message.warn(this.l('IncorrectPayrollPeriod'));
            return;
        }

        if (!this.unlockPayrollAreaId() && this.parseCodes(this.unlockEmployeeCodes()).length === 0) {
            this.message.warn(this.l('PayrollAreaAndEmployeeCodeBothCannotBeBlank'));
            return;
        }

        this.message.confirm(
            this.l(
                'ConfirmUnlockSalaryRegisters',
                this.unlockMonth(),
                this.unlockYear(),
                this.runTypeName(this.unlockRunType()),
                this.payrollAreaName(this.unlockPayrollAreaId()),
                this.scopeText(this.unlockEmployeeCodes())
            ),
            this.l('AreYouSure'),
            (isConfirmed) => {
                if (!isConfirmed) {
                    return;
                }

                this.start('unlock', this.l('UnlockSalaryRegisters'));
                this._payrollAdminToolsService
                    .unlockSalaryRegisters(
                        new SalaryRegisterMaintenanceInput({
                            month: this.unlockMonth(),
                            year: this.unlockYear(),
                            payrollAreaId: this.unlockPayrollAreaId(),
                            employeeCodes: this.parseCodes(this.unlockEmployeeCodes()),
                            payrollRunType: this.unlockRunType(),
                        })
                    )
                    .pipe(finalize(() => this.running.set('')))
                    .subscribe({
                        next: (r) => this.succeeded(r),
                        error: (e) => this.failed(e),
                    });
            }
        );
    }

    fixForfeitQuota(): void {
        this.message.confirm(
            this.l(
                'ConfirmFixForfeitQuota',
                this.forfeitStartDate() || this.l('EarliestEvaluationDateDefault'),
                this.scopeText(this.forfeitEmployeeCodes())
            ),
            this.l('AreYouSure'),
            (isConfirmed) => {
                if (!isConfirmed) {
                    return;
                }

                this.start('forfeit', this.l('FixForfeitQuota'));
                this._payrollAdminToolsService
                    .fixForfeitQuota(
                        new FixForfeitQuotaInput({
                            startDate: this.forfeitStartDate() ? DateTime.fromISO(this.forfeitStartDate()) : undefined,
                            employeeCodes: this.parseCodes(this.forfeitEmployeeCodes()),
                        })
                    )
                    .pipe(finalize(() => this.running.set('')))
                    .subscribe({
                        next: (r) => this.succeeded(r),
                        error: (e) => this.failed(e),
                    });
            }
        );
    }

    // ---- Result plumbing ---------------------------------------------------------------------------

    private start(key: string, title: string): void {
        this.running.set(key);
        this.resultTitle.set(title);
        this.result.set(undefined);
        this.errorMessage.set('');
    }

    private succeeded(result: AdminToolResultDto): void {
        this.result.set(result);
        this.notify.success(this.l('AdminToolResultSummary', result.affectedCount, result.skippedCount));
    }

    private failed(error: any): void {
        this.errorMessage.set(error?.message || error?.details || '');
    }
}
