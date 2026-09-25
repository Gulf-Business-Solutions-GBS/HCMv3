import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditPositionRelationshipDto,
    EmployeeDto,
    EmployeesServiceProxy,
    GetAllEmployeesInput,
    GetAllPositionsInput,
    PositionDto,
    PositionRelationshipsServiceProxy,
    PositionRelationType,
    PositionsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';
import { attachEffectiveDatedPrefill } from '@app/shared/common/effective-dated/attach-effective-dated-prefill';
import { DateInputDirective } from '@shared/common/dates/date-input.directive';

@Component({
    selector: 'createOrEditPositionRelationshipModal',
    templateUrl: './create-or-edit-position-relationship-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditPositionRelationshipModalComponent extends AppComponentBase {
    private _service = inject(PositionRelationshipsServiceProxy);
    private _positionsService = inject(PositionsServiceProxy);
    private _employeesService = inject(EmployeesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditPositionRelationshipDto>(new CreateOrEditPositionRelationshipDto());

    /** Adding, not editing: only a new record is filled in from the one in force on the date typed. */
    readonly isNew = signal<boolean>(false);

    constructor() {
        super();

        attachEffectiveDatedPrefill({
            recordType: 'PositionRelationship',
            record: this.record,
            isNew: this.isNew,
            keys: () => ({ PositionId: this.record().positionId, RelationType: this.record().relationType }),
            onPrefilled: (sourceDate) => this.notify.info(this.l('PrefilledFrom', sourceDate.toFormat('dd/MM/yyyy'))),
        });
    }

    readonly positions = signal<PositionDto[]>([]);
    readonly employees = signal<EmployeeDto[]>([]);

    readonly relationTypes: { value: PositionRelationType; label: string }[] = [
        { value: PositionRelationType.Holder, label: 'Holder' },
    ];

    show(id?: number): void {
        this.isNew.set(!id);
        this.active.set(true);

        this._positionsService
            .getAll(new GetAllPositionsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined, vacant: undefined }))
            .subscribe((result) => this.positions.set(result.items));

        this._employeesService
            .getAll(new GetAllEmployeesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.employees.set(result.items));

        if (!id) {
            const record = new CreateOrEditPositionRelationshipDto();
            record.relationType = PositionRelationType.Holder;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getPositionRelationshipForEdit(id).subscribe((result) => {
                this.record.set(result.positionRelationship);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('PositionRelationship_PositionId')?.focus();
    }

    positionLabel(position: PositionDto): string {
        return `${position.code} - ${position.nameEn}`;
    }

    employeeLabel(employee: EmployeeDto): string {
        return `${employee.code} - ${employee.fullNameEn}`;
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

    updateField(field: keyof CreateOrEditPositionRelationshipDto, value: any): void {
        const current = this.record();
        const next = new CreateOrEditPositionRelationshipDto();
        Object.assign(next, current);
        (next as any)[field] = value;
        this.record.set(next);
    }

    updateDateField(field: keyof CreateOrEditPositionRelationshipDto, value: string): void {
        const current = this.record();
        const next = new CreateOrEditPositionRelationshipDto();
        Object.assign(next, current);
        (next as any)[field] = value ? DateTime.fromISO(value) : (undefined as any);
        this.record.set(next);
    }
}
