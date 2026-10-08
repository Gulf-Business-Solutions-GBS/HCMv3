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
    EssWithdrawableDocumentDto,
    GetMyRequestsInput,
    MyPassportWithdrawalDto,
    MyPassportWithdrawalServiceProxy,
    RequestPassportWithdrawalInput,
    RequestStatus,
    ReturnDocumentInput,
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
        EssListHeadComponent,
        EssListPagerComponent,
        HelpPageDirective,
    ],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyPassportWithdrawalComponent extends MyRequestComponentBase<MyPassportWithdrawalDto> implements OnInit {
    private readonly _service = inject(MyPassportWithdrawalServiceProxy);
    private readonly _lookups = inject(EssLookupServiceProxy);

    /** The documents the employee may withdraw -- the server decides which, so the picker offers nothing else. */
    readonly myDocuments = signal<EssWithdrawableDocumentDto[]>([]);

    /** Colleagues who may be named as guarantor -- the same active-colleague list delegations use. */
    readonly guarantorCandidates = signal<EssColleagueLookupDto[]>([]);

    /** The row whose return is being recorded, if any. Null means the return form is closed. */
    readonly returningRow = signal<MyPassportWithdrawalDto | null>(null);
    readonly returning = signal(false);

    /** The list's columns, for the shared heading (sorting), filter row and pager. */
    readonly list = new EssList<MyPassportWithdrawalDto>(this.rows, () => [
        {
            key: 'requestNumber',
            label: 'RequestNumber',
            text: (row) => row.requestNumber,

            // As a number where it is one, so 9 sorts before 10. A draft has no number yet and sorts as the
            // newest; a movement HR recorded was never requested and sorts below everything that was.
            sort: (row) => (row.requestNumber ? Number(row.requestNumber) || row.requestNumber : row.isDraft ? Number.MAX_SAFE_INTEGER : 0),
        },
        { key: 'documentType', label: 'DocumentType', text: (row) => this.typeName(row), filter: 'select' },
        { key: 'documentNumber', label: 'DocumentNumber', text: (row) => row.documentNum },
        { key: 'withdrawDate', label: 'WithdrawDate', text: (row) => this.detailDate(row.withdrawDate), sort: (row) => row.withdrawDate?.toMillis() },
        {
            key: 'actualWithdrawDate',
            label: 'HandedOverOn',
            text: (row) => this.detailDate(row.actualWithdrawDate),
            sort: (row) => row.actualWithdrawDate?.toMillis(),
        },
        { key: 'returnDate', label: 'ReturnDate', text: (row) => this.detailDate(row.returnDate), sort: (row) => row.returnDate?.toMillis() },
        {
            key: 'actualReturnDate',
            label: 'ActualReturnDate',
            text: (row) => (row.actualReturnDate ? this.detailDate(row.actualReturnDate) : this.l(row.isOverdue ? 'Overdue' : 'StillOut')),
            sort: (row) => row.actualReturnDate?.toMillis(),
        },
        { key: 'guarantor', label: 'Guarantor', text: (row) => this.guarantorName(row) },
        { key: 'status', label: 'Status', text: (row) => this.statusLabel(row), filter: 'select' },
    ]);

    form = {
        employeeDocumentId: null as number | null,
        withdrawDate: '',
        returnDate: '',
        withdrawalReason: '',
        comments: '',
        guarantorId: null as number | null,
    };

    returnForm = { actualReturnDate: '', returnRemarks: '' };

    ngOnInit(): void {
        // The same active-colleague list the delegation screen uses; the server excludes the caller.
        this._lookups.getDelegationCandidates().subscribe({
            next: (result) => this.guarantorCandidates.set(result ?? []),
            error: () => this.guarantorCandidates.set([]),
        });

        // Only what the employee may withdraw, which is narrower than everything HR holds for them. A failure
        // leaves an empty picker and the explanation below it, rather than an error toast on a screen that
        // otherwise works.
        this._lookups.getMyWithdrawableDocuments().subscribe({
            next: (result) => {
                const documents = result ?? [];

                // The form may already be open by the time the list arrives; a lone document is chosen for
                // it then, exactly as it is when the form opens afterwards.
                if (documents.length === 1 && this.showForm() && !this.form.employeeDocumentId) {
                    this.form.employeeDocumentId = documents[0].id;
                }

                this.myDocuments.set(documents);
            },
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
        const documents = this.myDocuments();

        this.form = {
            // Nothing to choose between when there is only one.
            employeeDocumentId: documents.length === 1 ? documents[0].id : null,
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
        this.returnForm = { actualReturnDate: this.todayIso(), returnRemarks: '' };
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
        input.returnRemarks = this.returnForm.returnRemarks?.trim() || undefined;

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

    /** Today as the date fields hold it, for the upper bound on a return date: the server refuses a future one. */
    todayIso(): string {
        return DateTime.local().toISODate() ?? '';
    }

    /** The document chosen on the withdrawal form, for the read-only fields beside the picker. */
    selectedDocument(): EssWithdrawableDocumentDto | undefined {
        const id = this.form.employeeDocumentId;

        return id ? this.myDocuments().find((document) => document.id === id) : undefined;
    }

    /**
     * True when the chosen document is already with the employee: an approved withdrawal of it that has not
     * come back. Asking again is then an extension, and the form says so before it is sent.
     */
    selectedDocumentIsOut(): boolean {
        const id = this.form.employeeDocumentId;

        return (
            !!id &&
            this.rows().some((row) => row.employeeDocumentId === id && row.workflowStatus === RequestStatus.Approved && !row.actualReturnDate)
        );
    }

    /** A date as the form's own date fields show it. Empty rather than "Invalid DateTime" when unset. */
    formDate(value: DateTime | undefined): string {
        return value?.toFormat('dd/MM/yyyy') ?? '';
    }

    /** "2026-10-12 (Request 1042), ..." -- the due dates this movement had before it was extended. */
    private earlierDueDates(row: MyPassportWithdrawalDto): string {
        return (row.extensions ?? [])
            .map((extension) => {
                const date = this.detailDate(extension.returnDate);

                return extension.requestNumber ? `${date} (${this.l('Request')} ${extension.requestNumber})` : date;
            })
            .join(', ');
    }

    documentLabel(document: EssWithdrawableDocumentDto): string {
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
        const fields: EssRequestDetailField[] = [
            { label: this.l('Employee'), value: this.employeeDisplay() },
            { label: this.l('RequestNumber'), value: row.requestNumber ?? '' },
            { label: this.l('DocumentType'), value: this.typeName(row) },
            { label: this.l('DocumentNumber'), value: row.documentNum ?? '' },
            { label: this.l('WithdrawDate'), value: this.detailDate(row.withdrawDate) },
            { label: this.l('HandedOverOn'), value: this.detailDate(row.actualWithdrawDate) },
            { label: this.l('ReturnDate'), value: this.detailDate(row.returnDate) },
            { label: this.l('ActualReturnDate'), value: this.detailDate(row.actualReturnDate) },
            { label: this.l('ReturnRemarks'), value: row.returnRemarks ?? '' },
            { label: this.l('Guarantor'), value: this.guarantorName(row) },
            { label: this.l('WithdrawalReason'), value: row.withdrawalReason ?? '' },
        ];

        if (row.extensions?.length) {
            fields.push({ label: this.l('EarlierDueDates'), value: this.earlierDueDates(row) });
        }

        return fields;
    }
}
