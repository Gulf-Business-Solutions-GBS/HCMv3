import { Component, OnInit, inject, signal } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { EmployeeDto, EmployeesServiceProxy, GetAllEmployeesInput, ReportsServiceProxy } from '@shared/service-proxies/service-proxies';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    templateUrl: './report-preview.component.html',
    animations: [appModuleAnimation()],
    imports: [SubHeaderComponent, BusyIfDirective, FormsModule, LocalizePipe],
})
export class ReportPreviewComponent extends AppComponentBase implements OnInit {
    private _employeesService = inject(EmployeesServiceProxy);
    private _reportsService = inject(ReportsServiceProxy);

    readonly employees = signal<EmployeeDto[]>([]);
    readonly selectedEmployeeId = signal<number | undefined>(undefined);
    readonly generatingSalaryCertificate = signal<boolean>(false);
    readonly generatingPayslip = signal<boolean>(false);

    ngOnInit(): void {
        this._employeesService
            .getAll(new GetAllEmployeesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.employees.set(result.items));
    }

    previewSalaryCertificate(): void {
        const employeeId = this.selectedEmployeeId();
        if (!employeeId) {
            this.message.warn(this.l('PleaseSelectAnEmployeeFirst'));
            return;
        }

        this.generatingSalaryCertificate.set(true);
        const pdfTab = window.open('', '_blank');
        this._reportsService
            .getSalaryCertificatePdf(employeeId)
            .pipe(finalize(() => this.generatingSalaryCertificate.set(false)))
            .subscribe((result) => this.openPdf(pdfTab, result.base64Content));
    }

    previewPayslip(): void {
        const employeeId = this.selectedEmployeeId();
        if (!employeeId) {
            this.message.warn(this.l('PleaseSelectAnEmployeeFirst'));
            return;
        }

        this.generatingPayslip.set(true);
        const pdfTab = window.open('', '_blank');
        this._reportsService
            .getPayslipPdf(employeeId)
            .pipe(finalize(() => this.generatingPayslip.set(false)))
            .subscribe((result) => this.openPdf(pdfTab, result.base64Content));
    }

    private openPdf(pdfTab: Window | null, base64Content: string): void {
        const byteCharacters = atob(base64Content);
        const byteNumbers = new Array(byteCharacters.length);
        for (let i = 0; i < byteCharacters.length; i++) {
            byteNumbers[i] = byteCharacters.charCodeAt(i);
        }
        const byteArray = new Uint8Array(byteNumbers);
        const blob = new Blob([byteArray], { type: 'application/pdf' });
        const url = URL.createObjectURL(blob);
        if (pdfTab) {
            pdfTab.location.href = url;
        } else {
            this.message.warn(this.l('PopupBlockedPleaseAllowPopups'));
        }
    }
}
