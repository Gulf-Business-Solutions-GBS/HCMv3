import { ChangeDetectionStrategy, Component, OnInit, inject, signal, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import {
    EntityDto,
    EssContextDto,
    EssSessionServiceProxy,
    GetMyRequestsInput,
    MyExitPermitDto,
    MyExitPermitServiceProxy,
    RequestExitPermitInput,
    TravelReason,
} from '@shared/service-proxies/service-proxies';
import { DateTime } from 'luxon';
import { Observable } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { SubHeaderComponent } from '../../../shared/common/sub-header/sub-header.component';
import { MyRequestComponentBase } from '../my-request-component-base';
import { EssRequestDetailModalComponent } from '../shared/ess-request-detail-modal.component';
import { HelpPageDirective } from '@app/shared/help/help-page.directive';

/**
 * Self-service exit permits.
 *
 * <p>The permit itself is obtained from the government, not from this system, so the number is keyed in
 * rather than generated -- the employee already has the document in hand when they reach this screen.</p>
 */
@Component({
    templateUrl: './my-exit-permit.component.html',
    animations: [appModuleAnimation()],
    imports: [
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
export class MyExitPermitComponent extends MyRequestComponentBase<MyExitPermitDto> implements OnInit {
    private readonly _service = inject(MyExitPermitServiceProxy);
    private readonly _essSessionService = inject(EssSessionServiceProxy);

    readonly detail = viewChild<EssRequestDetailModalComponent>('detail');

    /**
     * Only so the form can SHOW whose permit this is. The server takes the employee from the session, so
     * nothing here is ever sent -- but a form with no name on it reads as if it could be anyone's.
     */
    readonly context = signal<EssContextDto | undefined>(undefined);

    /** Listed rather than derived, so the order on screen is a decision rather than an accident. */
    readonly travelReasons: TravelReason[] = [TravelReason.PlannedLeave, TravelReason.EmergencyLeave, TravelReason.PersonalTravel];

    form = {
        externalRequestNumber: '',
        travelReason: TravelReason.PlannedLeave,
        exitDate: '',
        startDate: '',
        comments: '',
    };

    ngOnInit(): void {
        this.reload();

        // Failure is silent on purpose: the name is decoration, and an unlinked account is already
        // explained by the panel reload() puts up.
        this._essSessionService.getEssContext().subscribe({
            next: (result) => this.context.set(result),
            error: () => undefined,
        });
    }

    /** "1042 - Ahmad Al Sayed", or just the name when the code is missing. */
    employeeDisplay(): string {
        const ctx = this.context();
        if (!ctx) {
            return '';
        }

        const name = this.isRtl ? ctx.employeeFullNameAr || ctx.employeeFullNameEn : ctx.employeeFullNameEn;

        return [ctx.employeeCode, name].filter((p) => !!p).join(' - ');
    }

    protected fetch(input: GetMyRequestsInput): Observable<{ items: MyExitPermitDto[] }> {
        return this._service.getMyExitPermits(input);
    }

    protected cancelRequest(input: EntityDto): Observable<void> {
        return this._service.cancelMyExitPermit(input);
    }

    protected resetForm(): void {
        this.form = {
            externalRequestNumber: '',
            travelReason: TravelReason.PlannedLeave,
            exitDate: '',
            startDate: '',
            comments: '',
        };
    }

    /**
     * Opens the submitted request read-only: every field, the requester's comment, the approvers' remarks
     * and the action trail. Read-only because once it is in the chain the workflow owns the row -- the only
     * thing the requester can still do to it is withdraw it, and that is on the list.
     */
    openDetail(row: MyExitPermitDto): void {
        this.detail()?.show({
            title: this.l('EssMyExitPermits'),
            fields: [
                { label: this.l('Employee'), value: this.employeeDisplay() },
                { label: this.l('TravelReason'), value: this.travelReasonLabel(row.travelReason) },
                { label: this.l('StartDate'), value: row.startDate?.toFormat('yyyy-MM-dd') ?? '' },
                { label: this.l('ExitDate'), value: row.exitDate?.toFormat('yyyy-MM-dd') ?? '' },
                { label: this.l('ExitPermitNumber'), value: row.externalRequestNumber ?? '' },
            ],
            comments: row.comments,
            workflowRequestId: row.workflowRequestId,
            statusLabel: this.statusLabel(row),
            statusClass: this.statusClass(row),
        });
    }

    submit(): void {
        if (this.saving()) {
            return;
        }

        // Refused here as well as by the server, so the employee is told before the round trip.
        if (!this.form.externalRequestNumber?.trim()) {
            this.message.warn(this.l('ExitPermitNumberIsRequired'));
            return;
        }

        if (!this.form.exitDate) {
            this.message.warn(this.l('ExitDateIsRequired'));
            return;
        }

        if (!this.form.startDate) {
            this.message.warn(this.l('StartDateIsRequired'));
            return;
        }

        const input = new RequestExitPermitInput();
        input.externalRequestNumber = this.form.externalRequestNumber.trim();
        input.travelReason = this.form.travelReason;
        input.exitDate = DateTime.fromISO(this.form.exitDate);
        input.startDate = DateTime.fromISO(this.form.startDate);
        input.comments = this.form.comments;

        this.saving.set(true);

        this._service
            .requestExitPermit(input)
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe((result) => this.notifySubmitted(result.submittedForApproval));
    }

    travelReasonLabel(reason: TravelReason | undefined): string {
        if (reason === undefined || reason === null) {
            return '';
        }

        return this.l('TravelReason_' + TravelReason[reason]);
    }
}
