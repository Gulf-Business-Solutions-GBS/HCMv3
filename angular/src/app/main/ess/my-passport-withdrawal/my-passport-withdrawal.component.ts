import { DateInputDirective } from '@shared/common/dates/date-input.directive';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import {
    EntityDto,
    EssColleagueLookupDto,
    EssLookupServiceProxy,
    GetMyRequestsInput,
    MyDocumentRequestDto,
    MyDocumentServiceProxy,
    MyPassportWithdrawalDto,
    MyPassportWithdrawalServiceProxy,
    RequestPassportWithdrawalInput,
    ReturnDocumentInput,
} from '@shared/service-proxies/service-proxies';
import { DateTime } from 'luxon';
import { Observable } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { SubHeaderComponent } from '../../../shared/common/sub-header/sub-header.component';
import { EssRequestDetailField, EssRequestDetailModalComponent } from '../shared/ess-request-detail-modal.component';
import { MyRequestComponentBase } from '../my-request-component-base';
import { HelpPageDirective } from '@app/shared/help/help-page.directive';

/**
 * Self-service passport (and other document) withdrawals.
 *
 * <p>Two legs, not one. Asking for the document out is a request; handing it back is an amendment against
 * the movement that is already live, which is why the return has its own small form rather than being a
 * field on the withdrawal one -- the return date is not known when the request is raised.</p>
 */
@Component({
    templateUrl: './my-passport-withdrawal.component.html',
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
export class MyPassportWithdrawalComponent extends MyRequestComponentBase<MyPassportWithdrawalDto> implements OnInit {
    private readonly _service = inject(MyPassportWithdrawalServiceProxy);
    private readonly _documentService = inject(MyDocumentServiceProxy);
    private readonly _lookups = inject(EssLookupServiceProxy);

    /** The employee's own documents -- the only things they are allowed to ask for. */
    readonly myDocuments = signal<MyDocumentRequestDto[]>([]);

    /** Colleagues who may be named as guarantor -- the same active-colleague list delegations use. */
    readonly guarantorCandidates = signal<EssColleagueLookupDto[]>([]);

    /** The row whose return is being recorded, if any. Null means the return form is closed. */
    readonly returningRow = signal<MyPassportWithdrawalDto | null>(null);
    readonly returning = signal(false);

    form = {
        employeeDocumentId: null as number | null,
        withdrawDate: '',
        returnDate: '',
        withdrawalReason: '',
        comments: '',
        guarantorId: null as number | null,
    };

    returnForm = { actualReturnDate: '' };

    ngOnInit(): void {
        // Reads the same list the My Documents screen shows. An account without that permission simply gets
        // an empty picker and the explanation below it, rather than an error toast on a screen that
        // otherwise works.
        // The same active-colleague list the delegation screen uses; the server excludes the caller.
        this._lookups.getDelegationCandidates().subscribe({
            next: (result) => this.guarantorCandidates.set(result ?? []),
            error: () => this.guarantorCandidates.set([]),
        });

        this._documentService
            .getMyDocuments(
                new GetMyRequestsInput({ includeHistory: false, maxResultCount: 200, skipCount: 0, sorting: undefined })
            )
            .subscribe({
                next: (result) => this.myDocuments.set(result.items ?? []),
                error: () => this.myDocuments.set([]),
            });

        this.reload();
        this.loadContext();
    }

    protected fetch(input: GetMyRequestsInput): Observable<{ items: MyPassportWithdrawalDto[] }> {
        return this._service.getMyPassportWithdrawals(input);
    }

    protected cancelRequest(input: EntityDto): Observable<void> {
        return this._service.cancelMyPassportWithdrawal(input);
    }

    protected resetForm(): void {
        this.form = {
            employeeDocumentId: null,
            withdrawDate: DateTime.local().toISODate() ?? '',
            returnDate: '',
            withdrawalReason: '',
            comments: '',
            guarantorId: null,
        };

        this.returningRow.set(null);
    }

    submit(): void {
        if (this.saving()) {
            return;
        }

        // Refused here as well as by the server, so the employee is told before the round trip.
        if (!this.form.employeeDocumentId) {
            this.message.warn(this.l('DocumentIsRequired'));
            return;
        }

        if (!this.form.withdrawDate) {
            this.message.warn(this.l('WithdrawDateIsRequired'));
            return;
        }

        if (!this.form.returnDate) {
            this.message.warn(this.l('ReturnDateIsRequired'));
            return;
        }

        if (this.form.returnDate < this.form.withdrawDate) {
            this.message.warn(this.l('ReturnDateCannotBeEarlierThanWithdrawDate'));
            return;
        }

        const input = new RequestPassportWithdrawalInput();
        input.employeeDocumentId = this.form.employeeDocumentId;
        input.withdrawDate = DateTime.fromISO(this.form.withdrawDate);
        input.returnDate = DateTime.fromISO(this.form.returnDate);

        // Legacy asked the employee to name a guarantor on this form and mailed them when the request was
        // raised. Optional here too, so a tenant that does not use guarantors simply leaves it blank.
        input.guarantorId = this.form.guarantorId ?? undefined;

        // Whether this may be left blank is a tenant setting, so the server decides rather than the screen.
        input.withdrawalReason = this.form.withdrawalReason?.trim();
        input.comments = this.form.comments;

        this.saving.set(true);

        this._service
            .requestPassportWithdrawal(input)
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe((result) => this.notifySubmitted(result.submittedForApproval));
    }

    openReturnForm(row: MyPassportWithdrawalDto): void {
        this.returnForm = { actualReturnDate: DateTime.local().toISODate() ?? '' };
        this.returningRow.set(row);
        this.closeForm();
    }

    closeReturnForm(): void {
        this.returningRow.set(null);
    }

    submitReturn(): void {
        const row = this.returningRow();

        if (!row || this.returning()) {
            return;
        }

        if (!this.returnForm.actualReturnDate) {
            this.message.warn(this.l('ActualReturnDateIsRequired'));
            return;
        }

        const withdrawDate = row.withdrawDate?.toISODate();

        if (withdrawDate && this.returnForm.actualReturnDate < withdrawDate) {
            this.message.warn(this.l('ActualReturnDateCannotBeEarlierThanWithdrawDate'));
            return;
        }

        const input = new ReturnDocumentInput();
        input.employeeDocumentMvmtId = row.id;
        input.actualReturnDate = DateTime.fromISO(this.returnForm.actualReturnDate);

        this.returning.set(true);

        this._service
            .returnMyDocument(input)
            .pipe(finalize(() => this.returning.set(false)))
            .subscribe(() => {
                // Always an approval request, never an immediate stamp: the document is not recorded as back
                // until somebody agrees, so saying "submitted" here is the honest message.
                this.notify.success(this.l('RequestSubmittedForApproval'));
                this.closeReturnForm();
                this.reload();
            });
    }

    documentLabel(document: MyDocumentRequestDto): string {
        const type = this.pick(document.documentTypeNameEn, document.documentTypeNameAr);

        return document.documentNum ? type + ' - ' + document.documentNum : type;
    }

    typeName(row: MyPassportWithdrawalDto): string {
        return this.pick(row.documentTypeNameEn, row.documentTypeNameAr);
    }

    colleagueName(colleague: EssColleagueLookupDto): string {
        return `${colleague.code} - ${this.pick(colleague.fullNameEn, colleague.fullNameAr)}`;
    }

    guarantorName(row: MyPassportWithdrawalDto): string {
        return this.pick(row.guarantorNameEn, row.guarantorNameAr);
    }

    protected requestType(): string {
        return 'EmployeeDocumentMvmt';
    }

    protected detailTitle(): string {
        return this.l('EssMyPassportWithdrawals');
    }

    protected detailFields(row: MyPassportWithdrawalDto): EssRequestDetailField[] {
        return [
            { label: this.l('Employee'), value: this.employeeDisplay() },
            { label: this.l('DocumentType'), value: this.typeName(row) },
            { label: this.l('DocumentNumber'), value: row.documentNum ?? '' },
            { label: this.l('WithdrawDate'), value: this.detailDate(row.withdrawDate) },
            { label: this.l('ReturnDate'), value: this.detailDate(row.returnDate) },
            { label: this.l('ActualReturnDate'), value: this.detailDate(row.actualReturnDate) },
            { label: this.l('Guarantor'), value: this.guarantorName(row) },
        ];
    }
}
