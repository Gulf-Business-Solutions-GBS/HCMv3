import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { CreateOrEditPAEmployeeDto, PAEmployeesServiceProxy } from '@shared/service-proxies/service-proxies';
import { TabsetComponent, TabDirective } from 'ngx-bootstrap/tabs';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PAEmployeeDetailsComponent } from './pa-employee-details/pa-employee-details.component';
import { PAEmployeeReviewersComponent } from './pa-employee-reviewers/pa-employee-reviewers.component';
import { PAEmployeeRatingsComponent } from './pa-employee-ratings/pa-employee-ratings.component';

@Component({
    templateUrl: './pa-employee-detail.component.html',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        TabsetComponent,
        TabDirective,
        LocalizePipe,
        PAEmployeeDetailsComponent,
        PAEmployeeReviewersComponent,
        PAEmployeeRatingsComponent,
    ],
})
export class PAEmployeeDetailComponent extends AppComponentBase implements OnInit {
    private _route = inject(ActivatedRoute);
    private _service = inject(PAEmployeesServiceProxy);

    readonly paEmployeeId = signal<number | undefined>(undefined);
    readonly paEmployee = signal<CreateOrEditPAEmployeeDto | undefined>(undefined);

    ngOnInit(): void {
        this._route.paramMap.subscribe((params) => {
            const id = Number(params.get('id'));
            if (id) {
                this.paEmployeeId.set(id);
                this.loadPAEmployee(id);
            }
        });
    }

    private loadPAEmployee(id: number): void {
        this._service.getPAEmployeeForEdit(id).subscribe((result) => {
            this.paEmployee.set(result.paEmployee);
        });
    }
}
