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

    constructor() {
        super();
        effect(() => {
            if (this.loanId()) {
                this.activeTab.set('installments');
                this.loadInstallments();
                this.loadRecoveries();
                this.loadExemptions();
            }
        });
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

    /** The last row runs on with no end (31/12/9999); shown blank rather than as a date nobody means. */
    isOpenEnded(record: EmployeeLoanInstallmentDto): boolean {
        return !record.endDate || record.endDate.year >= 9999;
    }

    createInstallment(): void {
        this.installmentModal()?.show(this.loanId());
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

    createRecovery(): void {
        this.recoveryModal()?.show(undefined, { employeeLoanId: this.loanId(), employeeId: this.employeeId() });
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

    createExemption(): void {
        this.exemptionModal()?.show(undefined, { employeeLoanId: this.loanId(), employeeId: this.employeeId() });
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
