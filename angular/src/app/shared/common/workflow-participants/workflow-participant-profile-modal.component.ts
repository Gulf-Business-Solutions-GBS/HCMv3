import { ChangeDetectionStrategy, Component, computed, inject, signal, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AppComponentBase } from '@shared/common/app-component-base';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { EmployeeAvatarComponent } from '@shared/common/employee-avatar/employee-avatar.component';
import {
    GetWorkflowParticipantProfileInput,
    WorkflowActivityServiceProxy,
    WorkflowParticipantProfileDto,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';

/**
 * A colleague's directory card, opened from a name on the participants panel.
 *
 * <p><b>A dialog rather than a route, deliberately.</b> The names it is opened from sit inside the approval
 * dialog, and an approver mid-decision who clicks a colleague's name wants to know who that person is --
 * not to lose the request they were reading. Navigating away would close the dialog and discard the comment
 * they had started typing.</p>
 *
 * <p><b>What it shows.</b> Work contact information only: photo, name, code, job title, department, work
 * location, work email and business telephone. Salary, bank details, civil ID, date of birth and home
 * contact details are all on the employee record and all deliberately absent -- this is the staff
 * directory entry, not the personnel file. The server enforces that rather than trusting this component:
 * the DTO simply has no such fields on it.</p>
 *
 * <p><b>Why it is safe to open for any self-service user.</b> The endpoint is scoped to one workflow
 * request and refuses any employee who is not already named on it, so it cannot be used to page through
 * the employee table. It needs no admin permission, which is the whole point -- linking these names to
 * the admin employee screen would give an ordinary approver a permission error.</p>
 */
@Component({
    selector: 'workflowParticipantProfileModal',
    templateUrl: './workflow-participant-profile-modal.component.html',
    styleUrls: ['./workflow-participant-profile-modal.component.less'],
    imports: [CommonModule, AppBsModalDirective, LocalizePipe, EmployeeAvatarComponent],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WorkflowParticipantProfileModalComponent extends AppComponentBase {
    private readonly _activityService = inject(WorkflowActivityServiceProxy);

    readonly modal = viewChild<ModalDirective>('workflowParticipantProfileModal');

    readonly active = signal(false);
    readonly loading = signal(false);
    readonly profile = signal<WorkflowParticipantProfileDto | undefined>(undefined);

    /** Shown while the card loads, so the dialog opens with a name on it rather than empty. */
    readonly pendingName = signal('');

    readonly name = computed(() => {
        const served = this.profile();

        return this.pick(served?.nameEn, served?.nameAr) || this.pendingName();
    });

    /** "Karam Ismaeil (3804)" -- the format the user asked for, name first. */
    readonly heading = computed(() => {
        const code = this.profile()?.code;

        return code ? `${this.name()} (${code})` : this.name();
    });

    readonly jobTitle = computed(() => this.pick(this.profile()?.jobTitleEn, this.profile()?.jobTitleAr));

    readonly workLocation = computed(() =>
        this.pick(this.profile()?.workLocationEn, this.profile()?.workLocationAr)
    );

    /** The business number with its extension appended, or whichever of the two exists. */
    readonly telephone = computed(() => {
        const served = this.profile();
        const parts = [served?.businessLandline, served?.businessExtension].filter((p) => !!p);

        return parts.join(' ext. ');
    });

    /** True when the card carries nothing but a name -- so it can say so instead of drawing empty rows. */
    readonly hasDetails = computed(
        () =>
            !!this.jobTitle() ||
            !!this.workLocation() ||
            !!this.profile()?.department ||
            !!this.profile()?.workEmail ||
            !!this.telephone()
    );

    show(workflowRequestId: number, employeeId: number, fallbackName = ''): void {
        this.profile.set(undefined);
        this.pendingName.set(fallbackName);
        this.active.set(true);
        this.modal()?.show();

        this.loading.set(true);

        const input = new GetWorkflowParticipantProfileInput();
        input.workflowRequestId = workflowRequestId;
        input.employeeId = employeeId;

        this._activityService
            .getParticipantProfile(input)
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe({
                next: (result) => this.profile.set(result),

                // The interceptor has already told the user why. Closing on failure is better than leaving
                // an empty card open, which reads as "this person has no details".
                error: () => this.close(),
            });
    }

    close(): void {
        this.active.set(false);
        this.profile.set(undefined);
        this.pendingName.set('');
        this.modal()?.hide();
    }

    private pick(en: string | undefined, ar: string | undefined): string {
        return this.isRtl ? ar || en || '' : en || '';
    }

    private get isRtl(): boolean {
        return document?.documentElement?.getAttribute('dir') === 'rtl';
    }
}
