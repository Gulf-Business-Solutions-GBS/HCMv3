import { DateInputDirective } from '@shared/common/dates/date-input.directive';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import {
    EntityDto,
    GetMyHealthReimburseBalanceInput,
    GetMyRequestsInput,
    MyHealthReimburseBalanceDto,
    MyHealthReimburseDto,
    MyHealthReimburseServiceProxy,
    RequestHealthReimburseInput,
} from '@shared/service-proxies/service-proxies';
import { DateTime } from 'luxon';
import { Observable, of } from 'rxjs';
import { catchError, finalize } from 'rxjs/operators';
import { SubHeaderComponent } from '../../../shared/common/sub-header/sub-header.component';
import { EssRequestDetailField, EssRequestDetailModalComponent } from '../shared/ess-request-detail-modal.component';
import { EssList } from '../shared/ess-list';
import { EssListHeadComponent } from '../shared/ess-list-head.component';
import { EssListPagerComponent } from '../shared/ess-list-pager.component';
import { MyRequestComponentBase } from '../my-request-component-base';
import { HelpPageDirective } from '@app/shared/help/help-page.directive';

/**
 * Self-service medical expense claims.
 *
 * <p>The balance panel is the point of the screen rather than decoration: what constrains a claim is the
 * employee's remaining entitlement, so showing the figures they are about to spend against -- and refusing
 * an over-claim before the round trip -- is what stops the form from being a guess.</p>
 */
@Component({
    templateUrl: './my-health-reimburse.component.html',
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
export class MyHealthReimburseComponent extends MyRequestComponentBase<MyHealthReimburseDto> implements OnInit {
    private readonly _service = inject(MyHealthReimburseServiceProxy);

    /** The list's columns, for the shared heading (sorting), filter row and pager. */
    readonly list = new EssList<MyHealthReimburseDto>(this.rows, () => [
        { key: 'paymentDate', label: 'PaymentDate', text: (row) => this.detailDate(row.paymentDate), sort: (row) => row.paymentDate?.toMillis() },
        { key: 'amount', label: 'Amount', text: (row) => row.amount },
        { key: 'comments', label: 'Comments', text: (row) => row.comments },
        { key: 'status', label: 'Status', text: (row) => this.statusLabel(row), filter: 'select' },
    ]);

    readonly balance = signal<MyHealthReimburseBalanceDto | undefined>(undefined);
    readonly balanceLoading = signal(false);

    form = { paymentDate: '', amount: null as number | null, comments: '' };

    ngOnInit(): void {
        this.loadBalance();
        this.reload();
        this.loadContext();
    }

    protected fetch(input: GetMyRequestsInput): Observable<{ items: MyHealthReimburseDto[] }> {
        return this._service.getMyHealthReimburses(input);
    }

    protected cancelRequest(input: EntityDto): Observable<void> {
        return this._service.cancelMyHealthReimburse(input);
    }

    protected resetForm(): void {
        this.form = { paymentDate: '', amount: null, comments: '' };
    }

    /**
     * The payment date decides which entitlement year the claim comes out of, so the panel follows the date
     * on the form rather than always reporting today's year.
     */
    loadBalance(): void {
        const asOf = this.form.paymentDate ? DateTime.fromISO(this.form.paymentDate) : (undefined as any);

        this.balanceLoading.set(true);

        this._service
            .getMyHealthReimburseBalance(new GetMyHealthReimburseBalanceInput({ asOf }))
            // An account with no employee record is handled by the page as a whole; the panel simply has
            // nothing to say rather than throwing a second error toast on top of that one.
            .pipe(
                catchError(() => of(undefined)),
                finalize(() => this.balanceLoading.set(false))
            )
            .subscribe((result) => this.balance.set(result));
    }

    onPaymentDateChanged(): void {
        this.loadBalance();
    }

    submit(): void {
        if (this.saving()) {
            return;
        }

        if (!this.form.paymentDate) {
            this.message.warn(this.l('PaymentDateIsRequired'));
            return;
        }

        if (!this.form.amount || this.form.amount <= 0) {
            this.message.warn(this.l('ClaimAmountMustBeGreaterThanZero'));
            return;
        }

        // The server owns this rule and re-checks it; refusing here means the employee is told what is left
        // before the claim disappears into a submission that can only fail.
        const claimable = this.balance()?.claimable;

        if (claimable !== undefined && claimable !== null && this.form.amount > claimable) {
            this.message.warn(this.l('ClaimExceedsRemainingHealthReimburseBalance', claimable));
            return;
        }

        const input = new RequestHealthReimburseInput();
        input.paymentDate = DateTime.fromISO(this.form.paymentDate);
        input.amount = this.form.amount;
        input.comments = this.form.comments;

        this.saving.set(true);

        this._service
            .requestHealthReimburse(input)
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe((result) => {
                this.notifySubmitted(result.submittedForApproval);
                this.loadBalance();
            });
    }

    subGroupName(): string {
        const balance = this.balance();

        return this.pick(balance?.employeeSubGroupNameEn, balance?.employeeSubGroupNameAr);
    }

    protected requestType(): string {
        return 'EmployeeHealthReimburse';
    }

    protected detailTitle(): string {
        return this.l('EssMyHealthReimburses');
    }

    protected detailFields(row: MyHealthReimburseDto): EssRequestDetailField[] {
        return [
            { label: this.l('Employee'), value: this.employeeDisplay() },
            { label: this.l('PaymentDate'), value: this.detailDate(row.paymentDate) },
            { label: this.l('Amount'), value: `${row.amount ?? ''}` },
        ];
    }
}
