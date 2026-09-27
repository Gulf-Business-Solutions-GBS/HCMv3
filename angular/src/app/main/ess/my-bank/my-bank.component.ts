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
    MyBankRequestDto,
    MyBankServiceProxy,
    RequestBankChangeInput,
    SimpleLookupDto,
} from '@shared/service-proxies/service-proxies';
import { DateTime } from 'luxon';
import { Observable } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { SubHeaderComponent } from '../../../shared/common/sub-header/sub-header.component';
import { EssRequestDetailField, EssRequestDetailModalComponent } from '../shared/ess-request-detail-modal.component';
import { MyRequestComponentBase } from '../my-request-component-base';
import { HelpPageDirective } from '@app/shared/help/help-page.directive';

/**
 * Self-service bank detail changes.
 *
 * <p>The list shows masked account numbers because that is all the server sends. An employee already knows
 * their own number, and a list screen is the easiest place for one to end up in a screenshot or a support
 * ticket.</p>
 */
@Component({
    templateUrl: './my-bank.component.html',
    animations: [appModuleAnimation()],
    imports: [
        DateInputDirective,
        CommonModule,
        FormsModule,
        SubHeaderComponent,
        LocalizePipe,
        LuxonFormatPipe,
        EssRequestDetailModalComponent,
        HelpPageDirective,
    ],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyBankComponent extends MyRequestComponentBase<MyBankRequestDto> implements OnInit {
    private readonly _service = inject(MyBankServiceProxy);
    private readonly _lookups = inject(EssLookupServiceProxy);

    readonly banks = signal<SimpleLookupDto[]>([]);

    form = { bankId: null as number | null, accountNum: '', iban: '', bankBranch: '', startDate: '' };

    ngOnInit(): void {
        this._lookups.getBanks().subscribe((result) => this.banks.set(result ?? []));

        this.reload();
        this.loadContext();
    }

    protected fetch(input: GetMyRequestsInput): Observable<{ items: MyBankRequestDto[] }> {
        return this._service.getMyBanks(input);
    }

    protected cancelRequest(input: EntityDto): Observable<void> {
        return this._service.cancelMyBank(input);
    }

    protected resetForm(): void {
        this.form = { bankId: null, accountNum: '', iban: '', bankBranch: '', startDate: '' };
    }

    submit(): void {
        if (!this.form.startDate || this.saving()) {
            return;
        }

        // One or the other. Some countries pay on an account number, others on IBAN; requiring both would
        // block legitimate changes, requiring neither would send a payment nowhere.
        if (!this.form.accountNum?.trim() && !this.form.iban?.trim()) {
            this.message.warn(this.l('AccountNumberOrIbanIsRequired'));
            return;
        }

        const input = new RequestBankChangeInput();
        input.bankId = this.form.bankId ?? undefined;
        input.accountNum = this.form.accountNum?.trim();
        input.iban = this.form.iban?.trim();
        input.bankBranch = this.form.bankBranch;
        input.startDate = DateTime.fromISO(this.form.startDate);

        this.saving.set(true);

        this._service
            .requestBankChange(input)
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe((result) => this.notifySubmitted(result.submittedForApproval));
    }

    bankName(row: MyBankRequestDto): string {
        return this.pick(row.bankNameEn, row.bankNameAr);
    }

    protected requestType(): string {
        return 'EmployeeBank';
    }

    protected detailTitle(): string {
        return this.l('EssMyBank');
    }

    protected detailFields(row: MyBankRequestDto): EssRequestDetailField[] {
        return [
            { label: this.l('Employee'), value: this.employeeDisplay() },
            { label: this.l('Bank'), value: this.bankName(row) },
            { label: this.l('AccountNumber'), value: row.accountNumMasked ?? '' },
            { label: this.l('Iban'), value: row.ibanMasked ?? '' },
            { label: this.l('StartDate'), value: this.detailDate(row.startDate) },
        ];
    }
}
