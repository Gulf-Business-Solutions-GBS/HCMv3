import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { AbsenceTypesServiceProxy, CreateOrEditAbsenceTypeDto } from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { AbsenceTypeFormComponent } from './absence-type-form.component';

@Component({
    selector: 'createOrEditAbsenceTypeModal',
    templateUrl: './create-or-edit-absence-type-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ButtonBusyDirective, LocalizePipe, AbsenceTypeFormComponent],
})
export class CreateOrEditAbsenceTypeModalComponent extends AppComponentBase {
    private _absenceTypesService = inject(AbsenceTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly absenceType = signal<CreateOrEditAbsenceTypeDto>(new CreateOrEditAbsenceTypeDto());

    show(absenceTypeId?: number): void {
        this.active.set(true);

        if (!absenceTypeId) {
            this.absenceType.set(new CreateOrEditAbsenceTypeDto());
            this.modal().show();
        } else {
            this._absenceTypesService.getAbsenceTypeForEdit(absenceTypeId).subscribe((result) => {
                this.absenceType.set(result.absenceType);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('AbsenceType_NameEn')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._absenceTypesService
            .createOrEdit(this.absenceType())
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
}
