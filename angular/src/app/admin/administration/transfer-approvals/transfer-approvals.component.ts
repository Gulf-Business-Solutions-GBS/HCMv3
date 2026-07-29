import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';

import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import {
    AdminToolResultDto,
    EmployeeDto,
    EmployeesServiceProxy,
    GetAllEmployeesInput,
    TransferApprovalsInput,
    WorkflowAdminToolsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';

/**
 * Port of the "Transfer Leaves" panel of legacy
 * HCM10.Web/UserControls/Administration/uscWorkFlowConfigPanel.ascx (line 96).
 *
 * Despite the legacy caption this moves ALL open workflow approvals, not just leave requests, which is why
 * the confirmation spells the scope out before anything is reassigned.
 */
@Component({
    templateUrl: './transfer-approvals.component.html',
    animations: [appModuleAnimation()],
    imports: [SubHeaderComponent, BusyIfDirective, FormsModule, LocalizePipe, PermissionPipe],
})
export class TransferApprovalsComponent extends AppComponentBase implements OnInit {
    private _workflowAdminToolsService = inject(WorkflowAdminToolsServiceProxy);
    private _employeesService = inject(EmployeesServiceProxy);

    readonly employees = signal<EmployeeDto[]>([]);

    readonly fromApproverEmployeeId = signal<number | undefined>(undefined);
    readonly toApproverEmployeeId = signal<number | undefined>(undefined);
    readonly requestingEmployeeId = signal<number | undefined>(undefined);

    readonly saving = signal<boolean>(false);
    readonly result = signal<AdminToolResultDto | undefined>(undefined);
    readonly errorMessage = signal<string>('');

    ngOnInit(): void {
        this._employeesService
            .getAll(
                new GetAllEmployeesInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: 'code',
                    filter: undefined,
                })
            )
            .subscribe((r) => this.employees.set(r.items));
    }

    employeeName(id: number | undefined): string {
        if (!id) {
            return this.l('AllEmployees');
        }

        const employee = this.employees().find((e) => e.id === id);
        return employee ? employee.code + ' - ' + employee.fullNameEn : String(id);
    }

    transfer(): void {
        if (!this.fromApproverEmployeeId()) {
            this.message.warn(this.l('TransferFromManagerNotFound'));
            return;
        }

        if (!this.toApproverEmployeeId()) {
            this.message.warn(this.l('TransferToManagerNotFound'));
            return;
        }

        if (this.fromApproverEmployeeId() === this.toApproverEmployeeId()) {
            this.message.warn(this.l('TransferFromAndToManagerMustDiffer'));
            return;
        }

        this.message.confirm(
            this.l(
                'ConfirmTransferPendingApprovals',
                this.employeeName(this.fromApproverEmployeeId()),
                this.employeeName(this.toApproverEmployeeId()),
                this.employeeName(this.requestingEmployeeId())
            ),
            this.l('AreYouSure'),
            (isConfirmed) => {
                if (!isConfirmed) {
                    return;
                }

                this.saving.set(true);
                this.result.set(undefined);
                this.errorMessage.set('');

                this._workflowAdminToolsService
                    .transferPendingApprovals(
                        new TransferApprovalsInput({
                            fromApproverEmployeeId: this.fromApproverEmployeeId(),
                            toApproverEmployeeId: this.toApproverEmployeeId(),
                            requestingEmployeeId: this.requestingEmployeeId(),
                        })
                    )
                    .pipe(finalize(() => this.saving.set(false)))
                    .subscribe({
                        next: (r) => {
                            this.result.set(r);
                            this.notify.success(this.l('AdminToolResultSummary', r.affectedCount, r.skippedCount));
                        },
                        error: (e) => this.errorMessage.set(e?.message || e?.details || ''),
                    });
            }
        );
    }

    clear(): void {
        this.fromApproverEmployeeId.set(undefined);
        this.toApproverEmployeeId.set(undefined);
        this.requestingEmployeeId.set(undefined);
    }
}
