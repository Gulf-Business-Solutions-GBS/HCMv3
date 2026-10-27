import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditSimulationTimeRegisterDetailDto,
    GetAllSimulationTimeRegistersInput,
    GetAllSimpleLookupsInput,
    SimulationTimeRegisterDetailsServiceProxy,
    SimulationTimeRegisterDto,
    SimulationTimeRegistersServiceProxy,
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
    selector: 'createOrEditSimulationTimeRegisterDetailModal',
    templateUrl: './create-or-edit-simulation-time-register-detail-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditSimulationTimeRegisterDetailModalComponent extends AppComponentBase {
    private _simulationTimeRegisterDetailsService = inject(SimulationTimeRegisterDetailsServiceProxy);
    private _simulationTimeRegistersService = inject(SimulationTimeRegistersServiceProxy);
    private _timeTypesService = inject(TimeTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly simulationTimeRegisterDetail = signal<CreateOrEditSimulationTimeRegisterDetailDto>(
        new CreateOrEditSimulationTimeRegisterDetailDto()
    );
    readonly simulationTimeRegisters = signal<SimulationTimeRegisterDto[]>([]);
    readonly timeTypes = signal<SimpleLookupDto[]>([]);

    show(simulationTimeRegisterDetailId?: number): void {
        this.active.set(true);

        this._simulationTimeRegistersService
            .getAll(
                new GetAllSimulationTimeRegistersInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    employeeId: undefined,
                })
            )
            .subscribe((result) => this.simulationTimeRegisters.set(result.items));

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

        if (!simulationTimeRegisterDetailId) {
            this.simulationTimeRegisterDetail.set(new CreateOrEditSimulationTimeRegisterDetailDto());
            this.modal().show();
        } else {
            this._simulationTimeRegisterDetailsService
                .getSimulationTimeRegisterDetailForEdit(simulationTimeRegisterDetailId)
                .subscribe((result) => {
                    this.simulationTimeRegisterDetail.set(result.simulationTimeRegisterDetail);
                    this.modal().show();
                });
        }
    }

    onShown(): void {
        document.getElementById('SimulationTimeRegisterDetail_SimulationTimeRegisterId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._simulationTimeRegisterDetailsService
            .createOrEdit(this.simulationTimeRegisterDetail())
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

    simulationTimeRegisterLabel(record: SimulationTimeRegisterDto): string {
        return `${record.employeeFullNameEn} - ${record.dayDate?.toISODate()}`;
    }

    updateField(field: keyof CreateOrEditSimulationTimeRegisterDetailDto, value: any): void {
        const current = this.simulationTimeRegisterDetail();
        (current as any)[field] = value;
        const next = new CreateOrEditSimulationTimeRegisterDetailDto();
        Object.assign(next, current);
        this.simulationTimeRegisterDetail.set(next);
    }
}
