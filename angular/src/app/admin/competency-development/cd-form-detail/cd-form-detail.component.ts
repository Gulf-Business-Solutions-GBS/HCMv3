import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { CreateOrEditCDFormDto, CDFormsServiceProxy } from '@shared/service-proxies/service-proxies';
import { TabsetComponent, TabDirective } from 'ngx-bootstrap/tabs';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { CDPlansComponent } from './cd-plans/cd-plans.component';
import { CDMidYearReviewsComponent } from './cd-mid-year-reviews/cd-mid-year-reviews.component';
import { CDAnnualYearReviewsComponent } from './cd-annual-year-reviews/cd-annual-year-reviews.component';

@Component({
    templateUrl: './cd-form-detail.component.html',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        TabsetComponent,
        TabDirective,
        LocalizePipe,
        CDPlansComponent,
        CDMidYearReviewsComponent,
        CDAnnualYearReviewsComponent,
    ],
})
export class CDFormDetailComponent extends AppComponentBase implements OnInit {
    private _route = inject(ActivatedRoute);
    private _service = inject(CDFormsServiceProxy);

    readonly cdFormId = signal<number | undefined>(undefined);
    readonly cdForm = signal<CreateOrEditCDFormDto | undefined>(undefined);

    ngOnInit(): void {
        this._route.paramMap.subscribe((params) => {
            const id = Number(params.get('id'));
            if (id) {
                this.cdFormId.set(id);
                this.loadCDForm(id);
            }
        });
    }

    private loadCDForm(id: number): void {
        this._service.getCDFormForEdit(id).subscribe((result) => {
            this.cdForm.set(result.cdForm);
        });
    }
}
