import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditDependantTypeDto,
    DependantTypesServiceProxy,
    Gender,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditDependantTypeModal',
    templateUrl: './create-or-edit-dependant-type-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditDependantTypeModalComponent extends AppComponentBase {
    private _dependantTypesService = inject(DependantTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly dependantType = signal<CreateOrEditDependantTypeDto>(new CreateOrEditDependantTypeDto());

    readonly genders = [
        { value: Gender.Male, label: 'Male' },
        { value: Gender.Female, label: 'Female' },
    ];

    show(dependantTypeId?: number): void {
        this.active.set(true);

        if (!dependantTypeId) {
            this.dependantType.set(new CreateOrEditDependantTypeDto());
            this.modal().show();
        } else {
            this._dependantTypesService.getDependantTypeForEdit(dependantTypeId).subscribe((result) => {
                this.dependantType.set(result.dependantType);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('DependantType_NameEn')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._dependantTypesService
            .createOrEdit(this.dependantType())
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

    // Signal-friendly field updaters (keep template bindings immutable).
    updateField(field: keyof CreateOrEditDependantTypeDto, value: any): void {
        const current = this.dependantType();
        (current as any)[field] = value;
        const next = new CreateOrEditDependantTypeDto();
        Object.assign(next, current);
        this.dependantType.set(next);
    }
}
