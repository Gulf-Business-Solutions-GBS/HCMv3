import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    AbsenceTypeDto,
    AbsenceTypesServiceProxy,
    CreateOrEditWageTypeLeaveDto,
    GetAllAbsenceTypesInput,
    GetAllWageTypesInput,
    WageTypeDto,
    WageTypeLeavesServiceProxy,
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
    selector: 'createOrEditWageTypeLeaveModal',
    templateUrl: './create-or-edit-wage-type-leave-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditWageTypeLeaveModalComponent extends AppComponentBase {
    private _wageTypeLeavesService = inject(WageTypeLeavesServiceProxy);
    private _wageTypesService = inject(WageTypesServiceProxy);
    private _absenceTypesService = inject(AbsenceTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly wageTypeLeave = signal<CreateOrEditWageTypeLeaveDto>(new CreateOrEditWageTypeLeaveDto());

    readonly wageTypes = signal<WageTypeDto[]>([]);
    readonly absenceTypes = signal<AbsenceTypeDto[]>([]);

    /** Set when opened from a wage type's detail lines on the Wage Types screen: the wage type is fixed. */
    readonly presetWageTypeId = signal<number | undefined>(undefined);

    show(wageTypeLeaveId?: number, wageTypeId?: number): void {
        this.presetWageTypeId.set(wageTypeId);
        this.active.set(true);

        this._wageTypesService
            .getAll(new GetAllWageTypesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.wageTypes.set(result.items));

        this._absenceTypesService
            .getAll(new GetAllAbsenceTypesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.absenceTypes.set(result.items));

        if (!wageTypeLeaveId) {
            const dto = new CreateOrEditWageTypeLeaveDto();
            if (wageTypeId) {
                dto.wageTypeId = wageTypeId;
            }
            this.wageTypeLeave.set(dto);
            this.modal().show();
        } else {
            this._wageTypeLeavesService.getWageTypeLeaveForEdit(wageTypeLeaveId).subscribe((result) => {
                this.wageTypeLeave.set(result.wageTypeLeave);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        const firstFieldId = this.presetWageTypeId() ? 'WageTypeLeave_AbsenceTypeId' : 'WageTypeLeave_WageTypeId';
        document.getElementById(firstFieldId)?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._wageTypeLeavesService
            .createOrEdit(this.wageTypeLeave())
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

    updateField(field: keyof CreateOrEditWageTypeLeaveDto, value: any): void {
        const current = this.wageTypeLeave();
        (current as any)[field] = value;
        const next = new CreateOrEditWageTypeLeaveDto();
        Object.assign(next, current);
        this.wageTypeLeave.set(next);
    }
}
