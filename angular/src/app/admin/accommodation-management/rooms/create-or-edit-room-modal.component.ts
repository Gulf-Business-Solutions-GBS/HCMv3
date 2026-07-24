import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditRoomDto,
    FlatDto,
    FlatsServiceProxy,
    GetAllFlatsInput,
    RoomsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditRoomModal',
    templateUrl: './create-or-edit-room-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditRoomModalComponent extends AppComponentBase {
    private _service = inject(RoomsServiceProxy);
    private _flatsService = inject(FlatsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditRoomDto>(new CreateOrEditRoomDto());
    readonly flats = signal<FlatDto[]>([]);

    show(id?: number): void {
        this.active.set(true);

        this._flatsService
            .getAll(new GetAllFlatsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined, floorId: undefined }))
            .subscribe((result) => this.flats.set(result.items));

        if (!id) {
            this.record.set(new CreateOrEditRoomDto());
            this.modal().show();
        } else {
            this._service.getRoomForEdit(id).subscribe((result) => {
                this.record.set(result.room);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('Room_NameEn')?.focus();
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

    updateField(field: keyof CreateOrEditRoomDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditRoomDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
