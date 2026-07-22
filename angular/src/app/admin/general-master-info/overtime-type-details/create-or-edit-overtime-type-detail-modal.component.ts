import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    OvertimeTypeDetailsServiceProxy,
    CreateOrEditOvertimeTypeDetailDto,
    OvertimeTypeDto,
    GetAllOvertimeTypesInput,
    OvertimeTypesServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';

@Component({
    selector: 'createOrEditOvertimeTypeDetailModal',
    templateUrl: './create-or-edit-overtime-type-detail-modal.component.html',
    imports: [
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditOvertimeTypeDetailModalComponent extends AppComponentBase {
    private _overtimeTypeDetailService = inject(OvertimeTypeDetailsServiceProxy);
    private _overtimeTypesService = inject(OvertimeTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly overtimeTypeDetail = signal<CreateOrEditOvertimeTypeDetailDto>(new CreateOrEditOvertimeTypeDetailDto());
    readonly overtimeTypes = signal<OvertimeTypeDto[]>([]);

    show(overtimeTypeDetailId?: number): void {
        this.active.set(true);

        this._overtimeTypesService
            .getAll(new GetAllOvertimeTypesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => {
                this.overtimeTypes.set(result.items);
            });

        if (!overtimeTypeDetailId) {
            this.overtimeTypeDetail.set(new CreateOrEditOvertimeTypeDetailDto());
            this.modal().show();
        } else {
            this._overtimeTypeDetailService.getOvertimeTypeDetailForEdit(overtimeTypeDetailId).subscribe((result) => {
                this.overtimeTypeDetail.set(result.overtimeTypeDetail);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('OvertimeTypeDetail_Rate')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._overtimeTypeDetailService
            .createOrEdit(this.overtimeTypeDetail())
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
    updateField(field: keyof CreateOrEditOvertimeTypeDetailDto, value: any): void {
        const current = this.overtimeTypeDetail();
        (current as any)[field] = value;
        const next = new CreateOrEditOvertimeTypeDetailDto();
        Object.assign(next, current);
        this.overtimeTypeDetail.set(next);
    }

    updateDateField(field: keyof CreateOrEditOvertimeTypeDetailDto, value: string): void {
        const current = this.overtimeTypeDetail();
        (current as any)[field] = value ? DateTime.fromISO(value) : (undefined as any);
        const next = new CreateOrEditOvertimeTypeDetailDto();
        Object.assign(next, current);
        this.overtimeTypeDetail.set(next);
    }
}
