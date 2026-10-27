import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditOrganizationDocumentDto,
    DocumentTypeDto,
    DocumentTypesServiceProxy,
    GetAllDocumentTypesInput,
    OrganizationDocumentsServiceProxy,
    OrganizationUnitDto,
    OrganizationUnitServiceProxy,
    SponsorDto,
    GetAllSponsorsInput,
    SponsorsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';
import { DateInputDirective } from '@shared/common/dates/date-input.directive';
import { AttachmentsComponent } from '@app/shared/common/attachments/attachments.component';

@Component({
    selector: 'createOrEditOrganizationDocumentModal',
    templateUrl: './create-or-edit-organization-document-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
        AttachmentsComponent,
    ],
})
export class CreateOrEditOrganizationDocumentModalComponent extends AppComponentBase {
    private _service = inject(OrganizationDocumentsServiceProxy);
    private _documentTypesService = inject(DocumentTypesServiceProxy);
    private _organizationUnitService = inject(OrganizationUnitServiceProxy);
    private _sponsorsService = inject(SponsorsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditOrganizationDocumentDto>(new CreateOrEditOrganizationDocumentDto());

    readonly documentTypes = signal<DocumentTypeDto[]>([]);
    readonly organizationUnits = signal<OrganizationUnitDto[]>([]);
    readonly sponsors = signal<SponsorDto[]>([]);

    show(id?: number): void {
        this.active.set(true);

        this.loadLookups();

        if (!id) {
            this.record.set(new CreateOrEditOrganizationDocumentDto());
            this.modal().show();
        } else {
            this._service.getOrganizationDocumentForEdit(id).subscribe((result) => {
                this.record.set(result.organizationDocument);
                this.modal().show();
            });
        }
    }

    private loadLookups(): void {
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

        this._organizationUnitService
            .getOrganizationUnits()
            .subscribe((result) => this.organizationUnits.set(result.items ?? []));

        this._sponsorsService
            .getAll(
                new GetAllSponsorsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined })
            )
            .subscribe((result) => this.sponsors.set(result.items));
    }

    onShown(): void {
        document.getElementById('OrganizationDocument_OrganizationUnitId')?.focus();
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

    updateField(field: keyof CreateOrEditOrganizationDocumentDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditOrganizationDocumentDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: 'issueDate' | 'expiryDate', value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }
}
