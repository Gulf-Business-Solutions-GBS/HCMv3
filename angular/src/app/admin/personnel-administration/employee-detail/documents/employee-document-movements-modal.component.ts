import { Component, inject, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditEmployeeDocumentMvmtDto,
    EmployeeDocumentMvmtDto,
    EmployeeDocumentMvmtsServiceProxy,
    GetAllEmployeeDocumentMvmtsInput,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { DateTime } from 'luxon';

@Component({
    selector: 'employee-document-movements-modal',
    templateUrl: './employee-document-movements-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ButtonBusyDirective, LocalizePipe, LuxonFormatPipe, PermissionPipe],
})
export class EmployeeDocumentMovementsModalComponent extends AppComponentBase {
    private _service = inject(EmployeeDocumentMvmtsServiceProxy);

    readonly modal = viewChild<ModalDirective>('movementsModal');

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly loading = signal<boolean>(false);
    readonly employeeDocumentId = signal<number | undefined>(undefined);
    readonly movements = signal<EmployeeDocumentMvmtDto[]>([]);
    readonly newRecord = signal<CreateOrEditEmployeeDocumentMvmtDto>(new CreateOrEditEmployeeDocumentMvmtDto());

    show(employeeDocumentId: number): void {
        this.active.set(true);
        this.employeeDocumentId.set(employeeDocumentId);
        this.resetNewRecord();
        this.loadMovements();
        this.modal().show();
    }

    private loadMovements(): void {
        if (!this.employeeDocumentId()) {
            return;
        }

        this.loading.set(true);
        this._service
            .getAll(
                new GetAllEmployeeDocumentMvmtsInput({
                    employeeDocumentId: this.employeeDocumentId(),
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                })
            )
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe((result) => this.movements.set(result.items));
    }

    private resetNewRecord(): void {
        const record = new CreateOrEditEmployeeDocumentMvmtDto();
        record.employeeDocumentId = this.employeeDocumentId();
        this.newRecord.set(record);
    }

    updateField(field: keyof CreateOrEditEmployeeDocumentMvmtDto, value: any): void {
        const current = this.newRecord();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeDocumentMvmtDto();
        Object.assign(next, current);
        this.newRecord.set(next);
    }

    updateDateField(field: 'withdrawDate' | 'returnDate' | 'actualReturnDate', value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }

    addMovement(): void {
        this.saving.set(true);
        this._service
            .createOrEdit(this.newRecord())
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe(() => {
                this.notify.info(this.l('SavedSuccessfully'));
                this.resetNewRecord();
                this.loadMovements();
            });
    }

    deleteMovement(record: EmployeeDocumentMvmtDto): void {
        this.message.confirm(this.l('AreYouSure'), this.l('AreYouSure'), (isConfirmed) => {
            if (isConfirmed) {
                this._service.delete(record.id).subscribe(() => {
                    this.loadMovements();
                    this.notify.success(this.l('SuccessfullyDeleted'));
                });
            }
        });
    }

    close(): void {
        this.active.set(false);
        this.modal().hide();
    }
}
