import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CompetenciesServiceProxy,
    CompetencyTypeDto,
    CompetencyTypesServiceProxy,
    CreateOrEditCompetencyDto,
    GetAllCompetencyTypesInput,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditCompetencyModal',
    templateUrl: './create-or-edit-competency-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditCompetencyModalComponent extends AppComponentBase {
    private _service = inject(CompetenciesServiceProxy);
    private _competencyTypesService = inject(CompetencyTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly competency = signal<CreateOrEditCompetencyDto>(new CreateOrEditCompetencyDto());
    readonly competencyTypes = signal<CompetencyTypeDto[]>([]);

    show(id?: number): void {
        this.active.set(true);

        this._competencyTypesService
            .getAll(new GetAllCompetencyTypesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.competencyTypes.set(result.items));

        if (!id) {
            const record = new CreateOrEditCompetencyDto();
            record.active = true;
            this.competency.set(record);
            this.modal().show();
        } else {
            this._service.getCompetencyForEdit(id).subscribe((result) => {
                this.competency.set(result.competency);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('Competency_Code')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._service
            .createOrEdit(this.competency())
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

    updateField(field: keyof CreateOrEditCompetencyDto, value: any): void {
        const current = this.competency();
        (current as any)[field] = value;
        const next = new CreateOrEditCompetencyDto();
        Object.assign(next, current);
        this.competency.set(next);
    }
}
