import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { CreateOrEditEmployeeDto, EmployeesServiceProxy } from '@shared/service-proxies/service-proxies';
import { TabsetComponent, TabDirective } from 'ngx-bootstrap/tabs';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { EmployeeDatesComponent } from './dates/employee-dates.component';
import { EmployeeManagersComponent } from './managers/employee-managers.component';
import { EmployeeDottedLineManagersComponent } from './dotted-line-managers/employee-dotted-line-managers.component';
import { EmployeeDelegationsComponent } from './delegations/employee-delegations.component';
import { EmployeeRelationshipsComponent } from './relationships/employee-relationships.component';
import { EmployeeEducationsComponent } from './education/employee-educations.component';
import { EmployeeCertificationsComponent } from './certifications/employee-certifications.component';
import { EmployeeWorkExperiencesComponent } from './work-experience/employee-work-experiences.component';
import { EmployeeDocumentsComponent } from './documents/employee-documents.component';
import { EmployeeSignatureComponent } from './signature/employee-signature.component';
import { EmployeeVisaInfoComponent } from './visa-info/employee-visa-info.component';
import { EmployeeOrgAssignmentsComponent } from './org-assignments/employee-org-assignments.component';
import { EmployeeContractsComponent } from './contracts/employee-contracts.component';
import { EmployeeActionsComponent } from './actions/employee-actions.component';
import { EmployeeSeparationsComponent } from './separations/employee-separations.component';
import { EmployeeRejoinsComponent } from './rejoins/employee-rejoins.component';
import { EmployeeDutyPeriodsComponent } from './duty-periods/employee-duty-periods.component';
import { EmployeeTimePoliciesComponent } from './time-policies/employee-time-policies.component';
import { EmployeeSubstituteDutiesComponent } from './substitute-duties/employee-substitute-duties.component';
import { EmployeeDutySchedulesComponent } from './duty-schedules/employee-duty-schedules.component';
import { EmployeeTimesheetsComponent } from './timesheets/employee-timesheets.component';
import { EmployeeWageAccGroupsComponent } from './wage-acc-groups/employee-wage-acc-groups.component';
import { EmployeeWageAccountsComponent } from './wage-accounts/employee-wage-accounts.component';
import { EmployeePayrollStatusComponent } from './payroll-status/employee-payroll-status.component';

@Component({
    templateUrl: './employee-detail.component.html',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        TabsetComponent,
        TabDirective,
        LocalizePipe,
        EmployeeDatesComponent,
        EmployeeManagersComponent,
        EmployeeDottedLineManagersComponent,
        EmployeeDelegationsComponent,
        EmployeeRelationshipsComponent,
        EmployeeEducationsComponent,
        EmployeeCertificationsComponent,
        EmployeeWorkExperiencesComponent,
        EmployeeDocumentsComponent,
        EmployeeSignatureComponent,
        EmployeeVisaInfoComponent,
        EmployeeOrgAssignmentsComponent,
        EmployeeContractsComponent,
        EmployeeActionsComponent,
        EmployeeSeparationsComponent,
        EmployeeRejoinsComponent,
        EmployeeDutyPeriodsComponent,
        EmployeeTimePoliciesComponent,
        EmployeeSubstituteDutiesComponent,
        EmployeeDutySchedulesComponent,
        EmployeeTimesheetsComponent,
        EmployeeWageAccGroupsComponent,
        EmployeeWageAccountsComponent,
        EmployeePayrollStatusComponent,
    ],
})
export class EmployeeDetailComponent extends AppComponentBase implements OnInit {
    private _route = inject(ActivatedRoute);
    private _employeesService = inject(EmployeesServiceProxy);

    readonly employeeId = signal<number | undefined>(undefined);
    readonly employee = signal<CreateOrEditEmployeeDto | undefined>(undefined);

    ngOnInit(): void {
        this._route.paramMap.subscribe((params) => {
            const id = Number(params.get('id'));
            if (id) {
                this.employeeId.set(id);
                this.loadEmployee(id);
            }
        });
    }

    private loadEmployee(id: number): void {
        this._employeesService.getEmployeeForEdit(id).subscribe((result) => {
            this.employee.set(result.employee);
        });
    }
}
