import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditProjectDto,
    GetAllSimpleLookupsInput,
    ProjectTypesServiceProxy,
    ProjectsServiceProxy,
    SimpleLookupDto,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';

@Component({
    selector: 'createOrEditProjectModal',
    templateUrl: './create-or-edit-project-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditProjectModalComponent extends AppComponentBase {
    private _service = inject(ProjectsServiceProxy);
    private _projectTypesService = inject(ProjectTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditProjectDto>(new CreateOrEditProjectDto());
    readonly projectTypes = signal<SimpleLookupDto[]>([]);

    show(id?: number): void {
        this.active.set(true);

        this._projectTypesService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.projectTypes.set(result.items));

        if (!id) {
            this.record.set(new CreateOrEditProjectDto());
            this.modal().show();
        } else {
            this._service.getProjectForEdit(id).subscribe((result) => {
                this.record.set(result.project);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('Project_NameEn')?.focus();
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

    updateField(field: keyof CreateOrEditProjectDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditProjectDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: keyof CreateOrEditProjectDto, value: string): void {
        const current = this.record();
        (current as any)[field] = value ? DateTime.fromISO(value) : (undefined as any);
        const next = new CreateOrEditProjectDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
