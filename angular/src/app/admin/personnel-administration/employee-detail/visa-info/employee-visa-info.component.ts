import { Component, effect, inject, input, signal } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditEmployeeVisaInfoDto,
    EmployeeVisaInfosServiceProxy,
    GetAllEmployeeVisaInfosInput,
    GetAllSimpleLookupsInput,
    PassportTypesServiceProxy,
    SimpleLookupDto,
} from '@shared/service-proxies/service-proxies';
import { finalize } from 'rxjs/operators';
import { FormsModule } from '@angular/forms';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { DateTime } from 'luxon';
import { DateInputDirective } from '@shared/common/dates/date-input.directive';

@Component({
    selector: 'employee-visa-info',
    templateUrl: './employee-visa-info.component.html',
    imports: [DateInputDirective, FormsModule, ButtonBusyDirective, LocalizePipe, PermissionPipe, BusyIfDirective],
})
export class EmployeeVisaInfoComponent extends AppComponentBase {
    private _service = inject(EmployeeVisaInfosServiceProxy);
    private _passportTypesService = inject(PassportTypesServiceProxy);

    readonly employeeId = input<number>();

    readonly loading = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeVisaInfoDto>(new CreateOrEditEmployeeVisaInfoDto());
    readonly passportTypes = signal<SimpleLookupDto[]>([]);

    constructor() {
        super();
        effect(() => {
            if (this.employeeId()) {
                this.load();
            }
        });

        this._passportTypesService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.passportTypes.set(result.items));
    }

    load(): void {
        this.loading.set(true);
        this._service
            .getAll(
                new GetAllEmployeeVisaInfosInput({
                    employeeId: this.employeeId(),
                    maxResultCount: 1,
                    skipCount: 0,
                    sorting: undefined,
                })
            )
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe((result) => {
                if (result.items.length > 0) {
                    this._service.getEmployeeVisaInfoForEdit(result.items[0].id).subscribe((editResult) => {
                        this.record.set(editResult.employeeVisaInfo);
                    });
                } else {
                    const record = new CreateOrEditEmployeeVisaInfoDto();
                    record.employeeId = this.employeeId();
                    this.record.set(record);
                }
            });
    }

    updateField(field: keyof CreateOrEditEmployeeVisaInfoDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeVisaInfoDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(
        field: 'passportIssueDate' | 'passportExpiryDate' | 'entryDate' | 'terminationDate' | 'startDate' | 'endDate',
        value: string
    ): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }

    save(): void {
        this.saving.set(true);
        this._service
            .createOrEdit(this.record())
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe(() => {
                this.notify.info(this.l('SavedSuccessfully'));
                this.load();
            });
    }
}
