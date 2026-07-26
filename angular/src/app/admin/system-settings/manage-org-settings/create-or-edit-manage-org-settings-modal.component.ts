import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { OrgSettingsEditDto, ManageOrgSettingsServiceProxy } from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditManageOrgSettingsModal',
    templateUrl: './create-or-edit-manage-org-settings-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditManageOrgSettingsModalComponent extends AppComponentBase {
    private _service = inject(ManageOrgSettingsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<OrgSettingsEditDto>(new OrgSettingsEditDto());

    show(id?: number): void {
        this.active.set(true);

        if (!id) {
            const record = new OrgSettingsEditDto();
            record.enablePostingGroup = false;
            record.enableBudgetingGroup = false;
            record.enablePosition = false;
            record.enableOrgChart3Levels = false;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getOrgSettingsForEdit(id).subscribe((result) => {
                this.record.set(result);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('OrgSettings_EnablePostingGroup')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._service
            .createOrEditOrgSettings(this.record())
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

    updateField(field: keyof OrgSettingsEditDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new OrgSettingsEditDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
