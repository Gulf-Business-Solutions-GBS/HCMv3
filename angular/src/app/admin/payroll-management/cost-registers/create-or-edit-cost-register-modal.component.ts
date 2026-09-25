import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { CostRegistersServiceProxy, CreateOrEditCostRegisterDto, PayrollRunType } from '@shared/service-proxies/service-proxies';
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
    selector: 'createOrEditCostRegisterModal',
    templateUrl: './create-or-edit-cost-register-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditCostRegisterModalComponent extends AppComponentBase {
    private _costRegistersService = inject(CostRegistersServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly costRegister = signal<CreateOrEditCostRegisterDto>(new CreateOrEditCostRegisterDto());

    readonly payrollRunTypes = [
        { value: PayrollRunType.Regular, name: 'Regular' },
        { value: PayrollRunType.OffCycle, name: 'OffCycle' },
    ];

    show(costRegisterId?: number): void {
        this.active.set(true);

        if (!costRegisterId) {
            this.costRegister.set(new CreateOrEditCostRegisterDto());
            this.modal().show();
        } else {
            this._costRegistersService.getCostRegisterForEdit(costRegisterId).subscribe((result) => {
                this.costRegister.set(result.costRegister);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('CostRegister_EmployeeId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._costRegistersService
            .createOrEdit(this.costRegister())
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

    updateField(field: keyof CreateOrEditCostRegisterDto, value: any): void {
        const current = this.costRegister();
        (current as any)[field] = value;
        const next = new CreateOrEditCostRegisterDto();
        Object.assign(next, current);
        this.costRegister.set(next);
    }

    updatePeriod(value: string): void {
        this.updateField('period', value ? (DateTime.fromISO(value) as any) : (undefined as any));
    }
}
