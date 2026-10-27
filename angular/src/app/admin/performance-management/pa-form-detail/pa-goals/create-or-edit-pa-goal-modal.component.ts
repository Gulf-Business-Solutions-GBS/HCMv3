import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { CreateOrEditPAGoalDto, PAGoalsServiceProxy } from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditPAGoalModal',
    templateUrl: './create-or-edit-pa-goal-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditPAGoalModalComponent extends AppComponentBase {
    private _service = inject(PAGoalsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditPAGoalDto>(new CreateOrEditPAGoalDto());

    show(paFormId: number, id?: number): void {
        this.active.set(true);

        if (!id) {
            const record = new CreateOrEditPAGoalDto();
            record.paFormId = paFormId;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getPAGoalForEdit(id).subscribe((result) => {
                this.record.set(result.paGoal);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('PAGoal_Goal')?.focus();
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

    updateField(field: keyof CreateOrEditPAGoalDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditPAGoalDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
