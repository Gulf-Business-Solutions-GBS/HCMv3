import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { CreateOrEditJobDescriptionDto, JobDescriptionsServiceProxy } from '@shared/service-proxies/service-proxies';
import { TabsetComponent, TabDirective } from 'ngx-bootstrap/tabs';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { JobDescriptionCertificatesComponent } from './certificates/job-description-certificates.component';
import { JobDescriptionDegreesComponent } from './degrees/job-description-degrees.component';
import { JobDescriptionInterviewQuestionsComponent } from './interview-questions/job-description-interview-questions.component';
import { JobDescriptionResponsibilitiesComponent } from './responsibilities/job-description-responsibilities.component';
import { JobDescriptionSkillsComponent } from './skills/job-description-skills.component';
import { JobDescriptionCompetenciesComponent } from './competencies/job-description-competencies.component';

@Component({
    templateUrl: './job-description-detail.component.html',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        TabsetComponent,
        TabDirective,
        LocalizePipe,
        JobDescriptionCertificatesComponent,
        JobDescriptionDegreesComponent,
        JobDescriptionInterviewQuestionsComponent,
        JobDescriptionResponsibilitiesComponent,
        JobDescriptionSkillsComponent,
        JobDescriptionCompetenciesComponent,
    ],
})
export class JobDescriptionDetailComponent extends AppComponentBase implements OnInit {
    private _route = inject(ActivatedRoute);
    private _jobDescriptionsService = inject(JobDescriptionsServiceProxy);

    readonly jobDescriptionId = signal<number | undefined>(undefined);
    readonly jobDescription = signal<CreateOrEditJobDescriptionDto | undefined>(undefined);

    ngOnInit(): void {
        this._route.paramMap.subscribe((params) => {
            const id = Number(params.get('id'));
            if (id) {
                this.jobDescriptionId.set(id);
                this.loadJobDescription(id);
            }
        });
    }

    private loadJobDescription(id: number): void {
        this._jobDescriptionsService.getJobDescriptionForEdit(id).subscribe((result) => {
            this.jobDescription.set(result.jobDescription);
        });
    }
}
