import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import {
    ClearanceChecklistAnswerInput,
    ClearanceChecklistLineDto,
    ClearanceTypeLookupDto,
    EntityDto,
    GetMyRequestsInput,
    MyClearanceDto,
    MyClearanceServiceProxy,
    RequestClearanceInput,
    SaveClearanceChecklistInput,
} from '@shared/service-proxies/service-proxies';
import { DateTime } from 'luxon';
import { Observable } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { SubHeaderComponent } from '../../../shared/common/sub-header/sub-header.component';
import { WorkflowHistoryModalComponent } from '../../../shared/common/workflow-activity/workflow-history-modal.component';
import { MyRequestComponentBase } from '../my-request-component-base';

/**
 * Self-service exit clearance.
 *
 * <p>Two things on one screen, because the server exposes two things: the clearance REQUEST (a type and a
 * date range, like every other self-service transaction) and the CHECKLIST attached to it -- a set of lines
 * each owned by a different department's workflow role. The checklist is opened per row rather than shown
 * inline for every row, because the server narrows it to the lines the signed-in person's roles own and that
 * is usually nothing at all: an employee looking at their own clearance is not one of the departments
 * signing it off, and the screen says so rather than showing an empty grid.</p>
 */
@Component({
    templateUrl: './my-clearance.component.html',
    animations: [appModuleAnimation()],
    imports: [CommonModule, FormsModule, SubHeaderComponent, LocalizePipe, LuxonFormatPipe, WorkflowHistoryModalComponent],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyClearanceComponent extends MyRequestComponentBase<MyClearanceDto> implements OnInit {
    private readonly _service = inject(MyClearanceServiceProxy);

    readonly clearanceTypes = signal<ClearanceTypeLookupDto[]>([]);

    form = { clearanceRequestTypeId: null as number | null, startDate: '', endDate: '', comments: '' };

    /** Which row's checklist is open; undefined means none. */
    readonly checklistFor = signal<number | undefined>(undefined);
    readonly checklistLoading = signal(false);
    readonly checklistSaving = signal(false);
    readonly checklistLines = signal<ClearanceChecklistLineDto[]>([]);

    /** False when the clearance is not sitting on the parallel step, so the lines render read-only. */
    readonly canRecordAnswers = signal(false);

    /** Line id -> answer, kept apart from the DTOs so cancelling simply throws this away. */
    answers: Record<number, string> = {};

    ngOnInit(): void {
        this._service.getClearanceTypes().subscribe((result) => this.clearanceTypes.set(result));

        this.reload();
    }

    protected fetch(input: GetMyRequestsInput): Observable<{ items: MyClearanceDto[] }> {
        return this._service.getMyClearances(input);
    }

    protected cancelRequest(input: EntityDto): Observable<void> {
        return this._service.cancelMyClearance(input);
    }

    protected resetForm(): void {
        this.form = { clearanceRequestTypeId: null, startDate: '', endDate: '', comments: '' };
    }

    submit(): void {
        if (this.saving()) {
            return;
        }

        if (!this.form.clearanceRequestTypeId) {
            this.message.warn(this.l('ClearanceTypeIsRequired'));
            return;
        }

        if (!this.form.startDate) {
            this.message.warn(this.l('StartDateIsRequired'));
            return;
        }

        if (!this.form.endDate) {
            this.message.warn(this.l('EndDateIsRequired'));
            return;
        }

        // The server refuses this too; this is the early, friendlier failure before a row is created and
        // rolled back.
        if (this.form.endDate < this.form.startDate) {
            this.message.warn(this.l('EndDateCannotBeBeforeStartDate'));
            return;
        }

        const input = new RequestClearanceInput();
        input.clearanceRequestTypeId = this.form.clearanceRequestTypeId;
        input.startDate = DateTime.fromISO(this.form.startDate);
        input.endDate = DateTime.fromISO(this.form.endDate);
        input.comments = this.form.comments;

        this.saving.set(true);

        this._service
            .requestClearance(input)
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe((result) => this.notifySubmitted(result.submittedForApproval));
    }

    toggleChecklist(row: MyClearanceDto): void {
        if (this.checklistFor() === row.id) {
            this.closeChecklist();
            return;
        }

        this.checklistFor.set(row.id);
        this.checklistLines.set([]);
        this.answers = {};
        this.checklistLoading.set(true);

        this._service
            .getClearanceChecklist(row.id)
            .pipe(finalize(() => this.checklistLoading.set(false)))
            .subscribe((result) => {
                const lines = result.lines ?? [];

                // Seeded from what is already saved, so an approver coming back to a half-finished form sees
                // their own previous answers rather than a blank one.
                this.answers = {};
                lines.forEach((line) => (this.answers[line.clearanceRequestFieldId] = line.value ?? ''));

                this.canRecordAnswers.set(result.canRecordAnswers);
                this.checklistLines.set(lines);
            });
    }

    closeChecklist(): void {
        this.checklistFor.set(undefined);
        this.checklistLines.set([]);
        this.canRecordAnswers.set(false);
        this.answers = {};
    }

    saveChecklist(): void {
        const clearanceId = this.checklistFor();

        if (!clearanceId || this.checklistSaving() || !this.canRecordAnswers()) {
            return;
        }

        const lines = this.checklistLines();

        if (lines.length === 0) {
            return;
        }

        // Refused here as well as by the server: a half-filled checklist tells the next department nothing,
        // and the employee would rather be told now than after the round trip.
        const unanswered = lines.filter((line) => !(this.answers[line.clearanceRequestFieldId] ?? '').trim());

        if (unanswered.length > 0) {
            this.message.warn(this.l('EveryChecklistLineMustBeAnswered'));
            return;
        }

        const input = new SaveClearanceChecklistInput();
        input.employeeClearanceId = clearanceId;
        input.answers = lines.map(
            (line) =>
                new ClearanceChecklistAnswerInput({
                    clearanceRequestFieldId: line.clearanceRequestFieldId,
                    value: (this.answers[line.clearanceRequestFieldId] ?? '').trim(),
                })
        );

        this.checklistSaving.set(true);

        this._service
            .saveClearanceChecklist(input)
            .pipe(finalize(() => this.checklistSaving.set(false)))
            .subscribe(() => {
                this.notify.success(this.l('SavedSuccessfully'));
                this.closeChecklist();
            });
    }

    typeName(row: MyClearanceDto): string {
        return this.pick(row.clearanceRequestTypeNameEn, row.clearanceRequestTypeNameAr);
    }

    lineName(line: ClearanceChecklistLineDto): string {
        return this.pick(line.nameEn, line.nameAr);
    }

    lookupName(type: ClearanceTypeLookupDto): string {
        return this.pick(type.nameEn, type.nameAr);
    }
}
