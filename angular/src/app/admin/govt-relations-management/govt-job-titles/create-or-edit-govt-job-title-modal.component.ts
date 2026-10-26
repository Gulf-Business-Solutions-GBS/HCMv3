import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditGovtJobTitleDto,
    GetAllSimpleLookupsInput,
    GovtJobCategoriesServiceProxy,
    GovtJobTitlesServiceProxy,
    SimpleLookupDto,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditGovtJobTitleModal',
    templateUrl: './create-or-edit-govt-job-title-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditGovtJobTitleModalComponent extends AppComponentBase {
    private _service = inject(GovtJobTitlesServiceProxy);
    private _govtJobCategoriesService = inject(GovtJobCategoriesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditGovtJobTitleDto>(new CreateOrEditGovtJobTitleDto());
    readonly govtJobCategories = signal<SimpleLookupDto[]>([]);

    show(id?: number): void {
        this.active.set(true);

        this._govtJobCategoriesService
            .getAll(
                new GetAllSimpleLookupsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.govtJobCategories.set(result.items));

        if (!id) {
            this.record.set(new CreateOrEditGovtJobTitleDto());
            this.modal().show();
        } else {
            this._service.getGovtJobTitleForEdit(id).subscribe((result) => {
                this.record.set(result.govtJobTitle);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('GovtJobTitle_NameEn')?.focus();
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

    updateField(field: keyof CreateOrEditGovtJobTitleDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditGovtJobTitleDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
