import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditRosterGroupDetailDto,
    EmployeeDto,
    EmployeesServiceProxy,
    GetAllEmployeesInput,
    GetAllRosterGroupsInput,
    RosterGroupDetailsServiceProxy,
    RosterGroupDto,
    RosterGroupsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';
import { attachEffectiveDatedPrefill } from '@app/shared/common/effective-dated/attach-effective-dated-prefill';
import { DateInputDirective } from '@shared/common/dates/date-input.directive';

@Component({
    selector: 'createOrEditRosterGroupDetailModal',
    templateUrl: './create-or-edit-roster-group-detail-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditRosterGroupDetailModalComponent extends AppComponentBase {
    private _rosterGroupDetailsService = inject(RosterGroupDetailsServiceProxy);
    private _rosterGroupsService = inject(RosterGroupsServiceProxy);
    private _employeesService = inject(EmployeesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly rosterGroupDetail = signal<CreateOrEditRosterGroupDetailDto>(new CreateOrEditRosterGroupDetailDto());

    /** Adding, not editing: only a new record is filled in from the one in force on the date typed. */
    readonly isNew = signal<boolean>(false);

    constructor() {
        super();

        attachEffectiveDatedPrefill({
            recordType: 'RosterGroupDetail',
            record: this.rosterGroupDetail,
            isNew: this.isNew,
            keys: () => ({
                RosterGroupId: this.rosterGroupDetail().rosterGroupId,
                EmployeeId: this.rosterGroupDetail().employeeId,
            }),
            onPrefilled: (sourceDate) => this.notify.info(this.l('PrefilledFrom', sourceDate.toFormat('dd/MM/yyyy'))),
        });
    }
    readonly rosterGroups = signal<RosterGroupDto[]>([]);
    readonly employees = signal<EmployeeDto[]>([]);

    show(rosterGroupDetailId?: number): void {
        this.isNew.set(!rosterGroupDetailId);
        this.active.set(true);

        this._rosterGroupsService
            .getAll(
                new GetAllRosterGroupsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => {
                this.rosterGroups.set(result.items);
            });

        this._employeesService
            .getAll(
                new GetAllEmployeesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined })
            )
            .subscribe((result) => {
                this.employees.set(result.items);
            });

        if (!rosterGroupDetailId) {
            this.rosterGroupDetail.set(new CreateOrEditRosterGroupDetailDto());
            this.modal().show();
        } else {
            this._rosterGroupDetailsService.getRosterGroupDetailForEdit(rosterGroupDetailId).subscribe((result) => {
                this.rosterGroupDetail.set(result.rosterGroupDetail);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('RosterGroupDetail_RosterGroupId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._rosterGroupDetailsService
            .createOrEdit(this.rosterGroupDetail())
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

    employeeLabel(employee: EmployeeDto): string {
        return `${employee.code} - ${employee.fullNameEn}`;
    }

    updateField(field: keyof CreateOrEditRosterGroupDetailDto, value: any): void {
        const current = this.rosterGroupDetail();
        (current as any)[field] = value;
        const next = new CreateOrEditRosterGroupDetailDto();
        Object.assign(next, current);
        this.rosterGroupDetail.set(next);
    }

    updateDateField(field: keyof CreateOrEditRosterGroupDetailDto, value: string): void {
        const current = this.rosterGroupDetail();
        (current as any)[field] = value ? DateTime.fromISO(value) : (undefined as any);
        const next = new CreateOrEditRosterGroupDetailDto();
        Object.assign(next, current);
        this.rosterGroupDetail.set(next);
    }
}
