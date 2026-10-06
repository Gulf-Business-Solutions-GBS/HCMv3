import { DateInputDirective } from '@shared/common/dates/date-input.directive';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import {
    EntityDto,
    EssLookupServiceProxy,
    GetMyRequestsInput,
    MyLoanRequestDto,
    MyLoanServiceProxy,
    RequestLoanInput,
    SimpleLookupDto,
} from '@shared/service-proxies/service-proxies';
import { DateTime } from 'luxon';
import { Observable } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { SubHeaderComponent } from '../../../shared/common/sub-header/sub-header.component';
import { EssRequestDetailField, EssRequestDetailModalComponent } from '../shared/ess-request-detail-modal.component';
import { EssList } from '../shared/ess-list';
import { EssListHeadComponent } from '../shared/ess-list-head.component';
import { EssListPagerComponent } from '../shared/ess-list-pager.component';
import { MyRequestComponentBase } from '../my-request-component-base';
import { HelpPageDirective } from '@app/shared/help/help-page.directive';

/** Self-service loan requests. */
@Component({
    templateUrl: './my-loan.component.html',
    animations: [appModuleAnimation()],
    imports: [
        DateInputDirective,
        CommonModule,
        FormsModule,
        SubHeaderComponent,
        LocalizePipe,
        LuxonFormatPipe,
        EssRequestDetailModalComponent,
        EssListHeadComponent,
        EssListPagerComponent,
        HelpPageDirective,
    ],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyLoanComponent extends MyRequestComponentBase<MyLoanRequestDto> implements OnInit {
    private readonly _service = inject(MyLoanServiceProxy);
    private readonly _lookups = inject(EssLookupServiceProxy);

    readonly loanTypes = signal<SimpleLookupDto[]>([]);

    /** The list's columns, for the shared heading (sorting), filter row and pager. */
    readonly list = new EssList<MyLoanRequestDto>(this.rows, () => [
        { key: 'loanType', label: 'LoanType', text: (row) => this.typeName(row), filter: 'select' },
        { key: 'amount', label: 'LoanAmount', text: (row) => row.actualAmount },
        { key: 'instalment', label: 'Instalment', text: (row) => row.fixedAmount },
        { key: 'startDate', label: 'StartDate', text: (row) => this.detailDate(row.startDate), sort: (row) => row.startDate?.toMillis() },
        { key: 'endDate', label: 'EndDate', text: (row) => this.detailDate(row.endDate), sort: (row) => row.endDate?.toMillis() },
        { key: 'status', label: 'Status', text: (row) => this.statusLabel(row), filter: 'select' },
    ]);

    form = {
        loanTypeId: null as number | null,
        actualAmount: null as number | null,
        fixedAmount: null as number | null,
        startDate: '',
        recoveryStartDate: '',
        endDate: '',
        comments: '',
    };

    ngOnInit(): void {
        this._lookups.getLoanTypes().subscribe((result) => this.loanTypes.set(result ?? []));

        this.reload();
        this.loadContext();
    }

    protected fetch(input: GetMyRequestsInput): Observable<{ items: MyLoanRequestDto[] }> {
        return this._service.getMyLoans(input);
    }

    protected cancelRequest(input: EntityDto): Observable<void> {
        return this._service.cancelMyLoan(input);
    }

    protected resetForm(): void {
        this.form = {
            loanTypeId: null,
            actualAmount: null,
            fixedAmount: null,
            startDate: '',
            recoveryStartDate: '',
            endDate: '',
            comments: '',
        };
    }

    submit(): void {
        if (!this.form.loanTypeId || !this.form.startDate || !this.form.endDate || this.saving()) {
            return;
        }

        if (!this.form.actualAmount || this.form.actualAmount <= 0) {
            this.message.warn(this.l('LoanAmountMustBeGreaterThanZero'));
            return;
        }

        const input = new RequestLoanInput();
        input.loanTypeId = this.form.loanTypeId;
        input.actualAmount = this.form.actualAmount;
        input.fixedAmount = this.form.fixedAmount ?? 0;
        input.startDate = DateTime.fromISO(this.form.startDate);

        // Deductions default to starting with the loan when the employee does not say otherwise, which is
        // the common case and saves a field they would otherwise have to reason about.
        input.recoveryStartDate = DateTime.fromISO(this.form.recoveryStartDate || this.form.startDate);

        input.endDate = DateTime.fromISO(this.form.endDate);
        input.comments = this.form.comments;

        this.saving.set(true);

        this._service
            .requestLoan(input)
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe((result) => this.notifySubmitted(result.submittedForApproval));
    }

    typeName(row: MyLoanRequestDto): string {
        return this.pick(row.loanTypeNameEn, row.loanTypeNameAr);
    }

    protected requestType(): string {
        return 'EmployeeLoan';
    }

    protected detailTitle(): string {
        return this.l('EssMyLoans');
    }

    protected detailFields(row: MyLoanRequestDto): EssRequestDetailField[] {
        return [
            { label: this.l('Employee'), value: this.employeeDisplay() },
            { label: this.l('LoanType'), value: this.typeName(row) },
            { label: this.l('LoanAmount'), value: `${row.actualAmount ?? ''}` },
            { label: this.l('Instalment'), value: `${row.fixedAmount ?? ''}` },
            { label: this.l('StartDate'), value: this.detailDate(row.startDate) },
            { label: this.l('EndDate'), value: this.detailDate(row.endDate) },
        ];
    }
}
