import { Component, input, model, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePicker } from 'primeng/datepicker';
import { Select } from 'primeng/select';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { EmployeeStatus } from '@shared/service-proxies/service-proxies';

/**
 * SHARED FILTER PANEL for the legacy "Analysis" grid-report family
 * (HCM10.Web/UserControls/Reports/Analysis/**\/usc*.ascx, ~140 screens).
 *
 * An inventory of the family shows the same filters repeating: dtFrom/dtTo (24 of 25 sampled),
 * txtEmpCode (21) and cmbEmpStatus (20), plus one or two report-specific fields. Those three live here
 * and every report gets them for free; report-specific fields are PROJECTED into the panel through
 * <ng-content>, so they sit on the same row and inherit the same layout:
 *
 * ```html
 * <grid-report-filter
 *     [(fromDate)]="fromDate"
 *     [(toDate)]="toDate"
 *     [(employeeCode)]="employeeCode"
 *     [(employeeStatus)]="employeeStatus"
 *     [exporting]="exporting()"
 *     (search)="reloadPage()"
 *     (exportExcel)="exportToExcel()">
 *     <!-- optional report-specific fields here -->
 * </grid-report-filter>
 * ```
 *
 * This component is the exact UI mirror of the backend `GridReportInputBase` DTO. Fields not used by a
 * given report are switched off with [showEmployeeCode]/[showEmployeeStatus]/[showDateRange] rather than
 * by forking the component.
 *
 * Legacy default: blank dates meant "today" (uscRptEmployeeContract.ascx.cs:128-139), so both date
 * pickers start on today. The backend applies the same default if a null date ever reaches it.
 */
@Component({
    selector: 'grid-report-filter',
    templateUrl: './grid-report-filter.component.html',
    imports: [FormsModule, DatePicker, Select, LocalizePipe],
})
export class GridReportFilterComponent extends AppComponentBase {
    /** Legacy dtFrom. */
    readonly fromDate = model<Date | undefined>(new Date());

    /** Legacy dtTo. */
    readonly toDate = model<Date | undefined>(new Date());

    /** Legacy txtEmpCode: one code or several separated by commas. */
    readonly employeeCode = model<string>('');

    /** Legacy cmbEmpStatus. Undefined means "all statuses". */
    readonly employeeStatus = model<EmployeeStatus | undefined>(undefined);

    readonly showDateRange = input<boolean>(true);
    readonly showEmployeeCode = input<boolean>(true);
    readonly showEmployeeStatus = input<boolean>(true);
    readonly showExport = input<boolean>(true);

    /** Disables both buttons while a request is in flight. */
    readonly loading = input<boolean>(false);
    readonly exporting = input<boolean>(false);

    /** Legacy "Get Data" button. */
    readonly search = output<void>();

    /** Legacy ASPxGridViewExporter / Excel button. */
    readonly exportExcel = output<void>();

    readonly statusOptions = [
        { label: this.l('Active'), value: EmployeeStatus.Active },
        { label: this.l('Inactive'), value: EmployeeStatus.Inactive },
    ];
}
