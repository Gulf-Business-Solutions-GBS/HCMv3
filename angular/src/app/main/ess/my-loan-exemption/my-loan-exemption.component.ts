import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import {
    EntityDto,
    GetMyRequestsInput,
    MyLoanExemptionDto,
    MyLoanExemptionServiceProxy,
    MyLoanForExemptionDto,
    RequestLoanExemptionInput,
} from '@shared/service-proxies/service-proxies';
import { DateTime } from 'luxon';
import { Observable } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { SubHeaderComponent } from '../../../shared/common/sub-header/sub-header.component';
import { WorkflowHistoryModalComponent } from '../../../shared/common/workflow-activity/workflow-history-modal.component';
import { MyRequestComponentBase } from '../my-request-component-base';
import { HelpPageDirective } from '@app/shared/help/help-page.directive';
import { DateInputDirective } from '@shared/common/dates/date-input.directive';

/**
 * Self-service loan recovery exemptions: skipping one month's instalment on a loan the employee already
 * holds.
 *
 * <p>The exemption is scoped to a calendar month, so the form asks for a month rather than a day and sends
 * the first of it. The picked loan's instalment and recovery window are shown alongside, because what the
 * employee is actually asking to defer is a specific sum on a schedule they should be able to see.</p>
 */
@Component({
    templateUrl: './my-loan-exemption.component.html',
    animations: [appModuleAnimation()],
    imports: [
        DateInputDirective,
        CommonModule,
        FormsModule,
        SubHeaderComponent,
        LocalizePipe,
        LuxonFormatPipe,
        WorkflowHistoryModalComponent,
        HelpPageDirective,
    ],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyLoanExemptionComponent extends MyRequestComponentBase<MyLoanExemptionDto> implements OnInit {
    private readonly _service = inject(MyLoanExemptionServiceProxy);

    readonly loans = signal<MyLoanForExemptionDto[]>([]);

    readonly selectedLoanId = signal<number | null>(null);

    /** The picked loan, so the form can show what a single exemption defers and the window it must fall in. */
    readonly selectedLoan = computed(
        () => this.loans().find((loan) => loan.employeeLoanId === this.selectedLoanId()) ?? null
    );

    /** 'yyyy-MM' from an <input type="month">; the month is the unit an exemption is granted in. */
    form = { exemptionMonth: '', comments: '' };

    ngOnInit(): void {
        this.loadLoans();
        this.reload();
    }

    protected fetch(input: GetMyRequestsInput): Observable<{ items: MyLoanExemptionDto[] }> {
        return this._service.getMyLoanExemptions(input);
    }

    protected cancelRequest(input: EntityDto): Observable<void> {
        return this._service.cancelMyLoanExemption(input);
    }

    protected resetForm(): void {
        this.selectedLoanId.set(null);
        this.form = { exemptionMonth: '', comments: '' };
    }

    submit(): void {
        if (this.saving()) {
            return;
        }

        const loanId = this.selectedLoanId();

        if (!loanId) {
            this.message.warn(this.l('PleaseSelectALoan'));
            return;
        }

        if (!this.form.exemptionMonth) {
            this.message.warn(this.l('ExemptionMonthIsRequired'));
            return;
        }

        // Any date inside the month is accepted by the server; the first is sent so the value is
        // unambiguous. Nothing about a month is determined by which day of it was picked.
        const exemptionDate = DateTime.fromISO(`${this.form.exemptionMonth}-01`);

        if (!exemptionDate.isValid) {
            this.message.warn(this.l('ExemptionMonthIsRequired'));
            return;
        }

        // The recovery window is checked here as well as by the server: an exemption outside it would be
        // recorded and then quietly defer nothing, which is worse than being refused.
        const loan = this.selectedLoan();

        if (
            loan &&
            (exemptionDate.endOf('month') < loan.recoveryStartDate || exemptionDate.startOf('month') > loan.endDate)
        ) {
            this.message.warn(this.l('ExemptionMonthIsOutsideTheRecoveryPeriod'));
            return;
        }

        const input = new RequestLoanExemptionInput();
        input.employeeLoanId = loanId;
        input.exemptionDate = exemptionDate;
        input.comments = this.form.comments;

        this.saving.set(true);

        this._service
            .requestLoanExemption(input)
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe((result) => this.notifySubmitted(result.submittedForApproval));
    }

    loanLabel(loan: MyLoanForExemptionDto): string {
        return this.pick(loan.loanTypeNameEn, loan.loanTypeNameAr);
    }

    typeName(row: MyLoanExemptionDto): string {
        return this.pick(row.loanTypeNameEn, row.loanTypeNameAr);
    }

    private loadLoans(): void {
        // Only loans still in recovery come back; a settled loan has no instalment left to skip.
        this._service.getMyLoansForExemption().subscribe({
            next: (result) => this.loans.set(result ?? []),
            error: () => this.loans.set([]),
        });
    }
}
