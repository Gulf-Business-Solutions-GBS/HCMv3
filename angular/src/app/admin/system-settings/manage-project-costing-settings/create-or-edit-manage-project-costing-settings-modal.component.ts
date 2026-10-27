import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    ProjectCostingSettingsEditDto,
    ManageProjectCostingSettingsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditManageProjectCostingSettingsModal',
    templateUrl: './create-or-edit-manage-project-costing-settings-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditManageProjectCostingSettingsModalComponent extends AppComponentBase {
    private _service = inject(ManageProjectCostingSettingsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<ProjectCostingSettingsEditDto>(new ProjectCostingSettingsEditDto());

    show(id?: number): void {
        this.active.set(true);

        if (!id) {
            const record = new ProjectCostingSettingsEditDto();
            record.calculateNetPayProjectWise = false;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getProjectCostingSettingsForEdit(id).subscribe((result) => {
                this.record.set(result);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('ProjectCostingSettings_CalculateNetPayProjectWise')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._service
            .createOrEditProjectCostingSettings(this.record())
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe(() => {
                this.notify.info(this.l('SavedSuccessfully'));
                this.close();
                this.modalSave.emit();
            });
    }

    close(): void {
        this.active.set(false);
        this.modal().hide();
    }

    updateField(field: keyof ProjectCostingSettingsEditDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new ProjectCostingSettingsEditDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
