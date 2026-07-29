import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';

import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import {
    CopyWorkflowSetupInput,
    GetAllRequestMastersInput,
    RequestMasterDto,
    RequestMastersServiceProxy,
    WorkflowAdminToolsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';

/**
 * Port of the legacy "clone a workflow request setup" utility
 * (HCM10.Web/UserControls/uscCopyWorkflowSetup.ascx + .ascx.cs).
 *
 * Legacy was a three-field callback panel (source request combo, new code, new name) that duplicated a
 * RequestMaster and all of its RequestDetail step rows. The Arabic-name field is this port's addition --
 * RequestMaster.NameAr is required here but legacy's form had no such field, so leaving it blank copies the
 * English name (see CopyWorkflowSetupInput).
 */
@Component({
    templateUrl: './copy-workflow-setup.component.html',
    animations: [appModuleAnimation()],
    imports: [SubHeaderComponent, BusyIfDirective, FormsModule, LocalizePipe, PermissionPipe],
})
export class CopyWorkflowSetupComponent extends AppComponentBase implements OnInit {
    private _workflowAdminToolsService = inject(WorkflowAdminToolsServiceProxy);
    private _requestMastersService = inject(RequestMastersServiceProxy);

    readonly requestMasters = signal<RequestMasterDto[]>([]);
    readonly sourceRequestMasterId = signal<number | undefined>(undefined);
    readonly newRequestCode = signal<string>('');
    readonly newRequestNameEn = signal<string>('');
    readonly newRequestNameAr = signal<string>('');
    readonly saving = signal<boolean>(false);
    readonly messages = signal<string[]>([]);

    ngOnInit(): void {
        // Legacy BindRequest (lines 39-54): every RequestMaster is offered as a clone source.
        this._requestMastersService
            .getAll(
                new GetAllRequestMastersInput({
                    filter: undefined,
                    sorting: undefined,
                    maxResultCount: 1000,
                    skipCount: 0,
                })
            )
            .subscribe((result) => this.requestMasters.set(result.items));
    }

    /** Legacy ASPxCallbackPanel1_Callback "btnSubmit" (lines 96-186). */
    copy(): void {
        // Same order and wording the app service validates in (legacy lines 99-107).
        if (!this.newRequestCode() || !this.newRequestCode().trim()) {
            this.message.warn(this.l('CopyWorkflowSetupRequestCodeRequired'));
            return;
        }

        if (!this.newRequestNameEn()) {
            this.message.warn(this.l('CopyWorkflowSetupRequestNameRequired'));
            return;
        }

        if (!this.sourceRequestMasterId()) {
            this.message.warn(this.l('CopyWorkflowSetupSourceRequestRequired'));
            return;
        }

        this.saving.set(true);
        this._workflowAdminToolsService
            .copyWorkflowSetup(
                new CopyWorkflowSetupInput({
                    sourceRequestMasterId: this.sourceRequestMasterId(),
                    newRequestCode: this.newRequestCode().trim(),
                    newRequestNameEn: this.newRequestNameEn(),
                    newRequestNameAr: this.newRequestNameAr() ? this.newRequestNameAr() : undefined,
                })
            )
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe((result) => {
                this.messages.set(result.messages || []);
                this.notify.success(this.l('CopyWorkflowSetupSucceeded'));
                this.clear();
                this.ngOnInit();
            });
    }

    /** Legacy ClearControll (lines 68-73). */
    clear(): void {
        this.sourceRequestMasterId.set(undefined);
        this.newRequestCode.set('');
        this.newRequestNameEn.set('');
        this.newRequestNameAr.set('');
    }
}
