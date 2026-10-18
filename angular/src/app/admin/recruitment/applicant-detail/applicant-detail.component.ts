import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    ApplicantsServiceProxy,
    CreateOrEditApplicantDto,
    EmployeeDto,
    EmployeesServiceProxy,
    GetAllEmployeesInput,
} from '@shared/service-proxies/service-proxies';
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
    private _employeesService = inject(EmployeesServiceProxy);

    readonly applicantId = signal<number | undefined>(undefined);
    readonly applicant = signal<CreateOrEditApplicantDto | undefined>(undefined);
    /** Used only to show the onboarding coordinator and buddy by name. */
    readonly employees = signal<EmployeeDto[]>([]);

    ngOnInit(): void {
        this._employeesService
            .getAll(new GetAllEmployeesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.employees.set(result.items));

        this._route.paramMap.subscribe((params) => {
            const id = Number(params.get('id'));
            if (id) {
                this.applicantId.set(id);
                this.loadApplicant(id);
            }
        });
    }

    employeeName(employeeId: number | undefined): string {
        if (!employeeId) {
            return '-';
        }
        const employee = this.employees().find((e) => e.id === employeeId);
        return employee ? `${employee.code} - ${employee.fullNameEn}` : '-';
    }

    private loadApplicant(id: number): void {
        this._applicantsService.getApplicantForEdit(id).subscribe((result) => {
            this.applicant.set(result.applicant);
        });
    }
}
