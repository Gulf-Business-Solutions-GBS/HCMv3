import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { Tooltip } from 'primeng/tooltip';
import { finalize } from 'rxjs/operators';

import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { PrintReportLauncherService, RunnablePrintReport } from './print-report-launcher.service';

/**
 * Runtime launcher for the print-report catalog (the legacy HRMS "Print Reports" screen).
 *
 * This is NOT the catalog CRUD screen - that one lives under
 * `admin/report-management/print-reports`. This screen only lists the reports a user can actually
 * run, and routes to the DevExpress viewer (View) or the DevExpress designer (Edit).
 */
@Component({
    templateUrl: './print-reports-launcher.component.html',
    animations: [appModuleAnimation()],
    imports: [SubHeaderComponent, BusyIfDirective, FormsModule, TableModule, Tooltip, LocalizePipe, PermissionPipe],
})
export class PrintReportsLauncherComponent extends AppComponentBase implements OnInit {
    private _service = inject(PrintReportLauncherService);
    private _router = inject(Router);

    readonly isLoading = signal<boolean>(false);
    readonly records = signal<RunnablePrintReport[]>([]);
    readonly filterText = signal<string>('');

    /** Client-side filter over display name / code / category. */
    readonly filteredRecords = computed<RunnablePrintReport[]>(() => {
        const filter = this.filterText().trim().toLowerCase();
        const records = this.records();

        if (!filter) {
            return records;
        }

        return records.filter((record) =>
            [this.displayName(record), record.code, record.reportCategory, record.reportName]
                .filter((value) => !!value)
                .some((value) => value.toLowerCase().indexOf(filter) >= 0)
        );
    });

    ngOnInit(): void {
        this.loadReports();
    }

    loadReports(): void {
        this.isLoading.set(true);

        this._service
            .getRunnableReports()
            .pipe(finalize(() => this.isLoading.set(false)))
            .subscribe((items) => this.records.set(items ?? []));
    }

    /** Arabic display name when the UI is Arabic, English otherwise, falling back to whatever exists. */
    displayName(record: RunnablePrintReport): string {
        const preferred = this.isArabic() ? record.nameAr : record.nameEn;
        return preferred || record.nameEn || record.nameAr || record.code || record.reportName || '';
    }

    isArabic(): boolean {
        return abp.localization.currentLanguage.name?.startsWith('ar') ?? false;
    }

    viewReport(record: RunnablePrintReport): void {
        if (!record.isAvailable || !record.reportName) {
            return;
        }

        this._router.navigate(['/app/admin/print-reports-launcher/view', record.reportName]);
    }

    editReport(record: RunnablePrintReport): void {
        if (!record.isAvailable || !record.reportName) {
            return;
        }

        this._router.navigate(['/app/admin/report-designer-devexpress', record.reportName]);
    }
}
