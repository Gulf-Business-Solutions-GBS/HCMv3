import { ChangeDetectionStrategy, Component, OnChanges, computed, inject, input, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import {
    AddMyAppraisalPeerInput,
    CopyMyAppraisalKpiInput,
    GetMyAppraisalPeerCandidatesInput,
    KpiFrom,
    MyAppraisalKpiSourcesDto,
    MyAppraisalPersonDto,
    MyAppraisalRatingLineDto,
    MyAppraisalRatingOptionDto,
    MyAppraisalReviewDto,
    MyAppraisalServiceProxy,
    RequestStatus,
    SaveMyAppraisalRatingLineInput,
    SaveMyAppraisalReviewInput,
} from '@shared/service-proxies/service-proxies';
import { finalize } from 'rxjs/operators';

/** Which of the three jobs the form is doing, as every self-service request form has. */
export type AppraisalReviewFormMode = 'create' | 'view' | 'approve';

/** What the form needs to load itself: the appraisal, and who it is about for the heading. */
export interface AppraisalReviewFormValue {
    paEmployeeId: number;

    /** "1042 - Ahmad Al Sayed" when the host already has it; the form shows the server's otherwise. */
    employeeDisplay?: string;
}

/** One of the signed-in reviewer's own lines while it is being filled in. */
interface LineEdit {
    ratingId?: number;
    marks: number;
    comments: string;
}

/**
 * The 360 appraisal as one person reviews it: the request header, the KPIs, the rating lines, the reviewer
 * list and the averages -- and Copy KPI, Add Peer, Save Review and Submit Review.
 *
 * <p><b>Loads itself from the appraisal id.</b> A review is live data -- other reviewers are filling in
 * their lines in parallel -- so it is read from the server each time rather than from a snapshot the host
 * passed in. That is also what lets the approver inbox host this same form by the request's target id
 * (register it in ess-request-forms under "PAEmployee").</p>
 *
 * <p><b>What appears is the server's decision.</b> Every section and button follows the access object the
 * server worked out for the signed-in employee from the appraisal's current step (legacy "Appraisal WF
 * Setup"), and every write is re-checked there. The mode input only says who is hosting the form; it does
 * not grant anything.</p>
 */
@Component({
    selector: 'appraisal-review-form',
    templateUrl: './appraisal-review-form.component.html',
    imports: [CommonModule, FormsModule, LocalizePipe],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppraisalReviewFormComponent extends AppComponentBase implements OnChanges {
    private readonly _service = inject(MyAppraisalServiceProxy);

    readonly mode = input<AppraisalReviewFormMode>('view');
    readonly value = input.required<AppraisalReviewFormValue>();

    /** Something was saved, submitted or copied: the host's list may be out of date. */
    readonly changed = output<void>();
    readonly cancelled = output<void>();

    readonly loading = signal(false);
    readonly saving = signal(false);
    readonly review = signal<MyAppraisalReviewDto | undefined>(undefined);
    readonly edits = signal<Record<number, LineEdit>>({});

    // Copy KPI panel
    readonly showCopyKpi = signal(false);
    readonly kpiSources = signal<MyAppraisalKpiSourcesDto | undefined>(undefined);
    kpiFrom: KpiFrom = KpiFrom.JobTitle;
    kpiSourceId: number | undefined;
    readonly kpiFromOptions = [KpiFrom.JobTitle, KpiFrom.OrganizationUnit, KpiFrom.Employee];

    // Add Peer panel
    readonly showAddPeer = signal(false);
    readonly peerCandidates = signal<MyAppraisalPersonDto[]>([]);
    peerFilter = '';
    peerId: number | undefined;

    readonly access = computed(() => this.review()?.access);

    /** Reviewer names are a column only when the lines are not all the reader's own. */
    readonly showReviewerColumn = computed(() => (this.review()?.ratingLines ?? []).some((l) => !l.isOwn));

    readonly employeeDisplay = computed(() => {
        const r = this.review();
        if (!r) {
            return this.value()?.employeeDisplay ?? '';
        }

        return [r.employeeCode, this.pick(r.employeeFullNameEn, r.employeeFullNameAr)].filter((p) => !!p).join(' - ');
    });

    ngOnChanges(): void {
        this.load();
    }

    load(): void {
        const id = this.value()?.paEmployeeId;
        if (!id) {
            return;
        }

        this.loading.set(true);
        this._service
            .getAppraisalReview(id)
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe((review) => {
                this.review.set(review);

                const edits: Record<number, LineEdit> = {};
                for (const line of review.ratingLines ?? []) {
                    if (line.isOwn) {
                        edits[line.id] = {
                            ratingId: line.ratingId ?? undefined,
                            marks: line.marks ?? 0,
                            comments: line.comments ?? '',
                        };
                    }
                }

                this.edits.set(edits);
            });
    }

    // ------------------------------------------------------------------ lines

    canEdit(line: MyAppraisalRatingLineDto): boolean {
        return line.isOwn && !!this.access()?.canSaveReview && !this.saving();
    }

    edit(line: MyAppraisalRatingLineDto): LineEdit {
        return (
            this.edits()[line.id] ?? {
                ratingId: line.ratingId ?? undefined,
                marks: line.marks ?? 0,
                comments: line.comments ?? '',
            }
        );
    }

    setRating(line: MyAppraisalRatingLineDto, ratingId: number | undefined): void {
        this.edits.update((all) => ({ ...all, [line.id]: { ...this.edit(line), ratingId } }));
    }

    setMarks(line: MyAppraisalRatingLineDto, marks: number): void {
        this.edits.update((all) => ({ ...all, [line.id]: { ...this.edit(line), marks: Number(marks) || 0 } }));
    }

    setComments(line: MyAppraisalRatingLineDto, comments: string): void {
        this.edits.update((all) => ({ ...all, [line.id]: { ...this.edit(line), comments } }));
    }

    /**
     * A rating can be picked while its balance allows (legacy GetRatingBalanceLimit). The one already on
     * the line stays selectable, or the select would show a value it refuses to hold.
     */
    ratingSelectable(option: MyAppraisalRatingOptionDto, line: MyAppraisalRatingLineDto): boolean {
        return option.remainingLimit > 0 || this.edit(line).ratingId === option.id;
    }

    ratingLabel(option: MyAppraisalRatingOptionDto): string {
        return `${this.pick(option.nameEn, option.nameAr)} (${option.remainingLimit}/${option.maxLimit})`;
    }

    /** A comment is required where the chosen rating asks for one. */
    commentRequired(line: MyAppraisalRatingLineDto): boolean {
        const ratingId = this.edit(line).ratingId;
        return !!ratingId && !!this.review()?.ratingOptions?.find((o) => o.id === ratingId)?.isCommentRequired;
    }

    lineRatingName(line: MyAppraisalRatingLineDto): string {
        return this.pick(line.ratingNameEn, line.ratingNameAr);
    }

    // ------------------------------------------------------------------ save / submit

    saveReview(): void {
        this.saving.set(true);
        this._service
            .saveReview(this.reviewInput())
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe(() => {
                this.notify.success(this.l('ReviewSaved'));
                this.load();
                this.changed.emit();
            });
    }

    submitReview(): void {
        this.message.confirm(this.l('SubmitReviewConfirmation'), this.l('AreYouSure'), (isConfirmed) => {
            if (!isConfirmed) {
                return;
            }

            this.saving.set(true);
            this._service
                .submitReview(this.reviewInput())
                .pipe(finalize(() => this.saving.set(false)))
                .subscribe((result) => {
                    this.notify.success(
                        this.l(result.stepApproved ? 'ReviewSubmittedAndStepApproved' : 'ReviewSubmittedSuccessfully')
                    );
                    this.load();
                    this.changed.emit();
                });
        });
    }

    private reviewInput(): SaveMyAppraisalReviewInput {
        const review = this.review()!;
        const input = new SaveMyAppraisalReviewInput();
        input.paEmployeeId = review.id;
        input.customTextField1 = review.customTextField1;
        input.customTextField2 = review.customTextField2;
        input.customTextField3 = review.customTextField3;
        input.lines = (review.ratingLines ?? [])
            .filter((l) => l.isOwn)
            .map((l) => {
                const e = this.edit(l);
                const line = new SaveMyAppraisalRatingLineInput();
                line.id = l.id;
                line.ratingId = e.ratingId;
                line.marks = e.marks;
                line.comments = e.comments;
                return line;
            });

        return input;
    }

    // ------------------------------------------------------------------ copy KPI

    openCopyKpi(): void {
        this.showAddPeer.set(false);
        this.showCopyKpi.set(true);
        this.kpiSourceId = undefined;

        this._service.getKpiSources(this.review()!.id).subscribe((sources) => this.kpiSources.set(sources));
    }

    kpiFromLabel(from: KpiFrom): string {
        return this.l('KpiFrom_' + KpiFrom[from]);
    }

    kpiSourceOptions(): { id: number; label: string }[] {
        const sources = this.kpiSources();
        if (!sources) {
            return [];
        }

        const dated = (name: string, start?: { toFormat(f: string): string }, end?: { toFormat(f: string): string }) =>
            start && end ? `${name} (${start.toFormat('dd/MM/yyyy')} - ${end.toFormat('dd/MM/yyyy')})` : name;

        switch (this.kpiFrom) {
            case KpiFrom.JobTitle:
                return (sources.jobTitles ?? []).map((s) => ({
                    id: s.id,
                    label: dated(this.pick(s.nameEn, s.nameAr), s.startDate, s.endDate),
                }));
            case KpiFrom.OrganizationUnit:
                return (sources.organizationUnits ?? []).map((s) => ({
                    id: s.id,
                    label: dated(this.pick(s.nameEn, s.nameAr), s.startDate, s.endDate),
                }));
            default:
                return (sources.employees ?? []).map((p) => ({
                    id: p.id,
                    label: `${p.code} - ${this.pick(p.fullNameEn, p.fullNameAr)}`,
                }));
        }
    }

    copyKpi(): void {
        if (!this.kpiSourceId) {
            this.message.warn(this.l('KpiSourceIsRequired'));
            return;
        }

        const input = new CopyMyAppraisalKpiInput();
        input.paEmployeeId = this.review()!.id;
        input.kpiFrom = this.kpiFrom;
        input.kpiByJobTitleId = this.kpiFrom === KpiFrom.JobTitle ? this.kpiSourceId : undefined;
        input.kpiByOrgUnitId = this.kpiFrom === KpiFrom.OrganizationUnit ? this.kpiSourceId : undefined;
        input.sourceEmployeeId = this.kpiFrom === KpiFrom.Employee ? this.kpiSourceId : undefined;

        this.saving.set(true);
        this._service
            .copyKpi(input)
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe(() => {
                this.notify.success(this.l('KpisCopied'));
                this.showCopyKpi.set(false);
                this.load();
                this.changed.emit();
            });
    }

    // ------------------------------------------------------------------ add peer

    openAddPeer(): void {
        this.showCopyKpi.set(false);
        this.showAddPeer.set(true);
        this.peerId = undefined;
        this.searchPeers();
    }

    searchPeers(): void {
        const input = new GetMyAppraisalPeerCandidatesInput();
        input.paEmployeeId = this.review()!.id;
        input.filter = this.peerFilter?.trim() || undefined;

        this._service.getPeerCandidates(input).subscribe((people) => this.peerCandidates.set(people));
    }

    personLabel(person: MyAppraisalPersonDto): string {
        return `${person.code} - ${this.pick(person.fullNameEn, person.fullNameAr)}`;
    }

    addPeer(): void {
        if (!this.peerId) {
            this.message.warn(this.l('PeerIsRequired'));
            return;
        }

        const input = new AddMyAppraisalPeerInput();
        input.paEmployeeId = this.review()!.id;
        input.peerEmployeeId = this.peerId;

        this.saving.set(true);
        this._service
            .addPeer(input)
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe(() => {
                this.notify.success(this.l('PeerAdded'));
                this.showAddPeer.set(false);
                this.load();
                this.changed.emit();
            });
    }

    // ------------------------------------------------------------------ display

    statusLabel(): string {
        const status = this.review()?.workflowStatus;
        return status === undefined || status === null
            ? this.l('Recorded')
            : this.l('WorkflowRequestStatus_' + RequestStatus[status]);
    }

    score(value: number | undefined): string {
        return value === undefined || value === null ? '' : value.toFixed(2);
    }

    pick(en: string | undefined, ar: string | undefined): string {
        const rtl = document?.documentElement?.getAttribute('dir') === 'rtl';
        return rtl ? ar || en || '' : en || '';
    }
}
