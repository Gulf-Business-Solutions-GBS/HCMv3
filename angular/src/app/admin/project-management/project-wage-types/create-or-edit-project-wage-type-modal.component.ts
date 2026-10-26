import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditProjectWageTypeDto,
    GetAllProjectsInput,
    GetAllWageTypesInput,
    ProjectDto,
    ProjectWageTypesServiceProxy,
    ProjectsServiceProxy,
    WageTypeDto,
    WageTypesServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditProjectWageTypeModal',
    templateUrl: './create-or-edit-project-wage-type-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditProjectWageTypeModalComponent extends AppComponentBase {
    private _service = inject(ProjectWageTypesServiceProxy);
    private _projectsService = inject(ProjectsServiceProxy);
    private _wageTypesService = inject(WageTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditProjectWageTypeDto>(new CreateOrEditProjectWageTypeDto());
    readonly projects = signal<ProjectDto[]>([]);
    readonly wageTypes = signal<WageTypeDto[]>([]);

    show(id?: number): void {
        this.active.set(true);

        this._projectsService
            .getAll(
                new GetAllProjectsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined })
            )
            .subscribe((result) => this.projects.set(result.items));

        this._wageTypesService
            .getAll(
                new GetAllWageTypesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined })
            )
            .subscribe((result) => this.wageTypes.set(result.items));

        if (!id) {
            this.record.set(new CreateOrEditProjectWageTypeDto());
            this.modal().show();
        } else {
            this._service.getProjectWageTypeForEdit(id).subscribe((result) => {
                this.record.set(result.projectWageType);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('ProjectWageType_ProjectId')?.focus();
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

    updateField(field: keyof CreateOrEditProjectWageTypeDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditProjectWageTypeDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
