import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    ClearanceRequestFieldsServiceProxy,
    CreateOrEditClearanceRequestFieldDto,
    SimpleLookupDto,
    GetAllSimpleLookupsInput,
    ClearanceRequestTypesServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditClearanceRequestFieldModal',
    templateUrl: './create-or-edit-clearance-request-field-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditClearanceRequestFieldModalComponent extends AppComponentBase {
    private _clearanceRequestFieldService = inject(ClearanceRequestFieldsServiceProxy);
    private _clearanceRequestTypesService = inject(ClearanceRequestTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly clearanceRequestField = signal<CreateOrEditClearanceRequestFieldDto>(
        new CreateOrEditClearanceRequestFieldDto()
    );
    readonly clearanceRequestTypes = signal<SimpleLookupDto[]>([]);

    show(clearanceRequestFieldId?: number): void {
        this.active.set(true);

        this._clearanceRequestTypesService
            .getAll(
                new GetAllSimpleLookupsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => {
                this.clearanceRequestTypes.set(result.items);
            });

        if (!clearanceRequestFieldId) {
            this.clearanceRequestField.set(new CreateOrEditClearanceRequestFieldDto());
            this.modal().show();
        } else {
            this._clearanceRequestFieldService
                .getClearanceRequestFieldForEdit(clearanceRequestFieldId)
                .subscribe((result) => {
                    this.clearanceRequestField.set(result.clearanceRequestField);
                    this.modal().show();
                });
        }
    }

    onShown(): void {
        document.getElementById('ClearanceRequestField_NameEn')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._clearanceRequestFieldService
            .createOrEdit(this.clearanceRequestField())
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
    updateField(field: keyof CreateOrEditClearanceRequestFieldDto, value: any): void {
        const current = this.clearanceRequestField();
        (current as any)[field] = value;
        const next = new CreateOrEditClearanceRequestFieldDto();
        Object.assign(next, current);
        this.clearanceRequestField.set(next);
    }
}
