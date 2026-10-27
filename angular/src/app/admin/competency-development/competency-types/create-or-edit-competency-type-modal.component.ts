import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { CreateOrEditCompetencyTypeDto, CompetencyTypesServiceProxy } from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditCompetencyTypeModal',
    templateUrl: './create-or-edit-competency-type-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditCompetencyTypeModalComponent extends AppComponentBase {
    private _service = inject(CompetencyTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly competencyType = signal<CreateOrEditCompetencyTypeDto>(new CreateOrEditCompetencyTypeDto());

    show(id?: number): void {
        this.active.set(true);

        if (!id) {
            this.competencyType.set(new CreateOrEditCompetencyTypeDto());
            this.modal().show();
        } else {
            this._service.getCompetencyTypeForEdit(id).subscribe((result) => {
                this.competencyType.set(result.competencyType);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('CompetencyType_Code')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._service
            .createOrEdit(this.competencyType())
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

    updateField(field: keyof CreateOrEditCompetencyTypeDto, value: any): void {
        const current = this.competencyType();
        (current as any)[field] = value;
        const next = new CreateOrEditCompetencyTypeDto();
        Object.assign(next, current);
        this.competencyType.set(next);
    }
}
