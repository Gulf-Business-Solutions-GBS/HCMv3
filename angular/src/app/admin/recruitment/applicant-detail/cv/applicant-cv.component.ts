import { Component, effect, inject, input, signal } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    ApplicantCVsServiceProxy,
    CreateOrEditApplicantCVDto,
    GetAllApplicantCVsInput,
    GetAllSimpleLookupsInput,
    JobTitlesServiceProxy,
    SimpleLookupDto,
} from '@shared/service-proxies/service-proxies';
import { finalize } from 'rxjs/operators';
import { FormsModule } from '@angular/forms';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';
import { TabsetComponent, TabDirective } from 'ngx-bootstrap/tabs';
import { CvQualificationsComponent } from './qualifications/cv-qualifications.component';
import { CvWorkExpsComponent } from './work-exps/cv-work-exps.component';
import { CvSkillsComponent } from './skills/cv-skills.component';
import { CvLanguagesComponent } from './languages/cv-languages.component';
import { DateInputDirective } from '@shared/common/dates/date-input.directive';

@Component({
    selector: 'applicant-cv',
    templateUrl: './applicant-cv.component.html',
    imports: [
        DateInputDirective,
        FormsModule,
        ButtonBusyDirective,
        LocalizePipe,
        TabsetComponent,
        TabDirective,
        CvQualificationsComponent,
        CvWorkExpsComponent,
        CvSkillsComponent,
        CvLanguagesComponent,
    ],
})
export class ApplicantCvComponent extends AppComponentBase {
    private _service = inject(ApplicantCVsServiceProxy);
    private _jobTitlesService = inject(JobTitlesServiceProxy);

    readonly applicantId = input<number>();

    readonly loading = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditApplicantCVDto | undefined>(undefined);

    readonly jobTitles = signal<SimpleLookupDto[]>([]);

    constructor() {
        super();
        effect(() => {
            if (this.applicantId()) {
                this.loadCv();
            }
        });

        this._jobTitlesService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.jobTitles.set(result.items));
    }

    private loadCv(): void {
        this.loading.set(true);
        this._service
            .getAll(
                new GetAllApplicantCVsInput({
                    applicantId: this.applicantId(),
                    maxResultCount: 1,
                    skipCount: 0,
                    sorting: undefined,
                })
            )
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe((result) => {
                if (result.items.length) {
                    this._service.getApplicantCVForEdit(result.items[0].id).subscribe((edit) => {
                        this.record.set(edit.applicantCV);
                    });
                } else {
                    const record = new CreateOrEditApplicantCVDto();
                    record.applicantId = this.applicantId();
                    this.record.set(record);
                }
            });
    }

    save(): void {
        this.saving.set(true);
        this._service
            .createOrEdit(this.record())
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe(() => {
                this.notify.info(this.l('SavedSuccessfully'));
                this.loadCv();
            });
    }

    updateField(field: keyof CreateOrEditApplicantCVDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditApplicantCVDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: 'uploadDate', value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }
}
