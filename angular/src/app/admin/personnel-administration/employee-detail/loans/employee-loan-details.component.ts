import { Component, effect, inject, input, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    EmployeeLoanExemptionDto,
    EmployeeLoanExemptionsServiceProxy,
    EmployeeLoanInstallmentDto,
    EmployeeLoanInstallmentsServiceProxy,
    EmployeeLoanRecoveriesServiceProxy,
    EmployeeLoanRecoveryDto,
    GetAllEmployeeLoanExemptionsInput,
    GetAllEmployeeLoanInstallmentsInput,
    GetAllEmployeeLoanRecoveriesInput,
    LoanRecoveryMethod,
    RequestStatus,
} from '@shared/service-proxies/service-proxies';
import { TableModule } from 'primeng/table';
import { Tooltip } from 'primeng/tooltip';
import { finalize } from 'rxjs/operators';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import { DateTime } from 'luxon';
import { CreateOrEditEmployeeLoanRecoveryModalComponent } from '../../employee-loan-recoveries/create-or-edit-employee-loan-recovery-modal.component';
import { CreateOrEditEmployeeLoanExemptionModalComponent } from '../../employee-loan-exemptions/create-or-edit-employee-loan-exemption-modal.component';
import { CreateOrEditEmployeeLoanInstallmentModalComponent } from './create-or-edit-employee-loan-installment-modal.component';

type LoanDetailTab = 'installments' | 'recoveries' | 'exemptions';

/**
 * A loan's instalment schedule, recoveries and exemptions as tabs under the loan -- the way legacy showed its
 * aggregated collections on the loan form. Used inside the loan's Edit dialog and by the separate
 * Installments, Recoveries and Exemptions dialog.
 *
 * Recoveries and exemptions reuse the stand-alone recovery / exemption dialogs (opened with the loan fixed), so
 * the rules are exactly theirs; each tab shows only to a user who may see those records. The instalment
 * schedule is part of the loan, edited with the loan's Edit permission.
 *
 * Recoveries written by the payroll run (Recovery Through Payroll) are shown but not editable: the next run of
 * that period deletes and rewrites them, so a hand edit would be silently lost.
 *
 * Must not be placed inside another form: the dialogs it opens carry forms of their own.
 */
@Component({
    selector: 'employeeLoanDetails',
    templateUrl: './employee-loan-details.component.html',
    imports: [
        BusyIfDirective,
        TableModule,
        Tooltip,
        LocalizePipe,
        PermissionPipe,
        LuxonFormatPipe,
        CreateOrEditEmployeeLoanRecoveryModalComponent,
        CreateOrEditEmployeeLoanExemptionModalComponent,
        CreateOrEditEmployeeLoanInstallmentModalComponent,
    ],
})
export class EmployeeLoanDetailsComponent extends AppComponentBase {
    private _recoveriesService = inject(EmployeeLoanRecoveriesServiceProxy);
    private _exemptionsService = inject(EmployeeLoanExemptionsServiceProxy);
    private _installmentsService = inject(EmployeeLoanInstallmentsServiceProxy);

    readonly loanId = input<number>();
    readonly employeeId = input<number>();

    /** The loan's recovery end date: where the last, open-ended instalment actually stops. */
    readonly recoveryEndDate = input<DateTime>();

    /**
     * For a loan not saved yet: saves it and gives its id, or undefined if it could not be saved. Called when
     * "Create new" is pressed on a tab of a new loan, as legacy let rows be added before the loan was saved.
     */
    readonly ensureLoan = input<() => Promise<number | undefined>>();

    /** Raised when anything under the loan changed, so whoever shows the loan can refresh its balances. */
    readonly changed = output<void>();

    readonly installmentModal = viewChild<CreateOrEditEmployeeLoanInstallmentModalComponent>('installmentModal');
    readonly recoveryModal = viewChild<CreateOrEditEmployeeLoanRecoveryModalComponent>('recoveryModal');
    readonly exemptionModal = viewChild<CreateOrEditEmployeeLoanExemptionModalComponent>('exemptionModal');

    readonly activeTab = signal<LoanDetailTab>('installments');
    readonly recoveries = signal<EmployeeLoanRecoveryDto[]>([]);
    readonly exemptions = signal<EmployeeLoanExemptionDto[]>([]);
    readonly installments = signal<EmployeeLoanInstallmentDto[]>([]);
    readonly loadingInstallments = signal<boolean>(false);
    readonly loadingRecoveries = signal<boolean>(false);
    readonly loadingExemptions = signal<boolean>(false);

    private shownLoanId: number | undefined;

    constructor() {
        super();
        effect(() => {
            const loanId = this.loanId();

            // Back to the first tab for a different loan -- but not when a new loan has just been saved from one
            // of its own tabs, where the tab in use must stay open.
            if (this.shownLoanId && loanId !== this.shownLoanId) {
                this.activeTab.set('installments');
            }

            this.shownLoanId = loanId;

            if (loanId) {
                this.loadInstallments();
                this.loadRecoveries();
                this.loadExemptions();
            } else {
                this.installments.set([]);
                this.recoveries.set([]);
                this.exemptions.set([]);
            }
        });
    }

    /** The loan to add under: this one, or a new loan saved first. Undefined when that save did not happen. */
    private async resolveLoanId(): Promise<number | undefined> {
        return this.loanId() ?? (await this.ensureLoan()?.());
    }

    // ------------------------------------------------------------------ installments

    loadInstallments(): void {
        const loanId = this.loanId();
        if (!loanId) {
            return;
        }

        this.loadingInstallments.set(true);
        this._installmentsService
            .getAll(
                new GetAllEmployeeLoanInstallmentsInput({
                    employeeLoanId: loanId,
                    sorting: 'StartDate DESC',
                    maxResultCount: 1000,
                    skipCount: 0,
                })
            )
            .pipe(finalize(() => this.loadingInstallments.set(false)))
            .subscribe((result) => this.installments.set(result.items));
    }

    /**
     * Each row ends the day before the next one starts (worked out by the server). The last row is stored with no
     * end (31/12/9999) and in practice runs until the loan is recovered, so it shows the loan's recovery end date.
     */
    installmentEndDate(record: EmployeeLoanInstallmentDto): DateTime | undefined {
        const openEnded = !record.endDate || record.endDate.year >= 9999;
        return openEnded ? this.recoveryEndDate() : record.endDate;
    }

    async createInstallment(): Promise<void> {
        const loanId = await this.resolveLoanId();
        if (loanId) {
            this.installmentModal()?.show(loanId);
        }
    }

    editInstallment(record: EmployeeLoanInstallmentDto): void {
        this.installmentModal()?.show(this.loanId(), record.id);
    }

    deleteInstallment(record: EmployeeLoanInstallmentDto): void {
        this.message.confirm(this.l('AreYouSure'), this.l('AreYouSure'), (isConfirmed) => {
            if (isConfirmed) {
                this._installmentsService.delete(record.id).subscribe(() => {
                    this.notify.success(this.l('SuccessfullyDeleted'));
                    this.onInstallmentSaved();
                });
            }
        });
    }

    onInstallmentSaved(): void {
        this.loadInstallments();
        this.changed.emit();
    }

    // ------------------------------------------------------------------ recoveries

    loadRecoveries(): void {
        const loanId = this.loanId();
        if (!loanId || !this.permission.isGranted('Pages.PersonnelAdministration.EmployeeLoanRecoveries')) {
            return;
        }

        this.loadingRecoveries.set(true);
        this._recoveriesService
            .getAll(
                new GetAllEmployeeLoanRecoveriesInput({
                    employeeLoanId: loanId,
                    sorting: 'RecoveryDate DESC',
                    maxResultCount: 1000,
                    skipCount: 0,
                })
            )
            .pipe(finalize(() => this.loadingRecoveries.set(false)))
            .subscribe((result) => this.recoveries.set(result.items));
    }

    isPayrollRecovery(record: EmployeeLoanRecoveryDto): boolean {
        return record.loanRecoveryMethod === LoanRecoveryMethod.RecoveryThroughPayroll;
    }

    recoveryMethodName(value: LoanRecoveryMethod): string {
        return LoanRecoveryMethod[value] ?? '';
    }

    async createRecovery(): Promise<void> {
        const loanId = await this.resolveLoanId();
        if (loanId) {
            this.recoveryModal()?.show(undefined, { employeeLoanId: loanId, employeeId: this.employeeId() });
        }
    }

    editRecovery(record: EmployeeLoanRecoveryDto): void {
        if (this.isPayrollRecovery(record)) {
            return;
        }

        this.recoveryModal()?.show(record.id, { employeeLoanId: this.loanId(), employeeId: this.employeeId() });
    }

    deleteRecovery(record: EmployeeLoanRecoveryDto): void {
        if (this.isPayrollRecovery(record)) {
            return;
        }

        this.message.confirm(this.l('AreYouSure'), this.l('AreYouSure'), (isConfirmed) => {
            if (isConfirmed) {
                this._recoveriesService.delete(record.id).subscribe(() => {
                    this.notify.success(this.l('SuccessfullyDeleted'));
                    this.onRecoverySaved();
                });
            }
        });
    }

    onRecoverySaved(): void {
        this.loadRecoveries();
        this.changed.emit();
    }

    // ------------------------------------------------------------------ exemptions

    loadExemptions(): void {
        const loanId = this.loanId();
        if (!loanId || !this.permission.isGranted('Pages.PersonnelAdministration.EmployeeLoanExemptions')) {
            return;
        }

        this.loadingExemptions.set(true);
        this._exemptionsService
            .getAll(
                new GetAllEmployeeLoanExemptionsInput({
                    employeeLoanId: loanId,
                    sorting: 'ExemptionDate DESC',
                    maxResultCount: 1000,
                    skipCount: 0,
                })
            )
            .pipe(finalize(() => this.loadingExemptions.set(false)))
            .subscribe((result) => this.exemptions.set(result.items));
    }

    requestStatusLabel(value: RequestStatus | undefined): string {
        return value === undefined || value === null ? '' : this.l('RequestStatus' + RequestStatus[value]);
    }

    async createExemption(): Promise<void> {
        const loanId = await this.resolveLoanId();
        if (loanId) {
            this.exemptionModal()?.show(undefined, { employeeLoanId: loanId, employeeId: this.employeeId() });
        }
    }

    editExemption(record: EmployeeLoanExemptionDto): void {
        this.exemptionModal()?.show(record.id, { employeeLoanId: this.loanId(), employeeId: this.employeeId() });
    }

    deleteExemption(record: EmployeeLoanExemptionDto): void {
        this.message.confirm(this.l('AreYouSure'), this.l('AreYouSure'), (isConfirmed) => {
            if (isConfirmed) {
                this._exemptionsService.delete(record.id).subscribe(() => {
                    this.notify.success(this.l('SuccessfullyDeleted'));
                    this.onExemptionSaved();
                });
            }
        });
    }

    onExemptionSaved(): void {
        this.loadExemptions();
        this.changed.emit();
    }
}
