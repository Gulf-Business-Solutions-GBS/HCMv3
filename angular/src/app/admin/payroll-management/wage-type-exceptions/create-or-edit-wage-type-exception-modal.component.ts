import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditWageTypeExceptionDto,
    GetAllWageTypesInput,
    WageTypeDto,
    WageTypeExceptionsServiceProxy,
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
    selector: 'createOrEditWageTypeExceptionModal',
    templateUrl: './create-or-edit-wage-type-exception-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditWageTypeExceptionModalComponent extends AppComponentBase {
    private _wageTypeExceptionsService = inject(WageTypeExceptionsServiceProxy);
    private _wageTypesService = inject(WageTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly wageTypeException = signal<CreateOrEditWageTypeExceptionDto>(new CreateOrEditWageTypeExceptionDto());
    readonly wageTypes = signal<WageTypeDto[]>([]);

    /** Set when opened from a wage type's detail page: the wage type is fixed. */
    readonly presetWageTypeId = signal<number | undefined>(undefined);

    show(wageTypeExceptionId?: number, wageTypeId?: number): void {
        this.presetWageTypeId.set(wageTypeId);
        this.active.set(true);

        this._wageTypesService
            .getAll(
                new GetAllWageTypesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined })
            )
            .subscribe((result) => this.wageTypes.set(result.items));

        if (!wageTypeExceptionId) {
            const dto = new CreateOrEditWageTypeExceptionDto();
            if (wageTypeId) {
                dto.wageTypeId = wageTypeId;
            }
            this.wageTypeException.set(dto);
            this.modal().show();
        } else {
            this._wageTypeExceptionsService.getWageTypeExceptionForEdit(wageTypeExceptionId).subscribe((result) => {
                this.wageTypeException.set(result.wageTypeException);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        const firstFieldId = this.presetWageTypeId()
            ? 'WageTypeException_ExceptWageTypeId'
            : 'WageTypeException_WageTypeId';
        document.getElementById(firstFieldId)?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._wageTypeExceptionsService
            .createOrEdit(this.wageTypeException())
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

    updateField(field: keyof CreateOrEditWageTypeExceptionDto, value: any): void {
        const current = this.wageTypeException();
        (current as any)[field] = value;
        const next = new CreateOrEditWageTypeExceptionDto();
        Object.assign(next, current);
        this.wageTypeException.set(next);
    }
}
