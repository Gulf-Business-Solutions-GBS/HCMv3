import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    OvertimeTypesServiceProxy,
    CreateOrEditOvertimeTypeDto,
    GetAllWageTypesInput,
    WageTypeDto,
    WageTypesServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditOvertimeTypeModal',
    templateUrl: './create-or-edit-overtime-type-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditOvertimeTypeModalComponent extends AppComponentBase {
    private _overtimeTypeService = inject(OvertimeTypesServiceProxy);
    private _wageTypesService = inject(WageTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly overtimeType = signal<CreateOrEditOvertimeTypeDto>(new CreateOrEditOvertimeTypeDto());

    /** The wage type the payroll pays this overtime through; without one the payroll skips it. */
    readonly wageTypes = signal<WageTypeDto[]>([]);

    show(overtimeTypeId?: number): void {
        this.active.set(true);

        this._wageTypesService
            .getAll(
                new GetAllWageTypesInput({ maxResultCount: 1000, skipCount: 0, sorting: 'Code ASC', filter: undefined })
            )
            .subscribe((result) => this.wageTypes.set(result.items ?? []));

        if (!overtimeTypeId) {
            this.overtimeType.set(new CreateOrEditOvertimeTypeDto());
            this.modal().show();
        } else {
            this._overtimeTypeService.getOvertimeTypeForEdit(overtimeTypeId).subscribe((result) => {
                this.overtimeType.set(result.overtimeType);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('OvertimeType_NameEn')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._overtimeTypeService
            .createOrEdit(this.overtimeType())
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
    updateField(field: keyof CreateOrEditOvertimeTypeDto, value: any): void {
        const current = this.overtimeType();
        (current as any)[field] = value;
        const next = new CreateOrEditOvertimeTypeDto();
        Object.assign(next, current);
        this.overtimeType.set(next);
    }
}
