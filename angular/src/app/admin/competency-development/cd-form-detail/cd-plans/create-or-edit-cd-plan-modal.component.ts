import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CDPlansServiceProxy,
    CompetenciesServiceProxy,
    CompetencyDto,
    CreateOrEditCDPlanDto,
    GetAllCompetenciesInput,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditCDPlanModal',
    templateUrl: './create-or-edit-cd-plan-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditCDPlanModalComponent extends AppComponentBase {
    private _service = inject(CDPlansServiceProxy);
    private _competenciesService = inject(CompetenciesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditCDPlanDto>(new CreateOrEditCDPlanDto());
    readonly competencies = signal<CompetencyDto[]>([]);

    show(cdFormId: number, id?: number): void {
        this.active.set(true);

        this._competenciesService
            .getAll(new GetAllCompetenciesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined, competencyTypeId: undefined }))
            .subscribe((result) => this.competencies.set(result.items));

        if (!id) {
            const record = new CreateOrEditCDPlanDto();
            record.cdFormId = cdFormId;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getCDPlanForEdit(id).subscribe((result) => {
                this.record.set(result.cdPlan);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('CDPlan_CompetencyId')?.focus();
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

    updateField(field: keyof CreateOrEditCDPlanDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditCDPlanDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
