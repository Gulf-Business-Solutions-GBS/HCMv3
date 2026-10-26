import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { CreateOrEditTimePolicyDto, TimePoliciesServiceProxy } from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditTimePolicyModal',
    templateUrl: './create-or-edit-time-policy-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditTimePolicyModalComponent extends AppComponentBase {
    private _timePoliciesService = inject(TimePoliciesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly timePolicy = signal<CreateOrEditTimePolicyDto>(new CreateOrEditTimePolicyDto());

    show(timePolicyId?: number): void {
        this.active.set(true);

        if (!timePolicyId) {
            this.timePolicy.set(new CreateOrEditTimePolicyDto());
            this.modal().show();
        } else {
            this._timePoliciesService.getTimePolicyForEdit(timePolicyId).subscribe((result) => {
                this.timePolicy.set(result.timePolicy);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('TimePolicy_NameEn')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._timePoliciesService
            .createOrEdit(this.timePolicy())
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

    updateField(field: keyof CreateOrEditTimePolicyDto, value: any): void {
        const current = this.timePolicy();
        (current as any)[field] = value;
        const next = new CreateOrEditTimePolicyDto();
        Object.assign(next, current);
        this.timePolicy.set(next);
    }
}
