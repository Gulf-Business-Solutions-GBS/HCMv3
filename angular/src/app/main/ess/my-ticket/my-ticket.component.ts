import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import {
    EntityDto,
    GetAllTicketTypesInput,
    GetMyRequestsInput,
    MyTicketRequestDto,
    MyTicketServiceProxy,
    RequestTicketInput,
    TicketTypeDto,
    TicketTypesServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { DateTime } from 'luxon';
import { Observable } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { SubHeaderComponent } from '../../../shared/common/sub-header/sub-header.component';
import { WorkflowHistoryModalComponent } from '../../../shared/common/workflow-activity/workflow-history-modal.component';
import { MyRequestComponentBase } from '../my-request-component-base';

/** Self-service air ticket requests. */
@Component({
    templateUrl: './my-ticket.component.html',
    animations: [appModuleAnimation()],
    imports: [CommonModule, FormsModule, SubHeaderComponent, LocalizePipe, LuxonFormatPipe, WorkflowHistoryModalComponent],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyTicketComponent extends MyRequestComponentBase<MyTicketRequestDto> implements OnInit {
    private readonly _service = inject(MyTicketServiceProxy);
    private readonly _typesService = inject(TicketTypesServiceProxy);

    readonly ticketTypes = signal<TicketTypeDto[]>([]);

    form = { ticketTypeId: null as number | null, ticketDate: '', amount: null as number | null, comments: '' };

    ngOnInit(): void {
        this._typesService
            .getAll(new GetAllTicketTypesInput({ maxResultCount: 200, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.ticketTypes.set(result.items));

        this.reload();
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
}
