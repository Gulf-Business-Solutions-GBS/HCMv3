import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditYearlyTimeRegisterDetailDto,
    GetAllYearlyTimeRegistersInput,
    GetAllSimpleLookupsInput,
    YearlyTimeRegisterDetailsServiceProxy,
    YearlyTimeRegisterDto,
    YearlyTimeRegistersServiceProxy,
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
    selector: 'createOrEditYearlyTimeRegisterDetailModal',
    templateUrl: './create-or-edit-yearly-time-register-detail-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditYearlyTimeRegisterDetailModalComponent extends AppComponentBase {
    private _yearlyTimeRegisterDetailsService = inject(YearlyTimeRegisterDetailsServiceProxy);
    private _yearlyTimeRegistersService = inject(YearlyTimeRegistersServiceProxy);
    private _timeTypesService = inject(TimeTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly yearlyTimeRegisterDetail = signal<CreateOrEditYearlyTimeRegisterDetailDto>(
        new CreateOrEditYearlyTimeRegisterDetailDto()
    );
    readonly yearlyTimeRegisters = signal<YearlyTimeRegisterDto[]>([]);
    readonly timeTypes = signal<SimpleLookupDto[]>([]);

    show(yearlyTimeRegisterDetailId?: number): void {
        this.active.set(true);

        this._yearlyTimeRegistersService
            .getAll(
                new GetAllYearlyTimeRegistersInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    employeeId: undefined,
                })
            )
            .subscribe((result) => this.yearlyTimeRegisters.set(result.items));

        this._timeTypesService
            .getAll(
                new GetAllSimpleLookupsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.timeTypes.set(result.items));

        if (!yearlyTimeRegisterDetailId) {
            this.yearlyTimeRegisterDetail.set(new CreateOrEditYearlyTimeRegisterDetailDto());
            this.modal().show();
        } else {
            this._yearlyTimeRegisterDetailsService
                .getYearlyTimeRegisterDetailForEdit(yearlyTimeRegisterDetailId)
                .subscribe((result) => {
                    this.yearlyTimeRegisterDetail.set(result.yearlyTimeRegisterDetail);
                    this.modal().show();
                });
        }
    }

    onShown(): void {
        document.getElementById('YearlyTimeRegisterDetail_YearlyTimeRegisterId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._yearlyTimeRegisterDetailsService
            .createOrEdit(this.yearlyTimeRegisterDetail())
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

    yearlyTimeRegisterLabel(record: YearlyTimeRegisterDto): string {
        return `${record.employeeFullNameEn} - ${record.year?.toFormat('yyyy')}`;
    }

    updateField(field: keyof CreateOrEditYearlyTimeRegisterDetailDto, value: any): void {
        const current = this.yearlyTimeRegisterDetail();
        (current as any)[field] = value;
        const next = new CreateOrEditYearlyTimeRegisterDetailDto();
        Object.assign(next, current);
        this.yearlyTimeRegisterDetail.set(next);
    }
}
