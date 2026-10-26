import { Component, computed, effect, inject, input, output, signal, untracked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditEmployeeBasicPayDto,
    EmployeeBasicPaysServiceProxy,
    GetAllEmployeeBasicPaysInput,
} from '@shared/service-proxies/service-proxies';
import { finalize } from 'rxjs/operators';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { EmployeeBasicPayFormComponent } from './employee-basic-pay-form.component';
import { EmployeeBasicPayLinesTabComponent } from './employee-basic-pay-lines-tab.component';

interface EmployeeBasicPayDetailTab {
    id: string;
    labelKey: string;
    permission: string;
}

const EMPLOYEE_BASIC_PAY_DETAIL_TABS: EmployeeBasicPayDetailTab[] = [
    { id: 'wage-types', labelKey: 'WageTypes', permission: 'Pages.PersonnelAdministration.EmployeeBasicPayDetails' },
];

/**
 * One basic pay of the employee, shown inside the Basic Pays section in place of the list:
 * its own fields editable in place, and its wage type lines as a tab below.
 */
@Component({
    selector: 'employeeBasicPayDetailView',
    templateUrl: './employee-basic-pay-detail-view.component.html',
    styleUrl: './employee-basic-pay-detail-view.component.css',
    imports: [
        BusyIfDirective,
        ButtonBusyDirective,
        FormsModule,
        EmployeeBasicPayFormComponent,
        EmployeeBasicPayLinesTabComponent,
        LocalizePipe,
    ],
})
export class EmployeeBasicPayDetailViewComponent extends AppComponentBase {
    private _service = inject(EmployeeBasicPaysServiceProxy);

    readonly employeeId = input.required<number>();
    readonly basicPayId = input.required<number>();

    /** Back to the list. */
    readonly back = output<void>();

    readonly record = signal<CreateOrEditEmployeeBasicPayDto>(new CreateOrEditEmployeeBasicPayDto());
    readonly loaded = signal<boolean>(false);
    readonly loading = signal<boolean>(false);
    readonly saving = signal<boolean>(false);

    /** Sum of the wage type lines, recomputed by the backend whenever a line changes. */
    readonly totalAmount = signal<number | undefined>(undefined);

    /** Period as last loaded or saved, so the title does not change while the dates are being edited. */
    readonly savedPeriod = signal<string>('');

    readonly title = computed(() =>
        this.savedPeriod() ? `${this.l('BasicPay')}: ${this.savedPeriod()}` : this.l('BasicPay')
    );

    readonly canEdit = this.permission.isGranted('Pages.PersonnelAdministration.EmployeeBasicPays.Edit');

    readonly tabs = EMPLOYEE_BASIC_PAY_DETAIL_TABS.filter((tab) => this.permission.isGranted(tab.permission));
    readonly activeTabId = signal<string | undefined>(this.tabs[0]?.id);

    constructor() {
        super();
        effect(() => {
            const id = this.basicPayId();
            untracked(() => this.loadRecord(id));
        });
    }

    loadRecord(id: number): void {
        this.loading.set(true);
        this._service
            .getEmployeeBasicPayForEdit(id)
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe((result) => {
                // A basic pay id from the URL that belongs to someone else is not shown on this employee.
                if (!result.employeeBasicPay || result.employeeBasicPay.employeeId !== this.employeeId()) {
                    this.back.emit();
                    return;
                }

                this.record.set(result.employeeBasicPay);
                this.setSavedPeriod(result.employeeBasicPay);
                this.loaded.set(true);
            });

        this.loadTotalAmount();
    }

    /**
     * The edit DTO carries no amount, and there is no single-record read that does, so the total is
     * taken from this employee's basic pay list (a handful of rows per employee).
     */
    loadTotalAmount(): void {
        this._service
            .getAll(
                new GetAllEmployeeBasicPaysInput({
                    employeeId: this.employeeId(),
                    sorting: undefined,
                    skipCount: 0,
                    maxResultCount: 1000,
                })
            )
            .subscribe((result) => {
                const row = result.items.find((item) => item.id === this.basicPayId());
                this.totalAmount.set(row?.amount);
            });
    }

    selectTab(tab: EmployeeBasicPayDetailTab): void {
        this.activeTabId.set(tab.id);
    }

    save(): void {
        if (!this.canEdit) {
            return;
        }

        this.saving.set(true);
        this._service
            .createOrEdit(this.record())
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe(() => {
                this.setSavedPeriod(this.record());
                this.notify.info(this.l('SavedSuccessfully'));
            });
    }

    goBack(): void {
        this.back.emit();
    }

    private setSavedPeriod(record: CreateOrEditEmployeeBasicPayDto): void {
        const start = record?.startDate?.toFormat('yyyy-MM-dd') ?? '';
        const end = record?.endDate?.toFormat('yyyy-MM-dd') ?? '';
        this.savedPeriod.set(start || end ? `${start} - ${end}` : '');
    }
}
