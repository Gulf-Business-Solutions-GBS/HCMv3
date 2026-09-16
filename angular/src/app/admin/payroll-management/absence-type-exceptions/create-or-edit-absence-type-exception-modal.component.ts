import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    AbsenceTypeDto,
    AbsenceTypeExceptionsServiceProxy,
    AbsenceTypesServiceProxy,
    CreateOrEditAbsenceTypeExceptionDto,
    GetAllAbsenceTypesInput,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditAbsenceTypeExceptionModal',
    templateUrl: './create-or-edit-absence-type-exception-modal.component.html',
    imports: [
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditAbsenceTypeExceptionModalComponent extends AppComponentBase {
    private _absenceTypeExceptionsService = inject(AbsenceTypeExceptionsServiceProxy);
    private _absenceTypesService = inject(AbsenceTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly absenceTypeException = signal<CreateOrEditAbsenceTypeExceptionDto>(new CreateOrEditAbsenceTypeExceptionDto());
    readonly absenceTypes = signal<AbsenceTypeDto[]>([]);

    /** Set when opened from an absence type's detail page: the absence type is fixed. */
    readonly presetAbsenceTypeId = signal<number | undefined>(undefined);

    show(absenceTypeExceptionId?: number, absenceTypeId?: number): void {
        this.presetAbsenceTypeId.set(absenceTypeId);
        this.active.set(true);

        this._absenceTypesService
            .getAll(new GetAllAbsenceTypesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.absenceTypes.set(result.items));

        if (!absenceTypeExceptionId) {
            const dto = new CreateOrEditAbsenceTypeExceptionDto();
            if (absenceTypeId) {
                dto.absenceTypeId = absenceTypeId;
            }
            this.absenceTypeException.set(dto);
            this.modal().show();
        } else {
            this._absenceTypeExceptionsService.getAbsenceTypeExceptionForEdit(absenceTypeExceptionId).subscribe((result) => {
                this.absenceTypeException.set(result.absenceTypeException);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        const firstFieldId = this.presetAbsenceTypeId()
            ? 'AbsenceTypeException_ExceptAbsenceTypeId'
            : 'AbsenceTypeException_AbsenceTypeId';
        document.getElementById(firstFieldId)?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._absenceTypeExceptionsService
            .createOrEdit(this.absenceTypeException())
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

    updateField(field: keyof CreateOrEditAbsenceTypeExceptionDto, value: any): void {
        const current = this.absenceTypeException();
        (current as any)[field] = value;
        const next = new CreateOrEditAbsenceTypeExceptionDto();
        Object.assign(next, current);
        this.absenceTypeException.set(next);
    }
}
