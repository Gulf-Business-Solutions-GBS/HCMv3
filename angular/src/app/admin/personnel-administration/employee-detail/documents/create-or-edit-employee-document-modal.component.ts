import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditEmployeeDocumentDto,
    DocumentTypesServiceProxy,
    EmployeeDocumentsServiceProxy,
    GetAllDocumentTypesInput,
    DocumentTypeDto,
    DocumentPlacesServiceProxy,
    GetAllSimpleLookupsInput,
    SimpleLookupDto,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';
import { AttachmentsComponent } from '@app/shared/common/attachments/attachments.component';
import { DateInputDirective } from '@shared/common/dates/date-input.directive';
import { RecordDirectlyCheckComponent } from '@app/shared/common/record-directly/record-directly-check.component';

@Component({
    selector: 'createOrEditEmployeeDocumentModal',
    templateUrl: './create-or-edit-employee-document-modal.component.html',
    imports: [
        RecordDirectlyCheckComponent,
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
        AttachmentsComponent,
    ],
})
export class CreateOrEditEmployeeDocumentModalComponent extends AppComponentBase {
    private _service = inject(EmployeeDocumentsServiceProxy);
    private _documentTypesService = inject(DocumentTypesServiceProxy);
    private _documentPlacesService = inject(DocumentPlacesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeDocumentDto>(new CreateOrEditEmployeeDocumentDto());
    readonly documentTypes = signal<DocumentTypeDto[]>([]);
    readonly documentPlaces = signal<SimpleLookupDto[]>([]);

    show(employeeId: number, id?: number): void {
        this.active.set(true);

        this._documentTypesService
            .getAll(
                new GetAllDocumentTypesInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                    onlyLetterTypes: false,
                })
            )
            .subscribe((result) => this.documentTypes.set(result.items));

        this._documentPlacesService
            .getAll(
                new GetAllSimpleLookupsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.documentPlaces.set(result.items));

        if (!id) {
            const record = new CreateOrEditEmployeeDocumentDto();
            record.employeeId = employeeId;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getEmployeeDocumentForEdit(id).subscribe((result) => {
                this.record.set(result.employeeDocument);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('EmployeeDocument_DocumentTypeId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._service
            .createOrEdit(this.record())
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

    updateField(field: keyof CreateOrEditEmployeeDocumentDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeDocumentDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: 'issueDate' | 'expiryDate', value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }
}
