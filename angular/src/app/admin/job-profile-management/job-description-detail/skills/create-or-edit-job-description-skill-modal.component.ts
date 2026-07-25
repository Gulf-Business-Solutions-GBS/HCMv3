import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    SkillsServiceProxy,
    CreateOrEditJobDescriptionSkillDto,
    JobDescriptionSkillsServiceProxy,
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
    selector: 'createOrEditJobDescriptionSkillModal',
    templateUrl: './create-or-edit-job-description-skill-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditJobDescriptionSkillModalComponent extends AppComponentBase {
    private _service = inject(JobDescriptionSkillsServiceProxy);
    private _skillsService = inject(SkillsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditJobDescriptionSkillDto>(new CreateOrEditJobDescriptionSkillDto());
    readonly skills = signal<SimpleLookupDto[]>([]);

    show(jobDescriptionId: number, id?: number): void {
        this.active.set(true);

        this._skillsService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.skills.set(result.items));

        if (!id) {
            const record = new CreateOrEditJobDescriptionSkillDto();
            record.jobDescriptionId = jobDescriptionId;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getJobDescriptionSkillForEdit(id).subscribe((result) => {
                this.record.set(result.jobDescriptionSkill);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('JobDescriptionSkill_SkillId')?.focus();
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

    updateField(field: keyof CreateOrEditJobDescriptionSkillDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditJobDescriptionSkillDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
