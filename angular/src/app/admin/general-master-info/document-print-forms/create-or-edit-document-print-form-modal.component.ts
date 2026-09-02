import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    DocumentPrintFormsServiceProxy,
    CreateOrEditDocumentPrintFormDto,
    DocumentTypeDto,
    GetAllDocumentTypesInput,
    DocumentTypesServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditDocumentPrintFormModal',
    templateUrl: './create-or-edit-document-print-form-modal.component.html',
    imports: [
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditDocumentPrintFormModalComponent extends AppComponentBase {
    private _documentPrintFormService = inject(DocumentPrintFormsServiceProxy);
    private _documentTypesService = inject(DocumentTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly documentPrintForm = signal<CreateOrEditDocumentPrintFormDto>(new CreateOrEditDocumentPrintFormDto());
    readonly documentTypes = signal<DocumentTypeDto[]>([]);

    show(documentPrintFormId?: number): void {
        this.active.set(true);

        this._documentTypesService
            .getAll(new GetAllDocumentTypesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined, onlyLetterTypes: false }))
            .subscribe((result) => {
                this.documentTypes.set(result.items);
            });

        if (!documentPrintFormId) {
            this.documentPrintForm.set(new CreateOrEditDocumentPrintFormDto());
            this.modal().show();
        } else {
            this._documentPrintFormService.getDocumentPrintFormForEdit(documentPrintFormId).subscribe((result) => {
                this.documentPrintForm.set(result.documentPrintForm);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('DocumentPrintForm_DocumentTypeId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._documentPrintFormService
            .createOrEdit(this.documentPrintForm())
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
    updateField(field: keyof CreateOrEditDocumentPrintFormDto, value: any): void {
        const current = this.documentPrintForm();
        (current as any)[field] = value;
        const next = new CreateOrEditDocumentPrintFormDto();
        Object.assign(next, current);
        this.documentPrintForm.set(next);
    }
}
