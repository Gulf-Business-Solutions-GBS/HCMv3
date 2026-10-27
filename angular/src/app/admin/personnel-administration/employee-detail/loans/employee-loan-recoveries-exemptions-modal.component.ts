import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    EmployeeLoanDto,
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
import { ModalDirective } from 'ngx-bootstrap/modal';
import { TableModule } from 'primeng/table';
import { Tooltip } from 'primeng/tooltip';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import { CreateOrEditEmployeeLoanRecoveryModalComponent } from '../../employee-loan-recoveries/create-or-edit-employee-loan-recovery-modal.component';
import { CreateOrEditEmployeeLoanExemptionModalComponent } from '../../employee-loan-exemptions/create-or-edit-employee-loan-exemption-modal.component';
import { CreateOrEditEmployeeLoanInstallmentModalComponent } from './create-or-edit-employee-loan-installment-modal.component';

/**
 * A loan's instalment schedule, recoveries and exemptions, seen from inside the loan -- the way legacy showed
 * them as nested lists on the loan form. Recoveries and exemptions reuse the stand-alone Loan Recoveries / Loan
 * Exemptions dialogs (opened with the loan fixed), so the rules are exactly those of the separate pages; each
 * list shows only to a user who may see that page. The instalment schedule has no page of its own -- it is part
 * of the loan, edited with the loan's Edit permission.
 *
 * Recoveries written by the payroll run (Recovery Through Payroll) are shown but not editable here: the
 * next run of that period deletes and rewrites them, so a hand edit would be silently lost.
 */
@Component({
    selector: 'employeeLoanRecoveriesExemptionsModal',
    templateUrl: './employee-loan-recoveries-exemptions-modal.component.html',
    imports: [
        AppBsModalDirective,
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
export class EmployeeLoanRecoveriesExemptionsModalComponent extends AppComponentBase {
    private _recoveriesService = inject(EmployeeLoanRecoveriesServiceProxy);
    private _exemptionsService = inject(EmployeeLoanExemptionsServiceProxy);
    private _installmentsService = inject(EmployeeLoanInstallmentsServiceProxy);

    readonly modal = viewChild<ModalDirective>('detailsModal');
    readonly installmentModal = viewChild<CreateOrEditEmployeeLoanInstallmentModalComponent>('installmentModal');
    readonly recoveryModal = viewChild<CreateOrEditEmployeeLoanRecoveryModalComponent>('recoveryModal');
    readonly exemptionModal = viewChild<CreateOrEditEmployeeLoanExemptionModalComponent>('exemptionModal');

    /** Raised when anything under the loan changed, so the Loans grid can refresh its balances. */
    readonly changed = output<void>();

    readonly active = signal<boolean>(false);
    readonly loan = signal<EmployeeLoanDto | undefined>(undefined);
    readonly recoveries = signal<EmployeeLoanRecoveryDto[]>([]);
    readonly exemptions = signal<EmployeeLoanExemptionDto[]>([]);
    readonly installments = signal<EmployeeLoanInstallmentDto[]>([]);
    readonly loadingInstallments = signal<boolean>(false);
    readonly loadingRecoveries = signal<boolean>(false);
    readonly loadingExemptions = signal<boolean>(false);

    show(loan: EmployeeLoanDto): void {
        this.loan.set(loan);
        this.active.set(true);
        this.loadInstallments();
        this.loadRecoveries();
        this.loadExemptions();
        this.modal().show();
    }

    close(): void {
        this.active.set(false);
        this.modal().hide();
    }

    // ------------------------------------------------------------------ installments

    loadInstallments(): void {
        const loan = this.loan();
        if (!loan) {
            return;
        }

        this.loadingInstallments.set(true);
        this._installmentsService
            .getAll(
                new GetAllEmployeeLoanInstallmentsInput({
                    employeeLoanId: loan.id,
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
        this.installmentModal()?.show(this.loan().id);
    }

    editInstallment(record: EmployeeLoanInstallmentDto): void {
        this.installmentModal()?.show(this.loan().id, record.id);
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
        const loan = this.loan();
        if (!loan || !this.permission.isGranted('Pages.PersonnelAdministration.EmployeeLoanRecoveries')) {
            return;
        }

        this.loadingRecoveries.set(true);
        this._recoveriesService
            .getAll(
                new GetAllEmployeeLoanRecoveriesInput({
                    employeeLoanId: loan.id,
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
        const loan = this.loan();
        this.recoveryModal()?.show(undefined, { employeeLoanId: loan.id, employeeId: loan.employeeId });
    }

    editRecovery(record: EmployeeLoanRecoveryDto): void {
        if (this.isPayrollRecovery(record)) {
            return;
        }

        const loan = this.loan();
        this.recoveryModal()?.show(record.id, { employeeLoanId: loan.id, employeeId: loan.employeeId });
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
        const loan = this.loan();
        if (!loan || !this.permission.isGranted('Pages.PersonnelAdministration.EmployeeLoanExemptions')) {
            return;
        }

        this.loadingExemptions.set(true);
        this._exemptionsService
            .getAll(
                new GetAllEmployeeLoanExemptionsInput({
                    employeeLoanId: loan.id,
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
        const loan = this.loan();
        this.exemptionModal()?.show(undefined, { employeeLoanId: loan.id, employeeId: loan.employeeId });
    }

    editExemption(record: EmployeeLoanExemptionDto): void {
        const loan = this.loan();
        this.exemptionModal()?.show(record.id, { employeeLoanId: loan.id, employeeId: loan.employeeId });
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
