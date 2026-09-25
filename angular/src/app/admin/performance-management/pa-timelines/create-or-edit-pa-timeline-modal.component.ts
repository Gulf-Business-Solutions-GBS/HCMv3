import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    ActivityType,
    CreateOrEditPATimelineDto,
    GetAllPAPeriodsInput,
    PAPeriodDto,
    PAPeriodsServiceProxy,
    PATimelinesServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';
import { DateInputDirective } from '@shared/common/dates/date-input.directive';

@Component({
    selector: 'createOrEditPATimelineModal',
    templateUrl: './create-or-edit-pa-timeline-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditPATimelineModalComponent extends AppComponentBase {
    private _service = inject(PATimelinesServiceProxy);
    private _paPeriodsService = inject(PAPeriodsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly paTimeline = signal<CreateOrEditPATimelineDto>(new CreateOrEditPATimelineDto());
    readonly paPeriods = signal<PAPeriodDto[]>([]);

    readonly activityTypes = [
        { value: ActivityType.Plan, key: 'ActivityTypePlan' },
        { value: ActivityType.MidYearReview, key: 'ActivityTypeMidYearReview' },
        { value: ActivityType.AnnualReview, key: 'ActivityTypeAnnualReview' },
        { value: ActivityType.Quarter1Review, key: 'ActivityTypeQuarter1Review' },
        { value: ActivityType.Quarter3Review, key: 'ActivityTypeQuarter3Review' },
    ];

    show(id?: number): void {
        this.active.set(true);

        this._paPeriodsService
            .getAll(new GetAllPAPeriodsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.paPeriods.set(result.items));

        if (!id) {
            this.paTimeline.set(new CreateOrEditPATimelineDto());
            this.modal().show();
        } else {
            this._service.getPATimelineForEdit(id).subscribe((result) => {
                this.paTimeline.set(result.paTimeline);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('PATimeline_PAPeriodId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._service
            .createOrEdit(this.paTimeline())
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

    updateField(field: keyof CreateOrEditPATimelineDto, value: any): void {
        const current = this.paTimeline();
        (current as any)[field] = value;
        const next = new CreateOrEditPATimelineDto();
        Object.assign(next, current);
        this.paTimeline.set(next);
    }

    updateDateField(field: keyof CreateOrEditPATimelineDto, value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }
}
