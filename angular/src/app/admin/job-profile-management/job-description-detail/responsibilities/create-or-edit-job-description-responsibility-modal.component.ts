import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    JobResponsibilitiesServiceProxy,
    CreateOrEditJobDescriptionResponsibilityDto,
    JobDescriptionResponsibilitiesServiceProxy,
    GetAllSimpleLookupsInput,
    SimpleLookupDto,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditJobDescriptionResponsibilityModal',
    templateUrl: './create-or-edit-job-description-responsibility-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditJobDescriptionResponsibilityModalComponent extends AppComponentBase {
    private _service = inject(JobDescriptionResponsibilitiesServiceProxy);
    private _jobResponsibilitiesService = inject(JobResponsibilitiesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditJobDescriptionResponsibilityDto>(
        new CreateOrEditJobDescriptionResponsibilityDto()
    );
    readonly jobResponsibilities = signal<SimpleLookupDto[]>([]);

    show(jobDescriptionId: number, id?: number): void {
        this.active.set(true);

        this._jobResponsibilitiesService
            .getAll(
                new GetAllSimpleLookupsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.jobResponsibilities.set(result.items));

        if (!id) {
            const record = new CreateOrEditJobDescriptionResponsibilityDto();
            record.jobDescriptionId = jobDescriptionId;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getJobDescriptionResponsibilityForEdit(id).subscribe((result) => {
                this.record.set(result.jobDescriptionResponsibility);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('JobDescriptionResponsibility_JobResponsibilityId')?.focus();
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

    updateField(field: keyof CreateOrEditJobDescriptionResponsibilityDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditJobDescriptionResponsibilityDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
