import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CompanyInsuranceDetailDto,
    CompanyInsuranceDetailsServiceProxy,
    EmployeeInsuranceDetailDto,
    EmployeeInsuranceDetailsServiceProxy,
    EmployeeInsuranceDto,
    GetAllCompanyInsuranceDetailsInput,
    GetAllEmployeeInsuranceDetailsInput,
    InstallmentType,
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
import { CreateOrEditEmployeeInsuranceDetailModalComponent } from '../../../benefits-management/employee-insurance-details/create-or-edit-employee-insurance-detail-modal.component';
import { CreateOrEditCompanyInsuranceDetailModalComponent } from '../../../benefits-management/company-insurance-details/create-or-edit-company-insurance-detail-modal.component';

/**
 * An insurance's installments, seen from inside the insurance -- legacy nested the employee share
 * (EmployeeInsuranceDetail) and the company share (CompanyInsuranceDetail) on the insurance form. It
 * reuses the stand-alone Employee / Company Insurance Details dialogs (opened with the insurance fixed),
 * so the rules are exactly those of the separate pages.
 *
 * Installments written by the payroll run (Through Payroll) are shown but not editable here: the next
 * run of that period deletes and rewrites them, so a hand edit would be silently lost.
 */
@Component({
    selector: 'employeeInsuranceInstallmentsModal',
    templateUrl: './employee-insurance-installments-modal.component.html',
    imports: [
        AppBsModalDirective,
        BusyIfDirective,
        TableModule,
        Tooltip,
        LocalizePipe,
        PermissionPipe,
        LuxonFormatPipe,
        CreateOrEditEmployeeInsuranceDetailModalComponent,
        CreateOrEditCompanyInsuranceDetailModalComponent,
    ],
})
export class EmployeeInsuranceInstallmentsModalComponent extends AppComponentBase {
    private _employeeDetailsService = inject(EmployeeInsuranceDetailsServiceProxy);
    private _companyDetailsService = inject(CompanyInsuranceDetailsServiceProxy);

    readonly modal = viewChild<ModalDirective>('installmentsModal');
    readonly employeeDetailModal = viewChild<CreateOrEditEmployeeInsuranceDetailModalComponent>('employeeDetailModal');
    readonly companyDetailModal = viewChild<CreateOrEditCompanyInsuranceDetailModalComponent>('companyDetailModal');

    /** Raised when an installment changed, so the Insurances grid can refresh its balances. */
    readonly changed = output<void>();

    readonly active = signal<boolean>(false);
    readonly insurance = signal<EmployeeInsuranceDto | undefined>(undefined);
    readonly employeeDetails = signal<EmployeeInsuranceDetailDto[]>([]);
    readonly companyDetails = signal<CompanyInsuranceDetailDto[]>([]);
    readonly loadingEmployeeDetails = signal<boolean>(false);
    readonly loadingCompanyDetails = signal<boolean>(false);

    show(insurance: EmployeeInsuranceDto): void {
        this.insurance.set(insurance);
        this.active.set(true);
        this.loadEmployeeDetails();
        this.loadCompanyDetails();
        this.modal().show();
    }

    close(): void {
        this.active.set(false);
        this.modal().hide();
    }

    isPayrollInstallment(record: { installmentType: InstallmentType }): boolean {
        return record.installmentType === InstallmentType.ThroughPayroll;
    }

    installmentTypeName(value: InstallmentType): string {
        return InstallmentType[value] ?? '';
    }

    private context(): { employeeInsuranceId: number; employeeId: number } {
        const insurance = this.insurance();
        return { employeeInsuranceId: insurance.id, employeeId: insurance.employeeId };
    }

    // ------------------------------------------------------------------ employee share

    loadEmployeeDetails(): void {
        const insurance = this.insurance();
        if (!insurance) {
            return;
        }

        this.loadingEmployeeDetails.set(true);
        this._employeeDetailsService
            .getAll(
                new GetAllEmployeeInsuranceDetailsInput({
                    employeeInsuranceId: insurance.id,
                    sorting: 'InstallmentDate DESC',
                    maxResultCount: 1000,
                    skipCount: 0,
                })
            )
            .pipe(finalize(() => this.loadingEmployeeDetails.set(false)))
            .subscribe((result) => this.employeeDetails.set(result.items));
    }

    createEmployeeDetail(): void {
        this.employeeDetailModal()?.show(undefined, this.context());
    }

    editEmployeeDetail(record: EmployeeInsuranceDetailDto): void {
        if (this.isPayrollInstallment(record)) {
            return;
        }

        this.employeeDetailModal()?.show(record.id, this.context());
    }

    deleteEmployeeDetail(record: EmployeeInsuranceDetailDto): void {
        if (this.isPayrollInstallment(record)) {
            return;
        }

        this.message.confirm(this.l('AreYouSure'), this.l('AreYouSure'), (isConfirmed) => {
            if (isConfirmed) {
                this._employeeDetailsService.delete(record.id).subscribe(() => {
                    this.notify.success(this.l('SuccessfullyDeleted'));
                    this.onEmployeeDetailSaved();
                });
            }
        });
    }

    onEmployeeDetailSaved(): void {
        this.loadEmployeeDetails();
        this.changed.emit();
    }

    // ------------------------------------------------------------------ company share

    loadCompanyDetails(): void {
        const insurance = this.insurance();
        if (!insurance) {
            return;
        }

        this.loadingCompanyDetails.set(true);
        this._companyDetailsService
            .getAll(
                new GetAllCompanyInsuranceDetailsInput({
                    employeeInsuranceId: insurance.id,
                    sorting: 'InstallmentDate DESC',
                    maxResultCount: 1000,
                    skipCount: 0,
                })
            )
            .pipe(finalize(() => this.loadingCompanyDetails.set(false)))
            .subscribe((result) => this.companyDetails.set(result.items));
    }

    createCompanyDetail(): void {
        this.companyDetailModal()?.show(undefined, this.context());
    }

    editCompanyDetail(record: CompanyInsuranceDetailDto): void {
        if (this.isPayrollInstallment(record)) {
            return;
        }

        this.companyDetailModal()?.show(record.id, this.context());
    }

    deleteCompanyDetail(record: CompanyInsuranceDetailDto): void {
        if (this.isPayrollInstallment(record)) {
            return;
        }

        this.message.confirm(this.l('AreYouSure'), this.l('AreYouSure'), (isConfirmed) => {
            if (isConfirmed) {
                this._companyDetailsService.delete(record.id).subscribe(() => {
                    this.notify.success(this.l('SuccessfullyDeleted'));
                    this.onCompanyDetailSaved();
                });
            }
        });
    }

    onCompanyDetailSaved(): void {
        this.loadCompanyDetails();
        this.changed.emit();
    }
}
