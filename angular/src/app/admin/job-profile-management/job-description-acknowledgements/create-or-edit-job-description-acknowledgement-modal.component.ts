import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditJobDescriptionAcknowledgementDto,
    EmployeeDto,
    EmployeesServiceProxy,
    GetAllEmployeesInput,
    GetAllJobDescriptionsInput,
    GetAllSimpleLookupsInput,
    JobDescriptionAcknowledgementsServiceProxy,
    JobDescriptionDto,
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

@Component({
    selector: 'createOrEditJobDescriptionAcknowledgementModal',
    templateUrl: './create-or-edit-job-description-acknowledgement-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditJobDescriptionAcknowledgementModalComponent extends AppComponentBase {
    private _service = inject(JobDescriptionAcknowledgementsServiceProxy);
    private _employeesService = inject(EmployeesServiceProxy);
    private _jobTitlesService = inject(JobTitlesServiceProxy);
    private _jobDescriptionsService = inject(JobDescriptionsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditJobDescriptionAcknowledgementDto>(
        new CreateOrEditJobDescriptionAcknowledgementDto()
    );
    readonly employees = signal<EmployeeDto[]>([]);
    readonly jobTitles = signal<SimpleLookupDto[]>([]);
    readonly jobDescriptions = signal<JobDescriptionDto[]>([]);

    show(id?: number): void {
        this.active.set(true);

        this._employeesService
            .getAll(
                new GetAllEmployeesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined })
            )
            .subscribe((result) => this.employees.set(result.items));

        this._jobTitlesService
            .getAll(
                new GetAllSimpleLookupsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.jobTitles.set(result.items));

        this._jobDescriptionsService
            .getAll(
                new GetAllJobDescriptionsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.jobDescriptions.set(result.items));

        if (!id) {
            this.record.set(new CreateOrEditJobDescriptionAcknowledgementDto());
            this.modal().show();
        } else {
            this._service.getJobDescriptionAcknowledgementForEdit(id).subscribe((result) => {
                this.record.set(result.jobDescriptionAcknowledgement);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('JobDescriptionAcknowledgement_EmployeeId')?.focus();
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

    employeeLabel(employee: EmployeeDto): string {
        return `${employee.code} - ${employee.fullNameEn}`;
    }

    jobDescriptionLabel(jobDescription: JobDescriptionDto): string {
        return `${jobDescription.jobTitleNameEn} - ${jobDescription.shortDescriptionEn}`;
    }

    updateField(field: keyof CreateOrEditJobDescriptionAcknowledgementDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditJobDescriptionAcknowledgementDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
