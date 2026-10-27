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
    MyTicketRequestDto,
    MyTicketServiceProxy,
    RequestTicketInput,
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

/** Self-service air ticket requests. */
@Component({
    templateUrl: './my-ticket.component.html',
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
export class MyTicketComponent extends MyRequestComponentBase<MyTicketRequestDto> implements OnInit {
    private readonly _service = inject(MyTicketServiceProxy);
    private readonly _lookups = inject(EssLookupServiceProxy);

    readonly ticketTypes = signal<SimpleLookupDto[]>([]);

    /** The list's columns, for the shared heading (sorting), filter row and pager. */
    readonly list = new EssList<MyTicketRequestDto>(this.rows, () => [
        { key: 'ticketType', label: 'TicketType', text: (row) => this.typeName(row), filter: 'select' },
        {
            key: 'ticketDate',
            label: 'TicketDate',
            text: (row) => this.detailDate(row.ticketDate),
            sort: (row) => row.ticketDate?.toMillis(),
        },
        { key: 'amount', label: 'Amount', text: (row) => row.amount },
        { key: 'status', label: 'Status', text: (row) => this.statusLabel(row), filter: 'select' },
    ]);

    form = { ticketTypeId: null as number | null, ticketDate: '', amount: null as number | null, comments: '' };

    ngOnInit(): void {
        this._lookups.getTicketTypes().subscribe((result) => this.ticketTypes.set(result ?? []));

        this.reload();
        this.loadContext();
    }

    protected fetch(input: GetMyRequestsInput): Observable<{ items: MyTicketRequestDto[] }> {
        return this._service.getMyTickets(input);
    }

    protected cancelRequest(input: EntityDto): Observable<void> {
        return this._service.cancelMyTicket(input);
    }

    protected resetForm(): void {
        this.form = { ticketTypeId: null, ticketDate: '', amount: null, comments: '' };
    }

    submit(): void {
        if (!this.form.ticketTypeId || !this.form.ticketDate || this.saving()) {
            return;
        }

        if (this.form.amount != null && this.form.amount < 0) {
            this.message.warn(this.l('TicketAmountCannotBeNegative'));
            return;
        }

        const input = new RequestTicketInput();
        input.ticketTypeId = this.form.ticketTypeId;
        input.ticketDate = DateTime.fromISO(this.form.ticketDate);

        // Left blank means "claim the entitlement, payroll prices it" rather than "claim nothing".
        input.amount = this.form.amount ?? 0;

        input.comments = this.form.comments;

        this.saving.set(true);

        this._service
            .requestTicket(input)
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe((result) => this.notifySubmitted(result.submittedForApproval));
    }

    typeName(row: MyTicketRequestDto): string {
        return this.pick(row.ticketTypeNameEn, row.ticketTypeNameAr);
    }

    protected requestType(): string {
        return 'EmployeeTicket';
    }

    protected detailTitle(): string {
        return this.l('EssMyTickets');
    }

    protected detailFields(row: MyTicketRequestDto): EssRequestDetailField[] {
        return [
            { label: this.l('Employee'), value: this.employeeDisplay() },
            { label: this.l('TicketType'), value: this.typeName(row) },
            { label: this.l('TicketDate'), value: this.detailDate(row.ticketDate) },
            { label: this.l('Amount'), value: `${row.amount ?? ''}` },
        ];
    }
}
