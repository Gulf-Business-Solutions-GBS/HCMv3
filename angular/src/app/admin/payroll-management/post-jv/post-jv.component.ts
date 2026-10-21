import { Component, computed, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { Tooltip } from 'primeng/tooltip';
import { finalize } from 'rxjs/operators';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { SubHeaderComponent } from '../../../shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '../../../../shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import { DateInputDirective } from '@shared/common/dates/date-input.directive';

// PROXY REGENERATION REQUIRED: PostJvServiceProxy's period context, employee popup, process log, RequestStop and
// FinanceRunStartedDto come from the backend changed in the same piece of work.
import { PostJvServiceProxy } from '@shared/service-proxies/service-proxies';
import { FinanceScreenBase } from '../costing-file/finance-screen-base';
import { FinanceRunStatusComponent } from '../costing-file/finance-run-status.component';
import { FinanceRunLogModalComponent } from '../costing-file/finance-run-log-modal.component';
import { CostingEmployeePickerModalComponent } from '../costing-file/costing-employee-picker-modal.component';
import { HelpPageDirective } from '@app/shared/help/help-page.directive';

/**
 * Post JV to Dynamics 365. Ported from the Post JV half of legacy uscCostingFile.ascx (CreatePostJV ->
 * PostTablewiseJV_D360); the old AX screen uscPostJV.ascx is hidden in legacy.
 *
 * Posting needs a preview that is still current: any change to the inputs clears it, and the server refuses a
 * JV whose fingerprint no longer matches the preview, one that does not balance, and any posting while Dynamics
 * 365 Finance is switched off in the Integration Center. The posting then runs in the background and the status
 * panel follows it; "Show details" lists every journal header and line batch with D365's full reply.
 *
 * As in legacy, a Regular run posts the area as one journal (two with Separate Provision Posting) and an OffCycle
 * run posts one journal per employee.
 */
@Component({
    templateUrl: './post-jv.component.html',
    animations: [appModuleAnimation()],
    imports: [
        HelpPageDirective,
        SubHeaderComponent,
        BusyIfDirective,
        FormsModule,
        TableModule,
        Tooltip,
        LocalizePipe,
        PermissionPipe,
        LuxonFormatPipe,
        DateInputDirective,
        FinanceRunStatusComponent,
        FinanceRunLogModalComponent,
        CostingEmployeePickerModalComponent,
    ],
})
export class PostJvComponent extends FinanceScreenBase {
    protected readonly api = inject(PostJvServiceProxy);

    readonly canPost = computed(
        () =>
            this.canSubmit() &&
            !this.isRunning() &&
            this.selectedAreaIsFiConfigured() &&
            this.lines().length > 0 &&
            this.isBalanced() &&
            !!this.previewFingerprint()
    );

    readonly postBlockedReason = computed(() => {
        if (this.canPost() || !this.canSubmit()) {
            return '';
        }

        if (!this.previewFingerprint() || !this.lines().length) {
            return this.l('JournalVoucherPreviewRequired');
        }

        return this.isBalanced() ? '' : this.l('DebitCreditNotMatched');
    });

    /** Legacy "Post JV" button. */
    postJournalVoucher(): void {
        if (!this.canPost()) {
            return;
        }

        this.message.confirm(this.l('PostJournalVoucher'), this.l('AreYouSure'), (isConfirmed) => {
            if (!isConfirmed) {
                return;
            }

            const input = this.buildInput();
            input.previewFingerprint = this.previewFingerprint();

            this.busy.set(true);
            this.api
                .postJournalVoucher(input)
                .pipe(finalize(() => this.busy.set(false)))
                .subscribe((started) => this.onRunQueued(started.message));
        });
    }
}
