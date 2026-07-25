import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditJobDescriptionDto,
    GetAllSimpleLookupsInput,
    JobDescriptionsServiceProxy,
    JobTitlesServiceProxy,
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

@Component({
    selector: 'createOrEditJobDescriptionModal',
    templateUrl: './create-or-edit-job-description-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditJobDescriptionModalComponent extends AppComponentBase {
    private _service = inject(JobDescriptionsServiceProxy);
    private _jobTitlesService = inject(JobTitlesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly jobDescription = signal<CreateOrEditJobDescriptionDto>(new CreateOrEditJobDescriptionDto());
    readonly jobTitles = signal<SimpleLookupDto[]>([]);

    show(id?: number): void {
        this.active.set(true);

        this._jobTitlesService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.jobTitles.set(result.items));

        if (!id) {
            this.jobDescription.set(new CreateOrEditJobDescriptionDto());
            this.modal().show();
        } else {
            this._service.getJobDescriptionForEdit(id).subscribe((result) => {
                this.jobDescription.set(result.jobDescription);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('JobDescription_JobTitleId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._service
            .createOrEdit(this.jobDescription())
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

    updateField(field: keyof CreateOrEditJobDescriptionDto, value: any): void {
        const current = this.jobDescription();
        (current as any)[field] = value;
        const next = new CreateOrEditJobDescriptionDto();
        Object.assign(next, current);
        this.jobDescription.set(next);
    }

    updateVersionChangedOn(value: string): void {
        this.updateField('versionChangedOn', value ? DateTime.fromISO(value) : (undefined as any));
    }
}
