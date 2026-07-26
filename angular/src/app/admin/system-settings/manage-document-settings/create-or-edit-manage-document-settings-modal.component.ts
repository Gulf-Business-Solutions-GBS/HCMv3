import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { DocumentSettingsEditDto, ManageDocumentSettingsServiceProxy } from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditManageDocumentSettingsModal',
    templateUrl: './create-or-edit-manage-document-settings-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditManageDocumentSettingsModalComponent extends AppComponentBase {
    private _service = inject(ManageDocumentSettingsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<DocumentSettingsEditDto>(new DocumentSettingsEditDto());

    show(id?: number): void {
        this.active.set(true);

        if (!id) {
            const record = new DocumentSettingsEditDto();
            record.enableDocSaveInFileSystem = false;
            record.fileSystemAttachmentPath = '';
            record.maxAttachmentsPerTransaction = 0;
            record.allowedFileExtensions = '';
            record.maxFileSizePerAttachmentMB = 0;
            record.maxStorageSizeMB = 0;
            record.storageWarningThresholdMB = 0;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getDocumentSettingsForEdit(id).subscribe((result) => {
                this.record.set(result);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('DocumentSettings_FileSystemAttachmentPath')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._service
            .createOrEditDocumentSettings(this.record())
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

    updateField(field: keyof DocumentSettingsEditDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new DocumentSettingsEditDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateNumberField(field: keyof DocumentSettingsEditDto, value: string): void {
        this.updateField(field, value !== null && value !== undefined && value !== '' ? Number(value) : 0);
    }
}
