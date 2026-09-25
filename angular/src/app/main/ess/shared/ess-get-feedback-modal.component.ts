import { ChangeDetectionStrategy, Component, computed, inject, output, signal, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AppComponentBase } from '@shared/common/app-component-base';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { EmployeeAvatarComponent } from '@shared/common/employee-avatar/employee-avatar.component';
import {
    EssInboxServiceProxy,
    FeedbackCandidateDto,
    GetFeedbackCandidatesInput,
    RequestFeedbackInput,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';

/**
 * "Who should look at this?" -- an approver picking colleagues to ask, without handing over the decision.
 *
 * <p>Deliberately not a delegation dialog, and worded so nobody mistakes it for one. Delegation moves the
 * authority; this moves nothing. The approver who asks is still the approver, the request does not change
 * step, and it can be approved a moment later whether or not anybody has replied.</p>
 *
 * <p>Multi-select on purpose: "ask these three" is one intention, and sending it as three separate asks
 * would let it half-succeed with no way to tell which of them landed.</p>
 */
@Component({
    selector: 'essGetFeedbackModal',
    templateUrl: './ess-get-feedback-modal.component.html',
    styleUrls: ['./ess-get-feedback-modal.component.less'],
    imports: [CommonModule, FormsModule, AppBsModalDirective, LocalizePipe, EmployeeAvatarComponent],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EssGetFeedbackModalComponent extends AppComponentBase {
    private readonly _inboxService = inject(EssInboxServiceProxy);

    readonly modal = viewChild<ModalDirective>('getFeedbackModal');

    /** Raised once the asks are stored, so the host can refresh the panel that lists them. */
    readonly requested = output<void>();

    readonly active = signal(false);
    readonly loading = signal(false);
    readonly saving = signal(false);

    readonly candidates = signal<FeedbackCandidateDto[]>([]);
    readonly selected = signal<Set<number>>(new Set<number>());

    filterText = '';
    note = '';

    private workflowRequestId = 0;

    /**
     * Narrowed as the approver types. Matches on code as well as name, because the code is how one of two
     * colleagues with the same name is actually told apart.
     */
    readonly visibleCandidates = computed(() => {
        const all = this.candidates();
        const needle = (this.filterValue() || '').trim().toLowerCase();

        if (!needle) {
            return all;
        }

        return all.filter((c) => {
            const name = (this.isRtl ? c.nameAr || c.nameEn : c.nameEn) || '';

            return name.toLowerCase().includes(needle) || (c.code || '').toLowerCase().includes(needle);
        });
    });

    /** Mirrors the ngModel into a signal so visibleCandidates recomputes as the text changes. */
    readonly filterValue = signal('');

    readonly selectedCount = computed(() => this.selected().size);

    /**
     * The chosen colleagues, in full.
     *
     * <p>Shown above the list because the list scrolls and the choice does not: with several hundred names
     * a ticked box two hundred rows up is invisible, and the only way to find out who was selected was to
     * send it. Each chip removes itself, so a mis-click is undone where it is seen.</p>
     */
    readonly selectedPeople = computed(() => {
        const ids = this.selected();

        return this.candidates().filter((c) => c.employeeId && ids.has(c.employeeId));
    });

    show(workflowRequestId: number): void {
        this.workflowRequestId = workflowRequestId;
        this.active.set(true);
        this.candidates.set([]);
        this.selected.set(new Set<number>());
        this.filterText = '';
        this.filterValue.set('');
        this.note = '';

        this.modal()?.show();
        this.load();
    }

    close(): void {
        this.active.set(false);
        this.modal()?.hide();
    }

    onFilterChange(value: string): void {
        this.filterText = value;
        this.filterValue.set(value);
    }

    isSelected(employeeId: number | undefined): boolean {
        return employeeId ? this.selected().has(employeeId) : false;
    }

    /**
     * Toggles one colleague. Somebody already asked is left alone -- asking twice stores nothing and would
     * leave the approver wondering why the second ask did not appear.
     */
    toggle(candidate: FeedbackCandidateDto): void {
        if (!candidate.employeeId || candidate.alreadyAsked) {
            return;
        }

        // Deselecting never needs a warning -- it takes somebody out of the ask, which cannot surprise
        // anyone. Only adding does.
        if (this.selected().has(candidate.employeeId)) {
            this.setSelected(candidate.employeeId, false);
            return;
        }

        // Somebody already in the approval chain can read the request and say their piece through the
        // ordinary route, so asking them is usually redundant -- but not always: a later approver may
        // genuinely want an earlier one's view first. Asked rather than forbidden, and asked at the moment
        // of choosing rather than at Send, where it would be too late to tell which name caused it.
        if (candidate.isAlreadyApprover) {
            this.message.confirm(
                this.l('AlreadyInApprovalCycleConfirm', this.displayName(candidate)),
                this.l('GetFeedback'),
                (confirmed: boolean) => {
                    if (confirmed) {
                        this.setSelected(candidate.employeeId!, true);
                    }
                }
            );

            return;
        }

        this.setSelected(candidate.employeeId, true);
    }

    /** Removes one chip. The same path as unticking, so the two cannot drift apart. */
    remove(candidate: FeedbackCandidateDto): void {
        if (candidate.employeeId) {
            this.setSelected(candidate.employeeId, false);
        }
    }

    /**
     * A new Set each time: mutating the existing one in place would not change the signal's reference, so
     * nothing downstream would recompute and the chips would not appear.
     */
    private setSelected(employeeId: number, selected: boolean): void {
        const next = new Set(this.selected());

        if (selected) {
            next.add(employeeId);
        } else {
            next.delete(employeeId);
        }

        this.selected.set(next);
    }

    send(): void {
        if (!this.selectedCount() || this.saving()) {
            return;
        }

        this.saving.set(true);

        const input = new RequestFeedbackInput();
        input.workflowRequestId = this.workflowRequestId;
        input.employeeIds = [...this.selected()];
        input.note = (this.note || '').trim();

        this._inboxService
            .requestFeedback(input)
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe(() => {
                this.notify.success(this.l('FeedbackRequestsSent'));
                this.requested.emit();
                this.close();
            });
    }

    displayName(candidate: FeedbackCandidateDto): string {
        const name = (this.isRtl ? candidate.nameAr || candidate.nameEn : candidate.nameEn) || '';

        return name && candidate.code ? `${name} (${candidate.code})` : name;
    }

    plainName(candidate: FeedbackCandidateDto): string {
        return (this.isRtl ? candidate.nameAr || candidate.nameEn : candidate.nameEn) || '';
    }

    /**
     * Direction, read from the document rather than from a service.
     *
     * <p>Each component that needs it declares its own -- it is not on AppComponentBase, which is the
     * assumption that broke this build.</p>
     */
    private get isRtl(): boolean {
        return document?.documentElement?.getAttribute('dir') === 'rtl';
    }

    private load(): void {
        this.loading.set(true);

        const input = new GetFeedbackCandidatesInput();
        input.workflowRequestId = this.workflowRequestId;

        this._inboxService
            .getFeedbackCandidates(input)
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe({
                next: (result) => this.candidates.set(result ?? []),
                error: () => this.candidates.set([]),
            });
    }
}
