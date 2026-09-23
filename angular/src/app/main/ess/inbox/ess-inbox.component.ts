import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import {
    ApproveRequestInput,
    EssInboxItemDto,
    EssInboxServiceProxy,
    GetMyInboxInput,
    RejectRequestInput,
    SendBackRequestInput,
} from '@shared/service-proxies/service-proxies';
import { finalize } from 'rxjs/operators';
import { SubHeaderComponent } from '../../../shared/common/sub-header/sub-header.component';
import { WorkflowActivityRailComponent } from '../../../shared/common/workflow-activity/workflow-activity-rail.component';
import { HelpPageDirective } from '@app/shared/help/help-page.directive';

/**
 * The approver's inbox: everything waiting on this person, of every transaction type, in one list.
 *
 * <p>One screen rather than one per type. Every row is rendered from the request's own summary, so adding a
 * seventh transaction type needs no change here at all -- legacy had roughly thirty separate approval
 * screens because its request data was per-type.</p>
 *
 * <p>Selecting a row opens the activity rail beside it, which is where the approver reads the discussion and
 * adds their own comment before deciding.</p>
 */
@Component({
    templateUrl: './ess-inbox.component.html',
    animations: [appModuleAnimation()],
    imports: [CommonModule, FormsModule, SubHeaderComponent, LocalizePipe, LuxonFormatPipe, WorkflowActivityRailComponent, HelpPageDirective],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EssInboxComponent extends AppComponentBase implements OnInit {
    private readonly _inboxService = inject(EssInboxServiceProxy);

    readonly loading = signal(false);
    readonly acting = signal(false);
    readonly notLinked = signal(false);

    readonly items = signal<EssInboxItemDto[]>([]);
    readonly selected = signal<EssInboxItemDto | undefined>(undefined);

    comment = '';

    ngOnInit(): void {
        this.reload();
    }

    reload(): void {
        this.loading.set(true);
        this.selected.set(undefined);

        this._inboxService
            .getMyInbox(new GetMyInboxInput({ requestType: undefined, maxResultCount: 50, skipCount: 0, sorting: undefined }))
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe({
                next: (result) => this.items.set(result.items),
                error: () => this.notLinked.set(true),
            });
    }

    select(item: EssInboxItemDto): void {
        this.selected.set(item);
        this.comment = '';
    }

    approve(): void {
        const item = this.selected();
        if (!item || this.acting()) {
            return;
        }

        const input = new ApproveRequestInput();
        input.workflowRequestId = item.id;
        input.comment = this.comment;

        this.act(() => this._inboxService.approve(input), 'RequestApproved');
    }

    reject(): void {
        const item = this.selected();
        if (!item || this.acting()) {
            return;
        }

        // Required by the server too; checked here so the approver is told before the round trip.
        if (!this.comment?.trim()) {
            this.message.warn(this.l('AReasonIsRequiredToReject'));
            return;
        }

        const input = new RejectRequestInput();
        input.workflowRequestId = item.id;
        input.comment = this.comment;

        this.act(() => this._inboxService.reject(input), 'RequestRejected');
    }

    sendBack(): void {
        const item = this.selected();
        if (!item || this.acting()) {
            return;
        }

        if (!this.comment?.trim()) {
            this.message.warn(this.l('AReasonIsRequiredToSendBack'));
            return;
        }

        const input = new SendBackRequestInput();
        input.workflowRequestId = item.id;
        input.comment = this.comment;

        this.act(() => this._inboxService.sendBack(input), 'RequestSentBack');
    }

    private act(call: () => any, successKey: string): void {
        this.acting.set(true);

        call()
            .pipe(finalize(() => this.acting.set(false)))
            .subscribe(() => {
                this.notify.success(this.l(successKey));

                // Reloaded rather than removed locally: on a multi-step chain an approval moves the request
                // to the next approver rather than finishing it, and the server is the only thing that knows
                // which happened.
                this.reload();
            });
    }

    summary(item: EssInboxItemDto): string {
        return this.isRtl ? item.summaryAr || item.summaryEn : item.summaryEn;
    }

    typeName(item: EssInboxItemDto): string {
        return this.isRtl ? item.requestTypeNameAr || item.requestTypeNameEn : item.requestTypeNameEn;
    }

    requesterName(item: EssInboxItemDto): string {
        return this.isRtl ? item.requesterNameAr || item.requesterNameEn : item.requesterNameEn;
    }

    initials(item: EssInboxItemDto): string {
        const name = (this.requesterName(item) || '').trim();
        if (!name) {
            return '?';
        }

        const parts = name.split(/\s+/);
        return (parts[0][0] + (parts.length > 1 ? parts[parts.length - 1][0] : '')).toUpperCase();
    }

    private get isRtl(): boolean {
        return document?.documentElement?.getAttribute('dir') === 'rtl';
    }
}
