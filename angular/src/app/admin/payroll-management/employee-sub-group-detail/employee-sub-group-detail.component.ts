import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { CreateOrEditEmployeeSubGroupDto, EmployeeSubGroupsServiceProxy } from '@shared/service-proxies/service-proxies';
import { TabsetComponent, TabDirective } from 'ngx-bootstrap/tabs';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { EmployeeSubGroupAbsenceTypesComponent } from './absence-types/employee-sub-group-absence-types.component';
import { EmployeeSubGroupAbsencesComponent } from './absences/employee-sub-group-absences.component';
import { EmployeeSubGroupAssetAllowancesComponent } from './asset-allowances/employee-sub-group-asset-allowances.component';
import { EmployeeSubGroupBenefitsComponent } from './benefits/employee-sub-group-benefits.component';
import { EmployeeSubGroupBonusesComponent } from './bonuses/employee-sub-group-bonuses.component';
import { EmployeeSubGroupCommissionsComponent } from './commissions/employee-sub-group-commissions.component';
import { EmployeeSubGroupFundsComponent } from './funds/employee-sub-group-funds.component';
import { EmployeeSubGroupGratuitiesComponent } from './gratuities/employee-sub-group-gratuities.component';
import { EmployeeSubGroupHealthReimbursesComponent } from './health-reimburses/employee-sub-group-health-reimburses.component';
import { EmployeeSubGroupPaymentsComponent } from './payments/employee-sub-group-payments.component';
import { EmployeeSubGroupTicketsComponent } from './tickets/employee-sub-group-tickets.component';
import { EmployeeSubGroupWageTypesComponent } from './wage-types/employee-sub-group-wage-types.component';

@Component({
    templateUrl: './employee-sub-group-detail.component.html',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        TabsetComponent,
        TabDirective,
        LocalizePipe,
        EmployeeSubGroupAbsenceTypesComponent,
        EmployeeSubGroupAbsencesComponent,
        EmployeeSubGroupAssetAllowancesComponent,
        EmployeeSubGroupBenefitsComponent,
        EmployeeSubGroupBonusesComponent,
        EmployeeSubGroupCommissionsComponent,
        EmployeeSubGroupFundsComponent,
        EmployeeSubGroupGratuitiesComponent,
        EmployeeSubGroupHealthReimbursesComponent,
        EmployeeSubGroupPaymentsComponent,
        EmployeeSubGroupTicketsComponent,
        EmployeeSubGroupWageTypesComponent,
    ],
})
export class EmployeeSubGroupDetailComponent extends AppComponentBase implements OnInit {
    private _route = inject(ActivatedRoute);
    private _employeeSubGroupsService = inject(EmployeeSubGroupsServiceProxy);

    readonly employeeSubGroupId = signal<number | undefined>(undefined);
    readonly employeeSubGroup = signal<CreateOrEditEmployeeSubGroupDto | undefined>(undefined);

    ngOnInit(): void {
        this._route.paramMap.subscribe((params) => {
            const id = Number(params.get('id'));
            if (id) {
                this.employeeSubGroupId.set(id);
                this.loadEmployeeSubGroup(id);
            }
        });
    }

    private loadEmployeeSubGroup(id: number): void {
        this._employeeSubGroupsService.getEmployeeSubGroupForEdit(id).subscribe((result) => {
            this.employeeSubGroup.set(result.employeeSubGroup);
        });
    }
}
