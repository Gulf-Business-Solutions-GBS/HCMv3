import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { CreateOrEditRosterGroupDto, RosterGroupsServiceProxy } from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditRosterGroupModal',
    templateUrl: './create-or-edit-roster-group-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditRosterGroupModalComponent extends AppComponentBase {
    private _rosterGroupsService = inject(RosterGroupsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly rosterGroup = signal<CreateOrEditRosterGroupDto>(new CreateOrEditRosterGroupDto());

    show(rosterGroupId?: number): void {
        this.active.set(true);

        if (!rosterGroupId) {
            this.rosterGroup.set(new CreateOrEditRosterGroupDto());
            this.modal().show();
        } else {
            this._rosterGroupsService.getRosterGroupForEdit(rosterGroupId).subscribe((result) => {
                this.rosterGroup.set(result.rosterGroup);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('RosterGroup_NameEn')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._rosterGroupsService
            .createOrEdit(this.rosterGroup())
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

    updateField(field: keyof CreateOrEditRosterGroupDto, value: any): void {
        const current = this.rosterGroup();
        (current as any)[field] = value;
        const next = new CreateOrEditRosterGroupDto();
        Object.assign(next, current);
        this.rosterGroup.set(next);
    }
}
