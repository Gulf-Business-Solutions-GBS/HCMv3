import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    DisciplinaryTypesServiceProxy,
    CreateOrEditDisciplinaryTypeDto,
    ViolationWageTypeDto,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditDisciplinaryTypeModal',
    templateUrl: './create-or-edit-disciplinary-type-modal.component.html',
    imports: [
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditDisciplinaryTypeModalComponent extends AppComponentBase {
    private _disciplinaryTypeService = inject(DisciplinaryTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly disciplinaryType = signal<CreateOrEditDisciplinaryTypeDto>(new CreateOrEditDisciplinaryTypeDto());
    // Legacy ViolationWageType: active deductions outside basic/calculated/arrear pay, not Net Pay.
    readonly wageTypes = signal<ViolationWageTypeDto[]>([]);

    show(disciplinaryTypeId?: number): void {
        this.active.set(true);

        if (!disciplinaryTypeId) {
            this.disciplinaryType.set(new CreateOrEditDisciplinaryTypeDto());
            this.loadWageTypes(undefined);
            this.modal().show();
        } else {
            this._disciplinaryTypeService.getDisciplinaryTypeForEdit(disciplinaryTypeId).subscribe((result) => {
                this.disciplinaryType.set(result.disciplinaryType);
                this.loadWageTypes(result.disciplinaryType.wageTypeId);
                this.modal().show();
            });
        }
    }

    // The wage type already on the record is always returned, so it stays visible.
    private loadWageTypes(currentWageTypeId: number | undefined): void {
        this._disciplinaryTypeService
            .getViolationWageTypes(currentWageTypeId)
            .subscribe((result) => this.wageTypes.set(result.items));
    }

    onShown(): void {
        document.getElementById('DisciplinaryType_NameEn')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._disciplinaryTypeService
            .createOrEdit(this.disciplinaryType())
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
    updateField(field: keyof CreateOrEditDisciplinaryTypeDto, value: any): void {
        const current = this.disciplinaryType();
        (current as any)[field] = value;
        const next = new CreateOrEditDisciplinaryTypeDto();
        Object.assign(next, current);
        this.disciplinaryType.set(next);
    }
}
