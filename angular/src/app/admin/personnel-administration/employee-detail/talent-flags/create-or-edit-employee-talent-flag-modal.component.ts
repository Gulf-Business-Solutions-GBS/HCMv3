import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditEmployeeTalentFlagDto,
    EmployeeTalentFlagsServiceProxy,
    GetAllSimpleLookupsInput,
    ImpactOfLossesServiceProxy,
    ReasonForLeavingsServiceProxy,
    RiskOfLossesServiceProxy,
    SimpleLookupDto,
    TalentPoolsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditEmployeeTalentFlagModal',
    templateUrl: './create-or-edit-employee-talent-flag-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditEmployeeTalentFlagModalComponent extends AppComponentBase {
    private _service = inject(EmployeeTalentFlagsServiceProxy);
    private _riskOfLossesService = inject(RiskOfLossesServiceProxy);
    private _impactOfLossesService = inject(ImpactOfLossesServiceProxy);
    private _reasonForLeavingsService = inject(ReasonForLeavingsServiceProxy);
    private _talentPoolsService = inject(TalentPoolsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeTalentFlagDto>(new CreateOrEditEmployeeTalentFlagDto());

    readonly riskOfLosses = signal<SimpleLookupDto[]>([]);
    readonly impactOfLosses = signal<SimpleLookupDto[]>([]);
    readonly reasonForLeavings = signal<SimpleLookupDto[]>([]);
    readonly talentPools = signal<SimpleLookupDto[]>([]);

    show(employeeId: number, id?: number): void {
        this.active.set(true);

        this._riskOfLossesService
            .getAll(
                new GetAllSimpleLookupsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.riskOfLosses.set(result.items));

        this._impactOfLossesService
            .getAll(
                new GetAllSimpleLookupsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.impactOfLosses.set(result.items));

        this._reasonForLeavingsService
            .getAll(
                new GetAllSimpleLookupsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.reasonForLeavings.set(result.items));

        this._talentPoolsService
            .getAll(
                new GetAllSimpleLookupsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.talentPools.set(result.items));

        if (!id) {
            const record = new CreateOrEditEmployeeTalentFlagDto();
            record.employeeId = employeeId;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getEmployeeTalentFlagForEdit(id).subscribe((result) => {
                this.record.set(result.employeeTalentFlag);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('EmployeeTalentFlag_RiskOfLossId')?.focus();
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

    updateField(field: keyof CreateOrEditEmployeeTalentFlagDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeTalentFlagDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
