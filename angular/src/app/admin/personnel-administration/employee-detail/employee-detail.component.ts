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
import { EmployeeAbsencesComponent } from './absences/employee-absences.component';
import { EmployeeAbsenceQuotasComponent } from './absence-quotas/employee-absence-quotas.component';
import { EmployeeAbsAdjustmentsComponent } from './abs-adjustments/employee-abs-adjustments.component';
import { EmployeeAbsQuotaForfeitsComponent } from './abs-quota-forfeits/employee-abs-quota-forfeits.component';
import { EmployeeAttendancesComponent } from './attendances/employee-attendances.component';
import { EmployeeAttendanceQuotasComponent } from './attendance-quotas/employee-attendance-quotas.component';
import { EmployeeBasicPaysComponent } from './basic-pays/employee-basic-pays.component';
import { EmployeeOnHoldsComponent } from './on-holds/employee-on-holds.component';
import { EmployeeGratuitiesComponent } from './gratuities/employee-gratuities.component';
import { EmployeeGratuityQuotasComponent } from './gratuity-quotas/employee-gratuity-quotas.component';
import { EmployeeLoansComponent } from './loans/employee-loans.component';
import { EmployeeFundsComponent } from './funds/employee-funds.component';
import { EmployeeTicketsComponent } from './tickets/employee-tickets.component';
import { EmployeeTicketQuotasComponent } from './ticket-quotas/employee-ticket-quotas.component';
import { EmployeeTicketRoutesComponent } from './ticket-routes/employee-ticket-routes.component';
import { EmployeeTktAdjustmentsComponent } from './tkt-adjustments/employee-tkt-adjustments.component';
import { EmployeeSocialSecuritiesComponent } from './social-securities/employee-social-securities.component';
import { EmployeeSocialSecurityClaimsComponent } from './social-security-claims/employee-social-security-claims.component';
import { EmployeeDependantsComponent } from './dependants/employee-dependants.component';
import { EmployeeSponsorsComponent } from './sponsors/employee-sponsors.component';
import { EmployeeFnFPaymentsComponent } from './fnf-payments/employee-fnf-payments.component';
import { EmployeeBanksComponent } from './banks/employee-banks.component';
import { EmployeeAssetsComponent } from './assets/employee-assets.component';
import { EmployeeDisciplinariesComponent } from './disciplinaries/employee-disciplinaries.component';
import { EmployeeGrievancesComponent } from './grievances/employee-grievances.component';
import { EmployeeHealthReimbursesComponent } from './health-reimburses/employee-health-reimburses.component';
import { EmployeeOvertimesComponent } from './overtimes/employee-overtimes.component';
import { EmployeePaymentsComponent } from './payments/employee-payments.component';
import { EmployeeRemunerationsComponent } from './remunerations/employee-remunerations.component';
import { EmployeeRewardsComponent } from './rewards/employee-rewards.component';
import { EmployeeCostDistributionsComponent } from './cost-distributions/employee-cost-distributions.component';
import { EmployeeAccountAssignmentsComponent } from './account-assignments/employee-account-assignments.component';
import { EmployeeExtdWorkLocationsComponent } from './extd-work-locations/employee-extd-work-locations.component';
import { EmployeeBenefitExceptionsComponent } from './benefit-exceptions/employee-benefit-exceptions.component';
import { EmployeeNonPayrollTransComponent } from './non-payroll-trans/employee-non-payroll-trans.component';
import { EmployeeTasksComponent } from './tasks/employee-tasks.component';

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
        EmployeeAbsencesComponent,
        EmployeeAbsenceQuotasComponent,
        EmployeeAbsAdjustmentsComponent,
        EmployeeAbsQuotaForfeitsComponent,
        EmployeeAttendancesComponent,
        EmployeeAttendanceQuotasComponent,
        EmployeeBasicPaysComponent,
        EmployeeOnHoldsComponent,
        EmployeeGratuitiesComponent,
        EmployeeGratuityQuotasComponent,
        EmployeeLoansComponent,
        EmployeeFundsComponent,
        EmployeeTicketsComponent,
        EmployeeTicketQuotasComponent,
        EmployeeTicketRoutesComponent,
        EmployeeTktAdjustmentsComponent,
        EmployeeSocialSecuritiesComponent,
        EmployeeSocialSecurityClaimsComponent,
        EmployeeDependantsComponent,
        EmployeeSponsorsComponent,
        EmployeeFnFPaymentsComponent,
        EmployeeBanksComponent,
        EmployeeAssetsComponent,
        EmployeeDisciplinariesComponent,
        EmployeeGrievancesComponent,
        EmployeeHealthReimbursesComponent,
        EmployeeOvertimesComponent,
        EmployeePaymentsComponent,
        EmployeeRemunerationsComponent,
        EmployeeRewardsComponent,
        EmployeeCostDistributionsComponent,
        EmployeeAccountAssignmentsComponent,
        EmployeeExtdWorkLocationsComponent,
        EmployeeBenefitExceptionsComponent,
        EmployeeNonPayrollTransComponent,
        EmployeeTasksComponent,
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
