import { Component, OnInit, inject, viewChild, signal } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    EmployeeSurveyDetailDto,
    EmployeeSurveyDetailsServiceProxy,
    GetAllEmployeeSurveyDetailsInput,
} from '@shared/service-proxies/service-proxies';
import { Table, TableModule } from 'primeng/table';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { LazyLoadEvent } from 'primeng/api';
import { TieredMenu } from 'primeng/tieredmenu';
import { MenuItem } from 'primeng/api';
import { Tooltip } from 'primeng/tooltip';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { CreateOrEditEmployeeSurveyDetailModalComponent } from './create-or-edit-employee-survey-detail-modal.component';
import { SubHeaderComponent } from '../../../shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '../../../../shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { PermissionAnyPipe } from '@shared/common/pipes/permission-any.pipe';

@Component({
    templateUrl: './employee-survey-details.component.html',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        BusyIfDirective,
        FormsModule,
        TableModule,
        PaginatorModule,
        TieredMenu,
        Tooltip,
        CreateOrEditEmployeeSurveyDetailModalComponent,
        LocalizePipe,
        PermissionPipe,
        PermissionAnyPipe,
    ],
})
export class EmployeeSurveyDetailsComponent extends AppComponentBase implements OnInit {
    private _employeeSurveyDetailsService = inject(EmployeeSurveyDetailsServiceProxy);

    readonly createOrEditModal = viewChild<CreateOrEditEmployeeSurveyDetailModalComponent>('createOrEditModal');
    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly activeMenuItems = signal<MenuItem[]>([]);

    ngOnInit(): void {
        // Intentionally empty: the PrimeNG table's (onLazyLoad) triggers the first getEmployeeSurveyDetails().
    }

    getEmployeeSurveyDetails(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._employeeSurveyDetailsService
            .getAll(
                new GetAllEmployeeSurveyDetailsInput({
                    employeeSurveyId: undefined,
                    sorting: this.primengTableHelper.getSorting(this.dataTable()),
                    maxResultCount: this.primengTableHelper.getMaxResultCount(this.paginator(), event),
                    skipCount: this.primengTableHelper.getSkipCount(this.paginator(), event),
                })
            )
            .pipe(finalize(() => this.primengTableHelper.hideLoadingIndicator()))
            .subscribe((result) => {
                this.primengTableHelper.totalRecordsCount.set(result.totalCount);
                this.primengTableHelper.records.set(result.items);
                this.primengTableHelper.hideLoadingIndicator();
            });
    }

    reloadPage(): void {
        const rows = this.paginator().rows || this.primengTableHelper.defaultRecordsCountPerPage;
        this.getEmployeeSurveyDetails({ first: this.paginator().getPage() * rows, rows } as LazyLoadEvent);
    }

    createEmployeeSurveyDetail(): void {
        this.createOrEditModal().show();
    }

    showMenu(event: Event, record: EmployeeSurveyDetailDto, menu: any): void {
        this.activeMenuItems.set(this.getMenuItems(record));
        menu.toggle(event);
    }

    getMenuItems(record: EmployeeSurveyDetailDto): MenuItem[] {
        const items: MenuItem[] = [];

        if (this.permission.isGranted('Pages.SurveyManagement.EmployeeSurveyDetails.Edit')) {
            items.push({
                label: this.l('Edit'),
                icon: 'pi pi-pencil',
                command: () => this.createOrEditModal().show(record.id),
            });
        }

        if (this.permission.isGranted('Pages.SurveyManagement.EmployeeSurveyDetails.Delete')) {
            items.push({
                label: this.l('Delete'),
                icon: 'pi pi-trash',
                command: () => this.deleteEmployeeSurveyDetail(record),
            });
        }

        return items;
    }

    deleteEmployeeSurveyDetail(record: EmployeeSurveyDetailDto): void {
        this.message.confirm(this.l('SimpleLookupDeleteWarningMessage', record.questionText), this.l('AreYouSure'), (isConfirmed) => {
            if (isConfirmed) {
                this._employeeSurveyDetailsService.delete(record.id).subscribe(() => {
                    this.reloadPage();
                    this.notify.success(this.l('SuccessfullyDeleted'));
                });
            }
        });
    }
}
