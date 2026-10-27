import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { CreateOrEditPAFormDto, PAFormsServiceProxy } from '@shared/service-proxies/service-proxies';
import { TabsetComponent, TabDirective } from 'ngx-bootstrap/tabs';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PAPlansComponent } from './pa-plans/pa-plans.component';
import { PAQuarter1ReviewsComponent } from './pa-quarter1-reviews/pa-quarter1-reviews.component';
import { PAMidYearReviewsComponent } from './pa-mid-year-reviews/pa-mid-year-reviews.component';
import { PAQuarter3ReviewsComponent } from './pa-quarter3-reviews/pa-quarter3-reviews.component';
import { PAAnnualYearReviewsComponent } from './pa-annual-year-reviews/pa-annual-year-reviews.component';
import { PAGoalsComponent } from './pa-goals/pa-goals.component';
import { PAPeerReviewsComponent } from './pa-peer-reviews/pa-peer-reviews.component';

@Component({
    templateUrl: './pa-form-detail.component.html',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        TabsetComponent,
        TabDirective,
        LocalizePipe,
        PAPlansComponent,
        PAQuarter1ReviewsComponent,
        PAMidYearReviewsComponent,
        PAQuarter3ReviewsComponent,
        PAAnnualYearReviewsComponent,
        PAGoalsComponent,
        PAPeerReviewsComponent,
    ],
})
export class PAFormDetailComponent extends AppComponentBase implements OnInit {
    private _route = inject(ActivatedRoute);
    private _service = inject(PAFormsServiceProxy);

    readonly paFormId = signal<number | undefined>(undefined);
    readonly paForm = signal<CreateOrEditPAFormDto | undefined>(undefined);

    ngOnInit(): void {
        this._route.paramMap.subscribe((params) => {
            const id = Number(params.get('id'));
            if (id) {
                this.paFormId.set(id);
                this.loadPAForm(id);
            }
        });
    }

    private loadPAForm(id: number): void {
        this._service.getPAFormForEdit(id).subscribe((result) => {
            this.paForm.set(result.paForm);
        });
    }
}
