import { Component, inject } from '@angular/core';
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

// PROXY REGENERATION REQUIRED: the CostingFileServiceProxy methods and DTOs used here (period context, employee
// popup, process log, FinanceRunStartedDto, ...) come from the backend changed in the same piece of work.
import { CostingFileServiceProxy } from '@shared/service-proxies/service-proxies';
import { FinanceScreenBase } from './finance-screen-base';
import { FinanceRunStatusComponent } from './finance-run-status.component';
import { FinanceRunLogModalComponent } from './finance-run-log-modal.component';
import { CostingEmployeePickerModalComponent } from './costing-employee-picker-modal.component';
import { downloadBase64File } from './finance-screen-api';
import { HelpPageDirective } from '@app/shared/help/help-page.directive';

/**
 * Costing File screen. Ported from legacy HCM10.Web/UserControls/PayrollManagement/uscCostingFile.ascx (the
 * cost-engine half; the Post JV half is the Post JV screen).
 *
 * Generate Costing File queues the cost engine on the server and returns; the status panel then follows the
 * run live -- progress, the employee being processed, Stop, and "Show details" with every employee's result --
 * as legacy's callback panel and timer did.
 */
@Component({
    templateUrl: './costing-file.component.html',
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
export class CostingFileComponent extends FinanceScreenBase {
    protected readonly api = inject(CostingFileServiceProxy);

    /** Legacy "Generate Costing File" button. */
    generateCostingFile(): void {
        if (!this.canSubmit() || this.isRunning()) {
            return;
        }

        this.busy.set(true);
        this.api
            .generateCostingFile(this.buildInput())
            .pipe(finalize(() => this.busy.set(false)))
            .subscribe((started) => this.onRunQueued(started.message));
    }

    /** Legacy "Export Costing File" button. */
    exportCostingFile(): void {
        if (!this.canSubmit()) {
            return;
        }

        this.busy.set(true);
        this.api
            .exportCostingFile(this.buildInput())
            .pipe(finalize(() => this.busy.set(false)))
            .subscribe((result) => downloadBase64File(result));
    }
}
