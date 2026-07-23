import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditTimeAttAPIDto,
    TimeAttAPIsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditTimeAttAPIModal',
    templateUrl: './create-or-edit-time-att-api-modal.component.html',
    imports: [
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditTimeAttAPIModalComponent extends AppComponentBase {
    private _timeAttAPIsService = inject(TimeAttAPIsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly timeAttAPI = signal<CreateOrEditTimeAttAPIDto>(new CreateOrEditTimeAttAPIDto());

    show(timeAttAPIId?: number): void {
        this.active.set(true);

        if (!timeAttAPIId) {
            this.timeAttAPI.set(new CreateOrEditTimeAttAPIDto());
            this.modal().show();
        } else {
            this._timeAttAPIsService.getTimeAttAPIForEdit(timeAttAPIId).subscribe((result) => {
                this.timeAttAPI.set(result.timeAttAPI);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('TimeAttAPI_TokenUrl')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._timeAttAPIsService
            .createOrEdit(this.timeAttAPI())
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

    updateField(field: keyof CreateOrEditTimeAttAPIDto, value: any): void {
        const current = this.timeAttAPI();
        (current as any)[field] = value;
        const next = new CreateOrEditTimeAttAPIDto();
        Object.assign(next, current);
        this.timeAttAPI.set(next);
    }
}
