import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { ApplicantsServiceProxy, CreateOrEditApplicantDto } from '@shared/service-proxies/service-proxies';
import { TabsetComponent, TabDirective } from 'ngx-bootstrap/tabs';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { ApplicantCvComponent } from './cv/applicant-cv.component';

@Component({
    templateUrl: './applicant-detail.component.html',
    animations: [appModuleAnimation()],
    imports: [SubHeaderComponent, TabsetComponent, TabDirective, LocalizePipe, ApplicantCvComponent],
})
export class ApplicantDetailComponent extends AppComponentBase implements OnInit {
    private _route = inject(ActivatedRoute);
    private _applicantsService = inject(ApplicantsServiceProxy);

    readonly applicantId = signal<number | undefined>(undefined);
    readonly applicant = signal<CreateOrEditApplicantDto | undefined>(undefined);

    ngOnInit(): void {
        this._route.paramMap.subscribe((params) => {
            const id = Number(params.get('id'));
            if (id) {
                this.applicantId.set(id);
                this.loadApplicant(id);
            }
        });
    }

    private loadApplicant(id: number): void {
        this._applicantsService.getApplicantForEdit(id).subscribe((result) => {
            this.applicant.set(result.applicant);
        });
    }
}
