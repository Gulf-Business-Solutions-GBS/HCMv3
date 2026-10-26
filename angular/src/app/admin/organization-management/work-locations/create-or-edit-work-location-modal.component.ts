import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditWorkLocationDto,
    GetAllSponsorsInput,
    SponsorDto,
    SponsorsServiceProxy,
    WorkLocationsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditWorkLocationModal',
    templateUrl: './create-or-edit-work-location-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditWorkLocationModalComponent extends AppComponentBase {
    private _workLocationsService = inject(WorkLocationsServiceProxy);
    private _sponsorsService = inject(SponsorsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly workLocation = signal<CreateOrEditWorkLocationDto>(new CreateOrEditWorkLocationDto());
    readonly sponsors = signal<SponsorDto[]>([]);

    show(workLocationId?: number): void {
        this.active.set(true);

        this._sponsorsService
            .getAll(
                new GetAllSponsorsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined })
            )
            .subscribe((result) => this.sponsors.set(result.items));

        if (!workLocationId) {
            const newWorkLocation = new CreateOrEditWorkLocationDto();
            newWorkLocation.isActive = true;
            this.workLocation.set(newWorkLocation);
            this.modal().show();
        } else {
            this._workLocationsService.getWorkLocationForEdit(workLocationId).subscribe((result) => {
                this.workLocation.set(result.workLocation);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('WorkLocation_NameEn')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._workLocationsService
            .createOrEdit(this.workLocation())
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

    updateField(field: keyof CreateOrEditWorkLocationDto, value: any): void {
        const current = this.workLocation();
        (current as any)[field] = value;
        const next = new CreateOrEditWorkLocationDto();
        Object.assign(next, current);
        this.workLocation.set(next);
    }
}
