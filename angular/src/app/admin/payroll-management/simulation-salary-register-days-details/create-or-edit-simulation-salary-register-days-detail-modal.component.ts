import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditSimulationSalaryRegisterDaysDetailDto,
    SimulationSalaryRegisterDaysDetailsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';
import { DateInputDirective } from '@shared/common/dates/date-input.directive';

@Component({
    selector: 'createOrEditSimulationSalaryRegisterDaysDetailModal',
    templateUrl: './create-or-edit-simulation-salary-register-days-detail-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditSimulationSalaryRegisterDaysDetailModalComponent extends AppComponentBase {
    private _simulationSalaryRegisterDaysDetailsService = inject(SimulationSalaryRegisterDaysDetailsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly simulationSalaryRegisterDaysDetail = signal<CreateOrEditSimulationSalaryRegisterDaysDetailDto>(
        new CreateOrEditSimulationSalaryRegisterDaysDetailDto()
    );

    show(simulationSalaryRegisterDaysDetailId?: number): void {
        this.active.set(true);

        if (!simulationSalaryRegisterDaysDetailId) {
            this.simulationSalaryRegisterDaysDetail.set(new CreateOrEditSimulationSalaryRegisterDaysDetailDto());
            this.modal().show();
        } else {
            this._simulationSalaryRegisterDaysDetailsService
                .getSimulationSalaryRegisterDaysDetailForEdit(simulationSalaryRegisterDaysDetailId)
                .subscribe((result) => {
                    this.simulationSalaryRegisterDaysDetail.set(result.simulationSalaryRegisterDaysDetail);
                    this.modal().show();
                });
        }
    }

    onShown(): void {
        document.getElementById('SimulationSalaryRegisterDaysDetail_SimulationSalaryRegisterDetailId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._simulationSalaryRegisterDaysDetailsService
            .createOrEdit(this.simulationSalaryRegisterDaysDetail())
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

    updateField(field: keyof CreateOrEditSimulationSalaryRegisterDaysDetailDto, value: any): void {
        const current = this.simulationSalaryRegisterDaysDetail();
        (current as any)[field] = value;
        const next = new CreateOrEditSimulationSalaryRegisterDaysDetailDto();
        Object.assign(next, current);
        this.simulationSalaryRegisterDaysDetail.set(next);
    }

    updatePeriod(value: string): void {
        this.updateField('period', value ? (DateTime.fromISO(value) as any) : (undefined as any));
    }
}
