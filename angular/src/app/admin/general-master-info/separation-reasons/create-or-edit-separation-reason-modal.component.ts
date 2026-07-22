import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditSeparationReasonDto,
    SeparationReasonsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditSeparationReasonModal',
    templateUrl: './create-or-edit-separation-reason-modal.component.html',
    imports: [
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditSeparationReasonModalComponent extends AppComponentBase {
    private _separationReasonsService = inject(SeparationReasonsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly separationReason = signal<CreateOrEditSeparationReasonDto>(new CreateOrEditSeparationReasonDto());

    show(separationReasonId?: number): void {
        this.active.set(true);

        if (!separationReasonId) {
            this.separationReason.set(new CreateOrEditSeparationReasonDto());
            this.modal().show();
        } else {
            this._separationReasonsService.getSeparationReasonForEdit(separationReasonId).subscribe((result) => {
                this.separationReason.set(result.separationReason);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('SeparationReason_NameEn')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._separationReasonsService
            .createOrEdit(this.separationReason())
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
    updateField(field: keyof CreateOrEditSeparationReasonDto, value: any): void {
        const current = this.separationReason();
        (current as any)[field] = value;
        const next = new CreateOrEditSeparationReasonDto();
        Object.assign(next, current);
        this.separationReason.set(next);
    }
}
