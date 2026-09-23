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
    SaveExitPermitDraftInput,
    TravelReason,
} from '@shared/service-proxies/service-proxies';
import { DateTime } from 'luxon';
import { Observable } from 'rxjs';
import { finalize, switchMap } from 'rxjs/operators';
import { SubHeaderComponent } from '../../../shared/common/sub-header/sub-header.component';
import { MyRequestComponentBase } from '../my-request-component-base';
import { EssRequestDetailModalComponent } from '../shared/ess-request-detail-modal.component';
import { ExitPermitFormComponent, ExitPermitFormValue } from './exit-permit-form.component';
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
        ExitPermitFormComponent,
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

    /** The create form's data, owned here and written into by the form component through ngModel. */
    form: ExitPermitFormValue = MyExitPermitComponent.emptyForm();

    private static emptyForm(employeeDisplay = ''): ExitPermitFormValue {
        return {
            employeeDisplay: employeeDisplay,
            externalRequestNumber: '',
            travelReason: TravelReason.PlannedLeave,
            exitDate: '',
            startDate: '',
            comments: '',
        };
    }

    ngOnInit(): void {
        this.reload();

        // Failure is silent on purpose: the name is decoration, and an unlinked account is already
        // explained by the panel reload() puts up.
        this._essSessionService.getEssContext().subscribe({
            next: (result) => {
                this.context.set(result);

                // A NEW object rather than a mutated one. The form component is OnPush and takes this as an
                // input, so writing into the existing object would change what the name IS without changing
                // the input reference -- and the field would keep showing blank until something else
                // happened to trigger a check.
                this.form = { ...this.form, employeeDisplay: this.employeeDisplay() };
            },
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
        // The employee is not a field the requester filled in, so clearing the form must not clear it.
        this.form = MyExitPermitComponent.emptyForm(this.employeeDisplay());
    }

    /**
     * Opens the submitted request read-only: the same form, the requester's comment, the approvers' remarks
     * and the action trail. Read-only because once it is in the chain the workflow owns the row -- the only
     * thing the requester can still do to it is withdraw it, and that is on the list.
     *
     * <p>The fields below are still passed, and are still worth passing: a permit raised while workflow is
     * switched off for this type has no workflow request, so the dialog has nothing to fetch and these are
     * all it can show.</p>
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
            // These are the signed-in employee's own requests, so the first post in the conversation is
            // theirs; naming them here saves the modal guessing from the Created activity.
            requesterName: this.employeeDisplay(),
            workflowRequestId: row.workflowRequestId,
            statusLabel: this.statusLabel(row),
            statusClass: this.statusClass(row),
        });
    }

    /**
     * Save: files what is on the form as a draft and goes back to the list. Nothing is validated, because
     * the reason to save rather than submit is usually that something is still missing -- a permit applied
     * for but not yet issued, most often. The rules are enforced at Submit instead.
     */
    save(): void {
        if (this.saving()) {
            return;
        }

        this.saving.set(true);

        this._service
            .saveMyExitPermitDraft(this.draftInput())
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe(() => this.notifyDraftSaved());
    }

    /**
     * Submit: sends it into the approval chain. Two paths, because a draft already exists as a row and a
     * new request does not -- so a draft is saved and then submitted, while a new one is created and
     * submitted in the single call that has always done both.
     */
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

        this.saving.set(true);

        const draftId = this.editingId();

        if (draftId !== undefined) {
            // Saved first, so what the approver sees is what is on the screen. Submitting without saving
            // would send whatever the draft held when it was last written, which is the one thing the
            // employee did not just read.
            this._service
                .saveMyExitPermitDraft(this.draftInput())
                .pipe(switchMap(() => this._service.submitMyExitPermitDraft(new EntityDto({ id: draftId }))))
                .pipe(finalize(() => this.saving.set(false)))
                .subscribe((result) => this.notifySubmitted(result.submittedForApproval));

            return;
        }

        const input = new RequestExitPermitInput();
        input.externalRequestNumber = this.form.externalRequestNumber.trim();
        input.travelReason = this.form.travelReason;
        input.exitDate = DateTime.fromISO(this.form.exitDate);
        input.startDate = DateTime.fromISO(this.form.startDate);
        input.comments = this.form.comments;

        this._service
            .requestExitPermit(input)
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe((result) => this.notifySubmitted(result.submittedForApproval));
    }

    /** Reopens a saved draft in the same form it was filled in on. */
    continueDraft(row: MyExitPermitDto): void {
        // Raw id, not an EntityDto: NSwag flattens EntityDto on GET/DELETE, so the generated signature is
        // getMyExitPermitDraftForEdit(id?: number).
        this._service.getMyExitPermitDraftForEdit(row.id).subscribe((draft) => {
            this.form = {
                // The form shows who the permit is for. A draft is always the signed-in employee's own, so
                // this is the same value the create form displays rather than anything stored on the draft.
                employeeDisplay: this.employeeDisplay(),
                externalRequestNumber: draft.externalRequestNumber ?? '',
                travelReason: draft.travelReason ?? TravelReason.PlannedLeave,
                // A draft is allowed to hold no date at all, and the server sends back the unset date it
                // stored. Rendering that as the year 1 would look like a real value the employee had
                // chosen, so it goes back to the empty field they left.
                exitDate: this.toInputDate(draft.exitDate),
                startDate: this.toInputDate(draft.startDate),
                comments: draft.comments ?? '',
            };

            this.editingId.set(row.id);
            this.showForm.set(true);
        });
    }

    private draftInput(): SaveExitPermitDraftInput {
        const input = new SaveExitPermitDraftInput();
        input.id = this.editingId();
        input.externalRequestNumber = this.form.externalRequestNumber?.trim();
        input.travelReason = this.form.travelReason;
        input.exitDate = this.form.exitDate ? DateTime.fromISO(this.form.exitDate) : undefined;
        input.startDate = this.form.startDate ? DateTime.fromISO(this.form.startDate) : undefined;
        input.comments = this.form.comments;

        return input;
    }

    private toInputDate(value: DateTime | undefined): string {
        return value && value.year > 1 ? value.toFormat('yyyy-MM-dd') : '';
    }

    travelReasonLabel(reason: TravelReason | undefined): string {
        if (reason === undefined || reason === null) {
            return '';
        }

        return this.l('TravelReason_' + TravelReason[reason]);
    }
}
