import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    DisciplinaryTypeDetailsServiceProxy,
    CreateOrEditDisciplinaryTypeDetailDto,
    DisciplinaryTypeDto,
    DisciplinaryDeductionTypeDto,
    GetAllDisciplinaryTypesInput,
    GetAllDisciplinaryDeductionTypesInput,
    DisciplinaryTypesServiceProxy,
    DisciplinaryDeductionTypesServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditDisciplinaryTypeDetailModal',
    templateUrl: './create-or-edit-disciplinary-type-detail-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditDisciplinaryTypeDetailModalComponent extends AppComponentBase {
    private _disciplinaryTypeDetailService = inject(DisciplinaryTypeDetailsServiceProxy);
    private _disciplinaryTypesService = inject(DisciplinaryTypesServiceProxy);
    private _disciplinaryDeductionTypesService = inject(DisciplinaryDeductionTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly disciplinaryTypeDetail = signal<CreateOrEditDisciplinaryTypeDetailDto>(
        new CreateOrEditDisciplinaryTypeDetailDto()
    );
    readonly disciplinaryTypes = signal<DisciplinaryTypeDto[]>([]);
    readonly disciplinaryDeductionTypes = signal<DisciplinaryDeductionTypeDto[]>([]);

    show(disciplinaryTypeDetailId?: number): void {
        this.active.set(true);

        this._disciplinaryTypesService
            .getAll(
                new GetAllDisciplinaryTypesInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => {
                this.disciplinaryTypes.set(result.items);
            });

        this._disciplinaryDeductionTypesService
            .getAll(
                new GetAllDisciplinaryDeductionTypesInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => {
                this.disciplinaryDeductionTypes.set(result.items);
            });

        if (!disciplinaryTypeDetailId) {
            this.disciplinaryTypeDetail.set(new CreateOrEditDisciplinaryTypeDetailDto());
            this.modal().show();
        } else {
            this._disciplinaryTypeDetailService
                .getDisciplinaryTypeDetailForEdit(disciplinaryTypeDetailId)
                .subscribe((result) => {
                    this.disciplinaryTypeDetail.set(result.disciplinaryTypeDetail);
                    this.modal().show();
                });
        }
    }

    onShown(): void {
        document.getElementById('DisciplinaryTypeDetail_DisciplinaryCount')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._disciplinaryTypeDetailService
            .createOrEdit(this.disciplinaryTypeDetail())
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
    updateField(field: keyof CreateOrEditDisciplinaryTypeDetailDto, value: any): void {
        const current = this.disciplinaryTypeDetail();
        (current as any)[field] = value;
        const next = new CreateOrEditDisciplinaryTypeDetailDto();
        Object.assign(next, current);
        this.disciplinaryTypeDetail.set(next);
    }
}
