import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditDisciplinaryDeductionTypeDto,
    DisciplinaryDeductionTypesServiceProxy,
    DisciplinaryDeductionUnit,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditDisciplinaryDeductionTypeModal',
    templateUrl: './create-or-edit-disciplinary-deduction-type-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditDisciplinaryDeductionTypeModalComponent extends AppComponentBase {
    private _disciplinaryDeductionTypesService = inject(DisciplinaryDeductionTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly disciplinaryDeductionType = signal<CreateOrEditDisciplinaryDeductionTypeDto>(
        new CreateOrEditDisciplinaryDeductionTypeDto()
    );

    readonly deductionUnits = [
        { value: DisciplinaryDeductionUnit.Days, label: 'DisciplinaryDeductionUnitDays' },
        { value: DisciplinaryDeductionUnit.Hours, label: 'DisciplinaryDeductionUnitHours' },
        { value: DisciplinaryDeductionUnit.FixedAmount, label: 'DisciplinaryDeductionUnitFixedAmount' },
        { value: DisciplinaryDeductionUnit.Percentage, label: 'DisciplinaryDeductionUnitPercentage' },
    ];

    show(disciplinaryDeductionTypeId?: number): void {
        this.active.set(true);

        if (!disciplinaryDeductionTypeId) {
            this.disciplinaryDeductionType.set(new CreateOrEditDisciplinaryDeductionTypeDto());
            this.modal().show();
        } else {
            this._disciplinaryDeductionTypesService
                .getDisciplinaryDeductionTypeForEdit(disciplinaryDeductionTypeId)
                .subscribe((result) => {
                    this.disciplinaryDeductionType.set(result.disciplinaryDeductionType);
                    this.modal().show();
                });
        }
    }

    onShown(): void {
        document.getElementById('DisciplinaryDeductionType_NameEn')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._disciplinaryDeductionTypesService
            .createOrEdit(this.disciplinaryDeductionType())
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
    updateField(field: keyof CreateOrEditDisciplinaryDeductionTypeDto, value: any): void {
        const current = this.disciplinaryDeductionType();
        (current as any)[field] = value;
        const next = new CreateOrEditDisciplinaryDeductionTypeDto();
        Object.assign(next, current);
        this.disciplinaryDeductionType.set(next);
    }
}
