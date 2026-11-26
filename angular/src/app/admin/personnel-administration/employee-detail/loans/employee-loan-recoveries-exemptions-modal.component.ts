import { Component, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { EmployeeLoanDto } from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import { EmployeeLoanDetailsComponent } from './employee-loan-details.component';

/**
 * A loan's instalments, recoveries and exemptions on their own, from a loan list's Actions menu. The same tabs
 * appear inside the loan's Edit dialog; this is for someone who wants them without opening the loan for edit.
 */
@Component({
    selector: 'employeeLoanRecoveriesExemptionsModal',
    templateUrl: './employee-loan-recoveries-exemptions-modal.component.html',
    imports: [AppBsModalDirective, LocalizePipe, LuxonFormatPipe, EmployeeLoanDetailsComponent],
})
export class EmployeeLoanRecoveriesExemptionsModalComponent extends AppComponentBase {
    readonly modal = viewChild<ModalDirective>('detailsModal');

    /** Raised when anything under the loan changed, so the Loans grid can refresh its balances. */
    readonly changed = output<void>();

    readonly active = signal<boolean>(false);
    readonly loan = signal<EmployeeLoanDto | undefined>(undefined);

    show(loan: EmployeeLoanDto): void {
        this.loan.set(loan);
        this.active.set(true);
        this.modal().show();
    }

    close(): void {
        this.active.set(false);
        this.modal().hide();
    }
}
