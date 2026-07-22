import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditPersonalActionDto,
    PersonalActionsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditPersonalActionModal',
    templateUrl: './create-or-edit-personal-action-modal.component.html',
    imports: [
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditPersonalActionModalComponent extends AppComponentBase {
    private _personalActionsService = inject(PersonalActionsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly personalAction = signal<CreateOrEditPersonalActionDto>(new CreateOrEditPersonalActionDto());

    show(personalActionId?: number): void {
        this.active.set(true);

        if (!personalActionId) {
            this.personalAction.set(new CreateOrEditPersonalActionDto());
            this.modal().show();
        } else {
            this._personalActionsService.getPersonalActionForEdit(personalActionId).subscribe((result) => {
                this.personalAction.set(result.personalAction);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('PersonalAction_NameEn')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._personalActionsService
            .createOrEdit(this.personalAction())
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

    // Signal-friendly field updaters (keep template bindings immutable).
    updateField(field: keyof CreateOrEditPersonalActionDto, value: any): void {
        const current = this.personalAction();
        (current as any)[field] = value;
        const next = new CreateOrEditPersonalActionDto();
        Object.assign(next, current);
        this.personalAction.set(next);
    }
}
