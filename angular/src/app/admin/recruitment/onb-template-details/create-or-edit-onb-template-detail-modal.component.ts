import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditOnbTemplateDetailsDto,
    GetAllOnbTemplatesInput,
    OnbTemplateDetailsServiceProxy,
    OnbTemplateDto,
    OnbTemplatesServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditOnbTemplateDetailModal',
    templateUrl: './create-or-edit-onb-template-detail-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditOnbTemplateDetailModalComponent extends AppComponentBase {
    private _service = inject(OnbTemplateDetailsServiceProxy);
    private _onbTemplatesService = inject(OnbTemplatesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditOnbTemplateDetailsDto>(new CreateOrEditOnbTemplateDetailsDto());

    readonly onbTemplates = signal<OnbTemplateDto[]>([]);

    show(id?: number): void {
        this.active.set(true);

        this._onbTemplatesService
            .getAll(new GetAllOnbTemplatesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.onbTemplates.set(result.items));

        if (!id) {
            this.record.set(new CreateOrEditOnbTemplateDetailsDto());
            this.modal().show();
        } else {
            this._service.getOnbTemplateDetailsForEdit(id).subscribe((result) => {
                this.record.set(result.onbTemplateDetails);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('OnbTemplateDetail_OnbTemplateId')?.focus();
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

    updateField(field: keyof CreateOrEditOnbTemplateDetailsDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditOnbTemplateDetailsDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
