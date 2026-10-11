import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    DocumentTypesServiceProxy,
    CreateOrEditDocumentTypeDto,
    NotificationConfigDto,
    GetAllNotificationConfigsInput,
    NotificationConfigsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditDocumentTypeModal',
    templateUrl: './create-or-edit-document-type-modal.component.html',
    imports: [
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditDocumentTypeModalComponent extends AppComponentBase {
    private _documentTypeService = inject(DocumentTypesServiceProxy);
    private _notificationConfigsService = inject(NotificationConfigsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly documentType = signal<CreateOrEditDocumentTypeDto>(new CreateOrEditDocumentTypeDto());
    readonly notificationConfigs = signal<NotificationConfigDto[]>([]);

    show(documentTypeId?: number): void {
        this.active.set(true);

        this._notificationConfigsService
            .getAll(new GetAllNotificationConfigsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => {
                this.notificationConfigs.set(result.items);
            });

        if (!documentTypeId) {
            const documentType = new CreateOrEditDocumentTypeDto();

            // Offered to employees unless somebody says otherwise, as every existing type is.
            documentType.includeInWorkflow = true;

            this.documentType.set(documentType);
            this.modal().show();
        } else {
            this._documentTypeService.getDocumentTypeForEdit(documentTypeId).subscribe((result) => {
                this.documentType.set(result.documentType);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('DocumentType_NameEn')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._documentTypeService
            .createOrEdit(this.documentType())
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

    // Signal-friendly field updaters (keep template bindings immutable).
    updateField(field: keyof CreateOrEditDocumentTypeDto, value: any): void {
        const current = this.documentType();
        (current as any)[field] = value;
        const next = new CreateOrEditDocumentTypeDto();
        Object.assign(next, current);
        this.documentType.set(next);
    }
}
