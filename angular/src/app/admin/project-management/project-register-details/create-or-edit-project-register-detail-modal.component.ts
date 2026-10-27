import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditProjectRegisterDetailDto,
    GetAllProjectRegistersInput,
    GetAllProjectsInput,
    GetAllWageTypesInput,
    ProjectDto,
    ProjectRegisterDetailsServiceProxy,
    ProjectRegisterDto,
    ProjectRegistersServiceProxy,
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
    selector: 'createOrEditProjectRegisterDetailModal',
    templateUrl: './create-or-edit-project-register-detail-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditProjectRegisterDetailModalComponent extends AppComponentBase {
    private _service = inject(ProjectRegisterDetailsServiceProxy);
    private _projectRegistersService = inject(ProjectRegistersServiceProxy);
    private _projectsService = inject(ProjectsServiceProxy);
    private _wageTypesService = inject(WageTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditProjectRegisterDetailDto>(new CreateOrEditProjectRegisterDetailDto());
    readonly projectRegisters = signal<ProjectRegisterDto[]>([]);
    readonly projects = signal<ProjectDto[]>([]);
    readonly wageTypes = signal<WageTypeDto[]>([]);

    show(id?: number): void {
        this.active.set(true);

        this._projectRegistersService
            .getAll(
                new GetAllProjectRegistersInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    employeeId: undefined,
                })
            )
            .subscribe((result) => this.projectRegisters.set(result.items));

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
            this.record.set(new CreateOrEditProjectRegisterDetailDto());
            this.modal().show();
        } else {
            this._service.getProjectRegisterDetailForEdit(id).subscribe((result) => {
                this.record.set(result.projectRegisterDetail);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('ProjectRegisterDetail_ProjectRegisterId')?.focus();
    }

    projectRegisterLabel(item: ProjectRegisterDto): string {
        return `${item.employeeCode} (${item.period?.toISODate()})`;
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

    updateField(field: keyof CreateOrEditProjectRegisterDetailDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditProjectRegisterDetailDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
