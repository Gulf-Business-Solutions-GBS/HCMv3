import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditOrgUnitWageAccGroupDto,
    GetAllSimpleLookupsInput,
    OrganizationUnitDto,
    OrganizationUnitServiceProxy,
    OrgUnitWageAccGroupsServiceProxy,
    SimpleLookupDto,
    WageAccGroupsServiceProxy,
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
    selector: 'createOrEditOrgUnitWageAccGroupModal',
    templateUrl: './create-or-edit-org-unit-wage-acc-group-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditOrgUnitWageAccGroupModalComponent extends AppComponentBase {
    private _orgUnitWageAccGroupsService = inject(OrgUnitWageAccGroupsServiceProxy);
    private _organizationUnitService = inject(OrganizationUnitServiceProxy);
    private _wageAccGroupsService = inject(WageAccGroupsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly orgUnitWageAccGroup = signal<CreateOrEditOrgUnitWageAccGroupDto>(new CreateOrEditOrgUnitWageAccGroupDto());

    /** Adding, not editing: only a new record is filled in from the one in force on the date typed. */
    readonly isNew = signal<boolean>(false);

    constructor() {
        super();

        attachEffectiveDatedPrefill({
            recordType: 'OrgUnitWageAccGroup',
            record: this.orgUnitWageAccGroup,
            isNew: this.isNew,
            keys: () => ({ OrganizationUnitId: this.orgUnitWageAccGroup().organizationUnitId }),
            onPrefilled: (sourceDate) => this.notify.info(this.l('PrefilledFrom', sourceDate.toFormat('dd/MM/yyyy'))),
        });
    }

    readonly organizationUnits = signal<OrganizationUnitDto[]>([]);
    readonly wageAccGroups = signal<SimpleLookupDto[]>([]);

    show(orgUnitWageAccGroupId?: number): void {
        this.isNew.set(!orgUnitWageAccGroupId);
        this.active.set(true);

        this._organizationUnitService
            .getOrganizationUnits()
            .subscribe((result) => this.organizationUnits.set(result.items ?? []));

        this._wageAccGroupsService
            .getAll(
                new GetAllSimpleLookupsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.wageAccGroups.set(result.items));

        if (!orgUnitWageAccGroupId) {
            this.orgUnitWageAccGroup.set(new CreateOrEditOrgUnitWageAccGroupDto());
            this.modal().show();
        } else {
            this._orgUnitWageAccGroupsService
                .getOrgUnitWageAccGroupForEdit(orgUnitWageAccGroupId)
                .subscribe((result) => {
                    this.orgUnitWageAccGroup.set(result.orgUnitWageAccGroup);
                    this.modal().show();
                });
        }
    }

    onShown(): void {
        document.getElementById('OrgUnitWageAccGroup_OrganizationUnitId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._orgUnitWageAccGroupsService
            .createOrEdit(this.orgUnitWageAccGroup())
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

    updateField(field: keyof CreateOrEditOrgUnitWageAccGroupDto, value: any): void {
        const current = this.orgUnitWageAccGroup();
        (current as any)[field] = value;
        const next = new CreateOrEditOrgUnitWageAccGroupDto();
        Object.assign(next, current);
        this.orgUnitWageAccGroup.set(next);
    }

    updateStartDate(value: string): void {
        this.updateField('startDate', value ? DateTime.fromISO(value) : (undefined as any));
    }
}
