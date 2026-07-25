import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditEmpPerformanceHistoryDto,
    EmpPerformanceHistoriesServiceProxy,
    GetAllSimpleLookupsInput,
    NineBoxRatingsServiceProxy,
    ScoreSource,
    SimpleLookupDto,
    SuccessionRatingsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';

@Component({
    selector: 'createOrEditEmployeePerformanceHistoryModal',
    templateUrl: './create-or-edit-employee-performance-history-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditEmployeePerformanceHistoryModalComponent extends AppComponentBase {
    private _service = inject(EmpPerformanceHistoriesServiceProxy);
    private _successionRatingsService = inject(SuccessionRatingsServiceProxy);
    private _nineBoxRatingsService = inject(NineBoxRatingsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmpPerformanceHistoryDto>(new CreateOrEditEmpPerformanceHistoryDto());

    readonly successionRatings = signal<SimpleLookupDto[]>([]);
    readonly nineBoxRatings = signal<SimpleLookupDto[]>([]);

    readonly scoreSources = [
        { value: ScoreSource.Live, name: 'Live' },
        { value: ScoreSource.Manual, name: 'Manual' },
    ];

    show(employeeId: number, id?: number): void {
        this.active.set(true);

        this._successionRatingsService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.successionRatings.set(result.items));

        this._nineBoxRatingsService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.nineBoxRatings.set(result.items));

        if (!id) {
            const record = new CreateOrEditEmpPerformanceHistoryDto();
            record.employeeId = employeeId;
            record.source = ScoreSource.Manual;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getEmpPerformanceHistoryForEdit(id).subscribe((result) => {
                this.record.set(result.empPerformanceHistory);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('EmpPerformanceHistory_StartDate')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._service
            .createOrEdit(this.record())
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

    updateField(field: keyof CreateOrEditEmpPerformanceHistoryDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmpPerformanceHistoryDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: keyof CreateOrEditEmpPerformanceHistoryDto, value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }
}
