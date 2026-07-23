import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditMonthlyTimeRegisterDetailDto,
    GetAllMonthlyTimeRegistersInput,
    GetAllSimpleLookupsInput,
    MonthlyTimeRegisterDetailsServiceProxy,
    MonthlyTimeRegisterDto,
    MonthlyTimeRegistersServiceProxy,
    SimpleLookupDto,
    TimeTypesServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditMonthlyTimeRegisterDetailModal',
    templateUrl: './create-or-edit-monthly-time-register-detail-modal.component.html',
    imports: [
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditMonthlyTimeRegisterDetailModalComponent extends AppComponentBase {
    private _monthlyTimeRegisterDetailsService = inject(MonthlyTimeRegisterDetailsServiceProxy);
    private _monthlyTimeRegistersService = inject(MonthlyTimeRegistersServiceProxy);
    private _timeTypesService = inject(TimeTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly monthlyTimeRegisterDetail = signal<CreateOrEditMonthlyTimeRegisterDetailDto>(new CreateOrEditMonthlyTimeRegisterDetailDto());
    readonly monthlyTimeRegisters = signal<MonthlyTimeRegisterDto[]>([]);
    readonly timeTypes = signal<SimpleLookupDto[]>([]);

    show(monthlyTimeRegisterDetailId?: number): void {
        this.active.set(true);

        this._monthlyTimeRegistersService
            .getAll(new GetAllMonthlyTimeRegistersInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, employeeId: undefined }))
            .subscribe((result) => this.monthlyTimeRegisters.set(result.items));

        this._timeTypesService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.timeTypes.set(result.items));

        if (!monthlyTimeRegisterDetailId) {
            this.monthlyTimeRegisterDetail.set(new CreateOrEditMonthlyTimeRegisterDetailDto());
            this.modal().show();
        } else {
            this._monthlyTimeRegisterDetailsService.getMonthlyTimeRegisterDetailForEdit(monthlyTimeRegisterDetailId).subscribe((result) => {
                this.monthlyTimeRegisterDetail.set(result.monthlyTimeRegisterDetail);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('MonthlyTimeRegisterDetail_MonthlyTimeRegisterId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._monthlyTimeRegisterDetailsService
            .createOrEdit(this.monthlyTimeRegisterDetail())
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

    monthlyTimeRegisterLabel(record: MonthlyTimeRegisterDto): string {
        return `${record.employeeFullNameEn} - ${record.month?.toFormat('yyyy-MM')}`;
    }

    updateField(field: keyof CreateOrEditMonthlyTimeRegisterDetailDto, value: any): void {
        const current = this.monthlyTimeRegisterDetail();
        (current as any)[field] = value;
        const next = new CreateOrEditMonthlyTimeRegisterDetailDto();
        Object.assign(next, current);
        this.monthlyTimeRegisterDetail.set(next);
    }
}
