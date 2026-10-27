import { Component, OnInit, computed, inject, signal, viewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { CreateOrEditEmployeeGroupDto, EmployeeGroupsServiceProxy } from '@shared/service-proxies/service-proxies';
import { finalize } from 'rxjs/operators';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { BreadcrumbStep, RecordBreadcrumbComponent } from '@app/shared/common/breadcrumb/record-breadcrumb.component';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { CreateOrEditEmployeeGroupModalComponent } from '../create-or-edit-employee-group-modal.component';
import { EmployeeSubGroupsComponent } from '../../employee-sub-groups/employee-sub-groups.component';

/**
 * One employee group: its own settings, read-only, and its sub groups below.
 *
 * Sub groups live here rather than on a screen of their own because each belongs to exactly one group --
 * looking for "Default Grade" means first knowing it is a grade of "Default Normal Staff".
 */
@Component({
    templateUrl: './employee-group-detail.component.html',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        RecordBreadcrumbComponent,
        BusyIfDirective,
        LocalizePipe,
        CreateOrEditEmployeeGroupModalComponent,
        EmployeeSubGroupsComponent,
    ],
})
export class EmployeeGroupDetailComponent extends AppComponentBase implements OnInit {
    private _route = inject(ActivatedRoute);
    private _router = inject(Router);
    private _employeeGroupsService = inject(EmployeeGroupsServiceProxy);

    readonly editModal = viewChild<CreateOrEditEmployeeGroupModalComponent>('editModal');

    readonly employeeGroupId = signal<number | undefined>(undefined);
    readonly employeeGroup = signal<CreateOrEditEmployeeGroupDto | undefined>(undefined);
    readonly loading = signal<boolean>(false);

    readonly canEdit = this.permission.isGranted('Pages.PayrollManagement.EmployeeGroups.Edit');
    readonly canSeeSubGroups = this.permission.isGranted('Pages.PayrollManagement.EmployeeSubGroups');

    readonly title = computed(() => {
        const group = this.employeeGroup();
        return group ? `${group.code} - ${group.nameEn}` : '';
    });

    readonly breadcrumb = computed<BreadcrumbStep[]>(() => {
        const steps: BreadcrumbStep[] = [{ label: 'EmployeeGroups', isKey: true, route: '/app/admin/employee-groups' }];
        if (this.title()) {
            steps.push({ label: this.title() });
        }
        return steps;
    });

    ngOnInit(): void {
        this._route.paramMap.subscribe((params) => {
            const id = Number(params.get('id'));
            if (id) {
                this.employeeGroupId.set(id);
                this.load(id);
            }
        });
    }

    load(id: number): void {
        this.loading.set(true);
        this._employeeGroupsService
            .getEmployeeGroupForEdit(id)
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe((result) => this.employeeGroup.set(result.employeeGroup));
    }

    /** The group's settings are few, so they are changed in the same dialog the list uses. */
    edit(): void {
        const id = this.employeeGroupId();
        if (id) {
            this.editModal().show(id);
        }
    }

    goBack(): void {
        this._router.navigate(['/app/admin/employee-groups']);
    }
}
