import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditSimulationSalaryRegisterDetailDto,
    GetAllWageTypesInput,
    SimulationSalaryRegisterDetailsServiceProxy,
    WageTypeDto,
    WageTypesServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditSimulationSalaryRegisterDetailModal',
    templateUrl: './create-or-edit-simulation-salary-register-detail-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditSimulationSalaryRegisterDetailModalComponent extends AppComponentBase {
    private _simulationSalaryRegisterDetailsService = inject(SimulationSalaryRegisterDetailsServiceProxy);
    private _wageTypesService = inject(WageTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly simulationSalaryRegisterDetail = signal<CreateOrEditSimulationSalaryRegisterDetailDto>(
        new CreateOrEditSimulationSalaryRegisterDetailDto()
    );

    readonly wageTypes = signal<WageTypeDto[]>([]);

    show(simulationSalaryRegisterDetailId?: number): void {
        this.active.set(true);

        this._wageTypesService
            .getAll(new GetAllWageTypesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.wageTypes.set(result.items));

        if (!simulationSalaryRegisterDetailId) {
            this.simulationSalaryRegisterDetail.set(new CreateOrEditSimulationSalaryRegisterDetailDto());
            this.modal().show();
        } else {
            this._simulationSalaryRegisterDetailsService
                .getSimulationSalaryRegisterDetailForEdit(simulationSalaryRegisterDetailId)
                .subscribe((result) => {
                    this.simulationSalaryRegisterDetail.set(result.simulationSalaryRegisterDetail);
                    this.modal().show();
                });
        }
    }

    onShown(): void {
        document.getElementById('SimulationSalaryRegisterDetail_SimulationSalaryRegisterId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._simulationSalaryRegisterDetailsService
            .createOrEdit(this.simulationSalaryRegisterDetail())
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

    updateField(field: keyof CreateOrEditSimulationSalaryRegisterDetailDto, value: any): void {
        const current = this.simulationSalaryRegisterDetail();
        (current as any)[field] = value;
        const next = new CreateOrEditSimulationSalaryRegisterDetailDto();
        Object.assign(next, current);
        this.simulationSalaryRegisterDetail.set(next);
    }
}
