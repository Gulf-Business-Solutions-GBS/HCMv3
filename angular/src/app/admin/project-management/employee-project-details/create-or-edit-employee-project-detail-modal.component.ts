import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CalculateWorkDaysInput,
    CreateOrEditEmployeeProjectDetailDto,
    EmployeeProjectDetailsServiceProxy,
    EmployeeProjectDto,
    EmployeeProjectsServiceProxy,
    GetAllEmployeeProjectsInput,
    GetAllProjectsInput,
    ProjectDto,
    ProjectsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';
import { attachEffectiveDatedPrefill } from '@app/shared/common/effective-dated/attach-effective-dated-prefill';
import { DateInputDirective } from '@shared/common/dates/date-input.directive';

@Component({
    selector: 'createOrEditEmployeeProjectDetailModal',
    templateUrl: './create-or-edit-employee-project-detail-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditEmployeeProjectDetailModalComponent extends AppComponentBase {
    private _service = inject(EmployeeProjectDetailsServiceProxy);
    private _employeeProjectsService = inject(EmployeeProjectsServiceProxy);
    private _projectsService = inject(ProjectsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly calculating = signal<boolean>(false);
    readonly workDays = signal<number | undefined>(undefined);
    readonly calendarDays = signal<number | undefined>(undefined);
    readonly record = signal<CreateOrEditEmployeeProjectDetailDto>(new CreateOrEditEmployeeProjectDetailDto());

    /** Adding, not editing: only a new record is filled in from the one in force on the date typed. */
    readonly isNew = signal<boolean>(false);

    constructor() {
        super();

        attachEffectiveDatedPrefill({
            recordType: 'EmployeeProjectDetail',
            record: this.record,
            isNew: this.isNew,
            keys: () => ({ EmployeeProjectId: this.record().employeeProjectId, ProjectId: this.record().projectId }),
            onPrefilled: (sourceDate) => this.notify.info(this.l('PrefilledFrom', sourceDate.toFormat('dd/MM/yyyy'))),
        });
    }
    readonly employeeProjects = signal<EmployeeProjectDto[]>([]);
    readonly projects = signal<ProjectDto[]>([]);

    show(id?: number): void {
        this.isNew.set(!id);
        this.active.set(true);
        this.workDays.set(undefined);
        this.calendarDays.set(undefined);

        this._employeeProjectsService
            .getAll(
                new GetAllEmployeeProjectsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    employeeId: undefined,
                })
            )
            .subscribe((result) => this.employeeProjects.set(result.items));

        this._projectsService
            .getAll(
                new GetAllProjectsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined })
            )
            .subscribe((result) => this.projects.set(result.items));

        if (!id) {
            this.record.set(new CreateOrEditEmployeeProjectDetailDto());
            this.modal().show();
        } else {
            this._service.getEmployeeProjectDetailForEdit(id).subscribe((result) => {
                this.record.set(result.employeeProjectDetail);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('EmployeeProjectDetail_EmployeeProjectId')?.focus();
    }

    employeeProjectLabel(employeeProject: EmployeeProjectDto): string {
        return `${employeeProject.employeeCode} (${employeeProject.startDate?.toISODate()} - ${employeeProject.endDate?.toISODate()})`;
    }

    calculateWorkDays(): void {
        const id = this.record().id;
        if (!id) {
            return;
        }

        this.calculating.set(true);
        this._service
            .calculateWorkDays(new CalculateWorkDaysInput({ employeeProjectDetailId: id }))
            .pipe(finalize(() => this.calculating.set(false)))
            .subscribe((result) => {
                this.workDays.set(result.workDays);
                this.calendarDays.set(result.calendarDays);
            });
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

    updateField(field: keyof CreateOrEditEmployeeProjectDetailDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeProjectDetailDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: keyof CreateOrEditEmployeeProjectDetailDto, value: string): void {
        const current = this.record();
        (current as any)[field] = value ? DateTime.fromISO(value) : (undefined as any);
        const next = new CreateOrEditEmployeeProjectDetailDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
