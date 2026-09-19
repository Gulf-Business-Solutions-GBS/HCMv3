import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditEmployeeGroupDto,
    CreateOrEditEmployeeSubGroupDto,
    EmployeeGroupsServiceProxy,
    EmployeeSubGroupsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { BreadcrumbStep, RecordBreadcrumbComponent } from '@app/shared/common/breadcrumb/record-breadcrumb.component';
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

interface SubGroupSection {
    /** Goes in the URL fragment, so a refresh or a shared link lands on the same list. */
    id: string;
    labelKey: string;
}

const SUB_GROUP_SECTIONS: SubGroupSection[] = [
    { id: 'absence-types', labelKey: 'EmployeeSubGroupAbsenceTypes' },
    { id: 'absences', labelKey: 'EmployeeSubGroupAbsences' },
    { id: 'asset-allowances', labelKey: 'EmployeeSubGroupAssetAllowances' },
    { id: 'benefits', labelKey: 'EmployeeSubGroupBenefits' },
    { id: 'bonuses', labelKey: 'EmployeeSubGroupBonuses' },
    { id: 'commissions', labelKey: 'EmployeeSubGroupCommissions' },
    { id: 'funds', labelKey: 'EmployeeSubGroupFunds' },
    { id: 'gratuities', labelKey: 'EmployeeSubGroupGratuities' },
    { id: 'health-reimburses', labelKey: 'EmployeeSubGroupHealthReimburses' },
    { id: 'payments', labelKey: 'EmployeeSubGroupPayments' },
    { id: 'tickets', labelKey: 'EmployeeSubGroupTickets' },
    { id: 'wage-types', labelKey: 'EmployeeSubGroupWageTypes' },
];

/**
 * One employee sub group and the twelve lists that hang off it.
 *
 * The lists are down the left, as on the employee profile, rather than as tabs across the top: twelve long
 * names wrapped onto three rows of tabs, and it was hard to see which one was open. Only the open list is
 * built. Reached from its employee group's page; the breadcrumb and Back lead there.
 */
@Component({
    templateUrl: './employee-sub-group-detail.component.html',
    styleUrl: './employee-sub-group-detail.component.css',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        RecordBreadcrumbComponent,
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
    private _router = inject(Router);
    private _employeeSubGroupsService = inject(EmployeeSubGroupsServiceProxy);
    private _employeeGroupsService = inject(EmployeeGroupsServiceProxy);

    readonly sections = SUB_GROUP_SECTIONS;

    readonly employeeSubGroupId = signal<number | undefined>(undefined);
    readonly employeeSubGroup = signal<CreateOrEditEmployeeSubGroupDto | undefined>(undefined);
    readonly employeeGroup = signal<CreateOrEditEmployeeGroupDto | undefined>(undefined);
    readonly activeSectionId = signal<string>(SUB_GROUP_SECTIONS[0].id);

    readonly title = computed(() => {
        const subGroup = this.employeeSubGroup();
        return subGroup ? `${subGroup.code} - ${subGroup.nameEn}` : '';
    });

    readonly activeSection = computed(() => this.sections.find((s) => s.id === this.activeSectionId()));

    /** Employee Groups > the group > this sub group > the open list. */
    readonly breadcrumb = computed<BreadcrumbStep[]>(() => {
        const steps: BreadcrumbStep[] = [{ label: 'EmployeeGroups', isKey: true, route: '/app/admin/employee-groups' }];

        const group = this.employeeGroup();
        if (group) {
            steps.push({ label: `${group.code} - ${group.nameEn}`, route: `/app/admin/employee-groups/${group.id}` });
        }

        if (this.title()) {
            steps.push({ label: this.title() });
        }

        const section = this.activeSection();
        if (section) {
            steps.push({ label: section.labelKey, isKey: true });
        }

        return steps;
    });

    ngOnInit(): void {
        this._route.paramMap.subscribe((params) => {
            const id = Number(params.get('id'));
            if (id) {
                this.employeeSubGroupId.set(id);
                this.loadEmployeeSubGroup(id);
            }
        });

        this._route.fragment.subscribe((fragment) => {
            const requested = this.sections.find((s) => s.id === fragment);
            this.activeSectionId.set((requested ?? this.sections[0]).id);
        });
    }

    selectSection(section: SubGroupSection): void {
        if (this.activeSectionId() === section.id) {
            return;
        }

        this.activeSectionId.set(section.id);
        // replaceUrl: moving between lists of one record is not a place for the back button to revisit.
        this._router.navigate([], { relativeTo: this._route, fragment: section.id, replaceUrl: true });
    }

    /** Back to the group this sub group belongs to; the group list if that is not known yet. */
    goBack(): void {
        const groupId = this.employeeSubGroup()?.employeeGroupId;
        this._router.navigate(groupId ? ['/app/admin/employee-groups', groupId] : ['/app/admin/employee-groups']);
    }

    private loadEmployeeSubGroup(id: number): void {
        this._employeeSubGroupsService.getEmployeeSubGroupForEdit(id).subscribe((result) => {
            this.employeeSubGroup.set(result.employeeSubGroup);

            const groupId = result.employeeSubGroup?.employeeGroupId;
            if (groupId) {
                this._employeeGroupsService
                    .getEmployeeGroupForEdit(groupId)
                    .subscribe((group) => this.employeeGroup.set(group.employeeGroup));
            }
        });
    }
}
