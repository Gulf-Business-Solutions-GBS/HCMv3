import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditOnbGroupDetailDto,
    GetAllOnbGroupsInput,
    GetAllOnbTasksInput,
    GetAllSimpleLookupsInput,
    JobTitlesServiceProxy,
    OnbGroupDetailsServiceProxy,
    OnbGroupDto,
    OnbGroupsServiceProxy,
    OnbTaskDto,
    OnbTasksServiceProxy,
    SimpleLookupDto,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditOnbGroupDetailModal',
    templateUrl: './create-or-edit-onb-group-detail-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditOnbGroupDetailModalComponent extends AppComponentBase {
    private _service = inject(OnbGroupDetailsServiceProxy);
    private _onbGroupsService = inject(OnbGroupsServiceProxy);
    private _onbTasksService = inject(OnbTasksServiceProxy);
    private _jobTitlesService = inject(JobTitlesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditOnbGroupDetailDto>(new CreateOrEditOnbGroupDetailDto());

    readonly onbGroups = signal<OnbGroupDto[]>([]);
    readonly onbTasks = signal<OnbTaskDto[]>([]);
    readonly jobTitles = signal<SimpleLookupDto[]>([]);

    show(id?: number): void {
        this.active.set(true);
        this.loadLookups();

        if (!id) {
            this.record.set(new CreateOrEditOnbGroupDetailDto());
            this.modal().show();
        } else {
            this._service.getOnbGroupDetailForEdit(id).subscribe((result) => {
                this.record.set(result.onbGroupDetail);
                this.modal().show();
            });
        }
    }

    private loadLookups(): void {
        this._onbGroupsService
            .getAll(
                new GetAllOnbGroupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined })
            )
            .subscribe((result) => this.onbGroups.set(result.items));

        this._onbTasksService
            .getAll(
                new GetAllOnbTasksInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    onbTemplateId: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.onbTasks.set(result.items));

        this._jobTitlesService
            .getAll(
                new GetAllSimpleLookupsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.jobTitles.set(result.items));
    }

    onShown(): void {
        document.getElementById('OnbGroupDetail_OnbGroupId')?.focus();
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

    updateField(field: keyof CreateOrEditOnbGroupDetailDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditOnbGroupDetailDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
