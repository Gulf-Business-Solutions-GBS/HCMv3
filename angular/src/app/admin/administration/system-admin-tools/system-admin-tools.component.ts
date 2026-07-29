import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DateTime } from 'luxon';
import { finalize } from 'rxjs/operators';

import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import {
    AdminToolResultDto,
    FetchMissingTimeAttendanceDataInput,
    SystemAdminToolsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';

/**
 * Port of legacy HCM10.Web/UserControls/Administration/uscSystemConfigPanel.ascx.
 *
 * Only the three panels that survived the port are here; "Reset Model Navigation Items Nodes" and
 * "Migrate Attachments" were deliberately closed out server side (see ISystemAdminToolsAppService).
 */
@Component({
    templateUrl: './system-admin-tools.component.html',
    animations: [appModuleAnimation()],
    imports: [SubHeaderComponent, BusyIfDirective, FormsModule, LocalizePipe, PermissionPipe],
})
export class SystemAdminToolsComponent extends AppComponentBase {
    private _systemAdminToolsService = inject(SystemAdminToolsServiceProxy);

    // ---- Fetch missing time attendance data (legacy line 844) --------------------------------------
    readonly fetchFromDate = signal<string>('');
    readonly fetchEmployeeCodes = signal<string>('');
    readonly fetchConnectionString = signal<string>('');

    // ---- Sync missing time attendance API data (legacy line 899) -----------------------------------
    readonly syncFromDate = signal<string>('');
    readonly syncEmployeeCodes = signal<string>('');

    readonly running = signal<string>('');
    readonly resultTitle = signal<string>('');
    readonly result = signal<AdminToolResultDto | undefined>(undefined);
    readonly errorMessage = signal<string>('');

    parseCodes(raw: string): string[] {
        return (raw || '')
            .split(',')
            .map((c) => c.trim())
            .filter((c) => !!c);
    }

    scopeText(raw: string): string {
        const codes = this.parseCodes(raw);
        return codes.length === 0
            ? this.l('ScopeAllActiveEmployees')
            : this.l('ScopeSelectedEmployeeCodes', codes.length, codes.join(', '));
    }

    /**
     * Legacy line 795. Overwrites Employee.ProbationEndDate for every active employee.
     *
     * The app service throws a UserFriendlyException when the tenant is configured for working-days
     * probation calculation; that message is rendered into the page's error panel rather than swallowed.
     */
    recalculateProbationEndDates(): void {
        this.message.confirm(this.l('ConfirmRecalculateProbationEndDates'), this.l('AreYouSure'), (isConfirmed) => {
            if (!isConfirmed) {
                return;
            }

            this.start('probation', this.l('RecalculateProbationEndDates'));
            this._systemAdminToolsService
                .recalculateProbationEndDates()
                .pipe(finalize(() => this.running.set('')))
                .subscribe({
                    next: (r) => this.succeeded(r),
                    error: (e) => this.failed(e),
                });
        });
    }

    /** Legacy line 844. */
    fetchMissingData(): void {
        if (!this.fetchConnectionString()) {
            this.message.warn(this.l('AccessControlConnectionStringIsRequired'));
            return;
        }

        this.message.confirm(
            this.l(
                'ConfirmFetchMissingTimeAttendanceData',
                this.fetchFromDate() || this.l('EarliestEvaluationDateDefault'),
                this.scopeText(this.fetchEmployeeCodes())
            ),
            this.l('AreYouSure'),
            (isConfirmed) => {
                if (!isConfirmed) {
                    return;
                }

                this.start('fetch', this.l('FetchMissingTimeAttendanceData'));
                this._systemAdminToolsService
                    .fetchMissingTimeAttendanceData(
                        new FetchMissingTimeAttendanceDataInput({
                            fromDate: this.fetchFromDate() ? DateTime.fromISO(this.fetchFromDate()) : undefined,
                            employeeCodes: this.parseCodes(this.fetchEmployeeCodes()),
                            accessControlConnectionString: this.fetchConnectionString(),
                        })
                    )
                    .pipe(finalize(() => this.running.set('')))
                    .subscribe({
                        next: (r) => this.succeeded(r),
                        error: (e) => this.failed(e),
                    });
            }
        );
    }

    /** Legacy line 899. Does not need the access-control connection string. */
    syncMissingApiData(): void {
        this.message.confirm(
            this.l(
                'ConfirmSyncMissingTimeAttendanceApiData',
                this.syncFromDate() || this.l('EarliestEvaluationDateDefault'),
                this.scopeText(this.syncEmployeeCodes())
            ),
            this.l('AreYouSure'),
            (isConfirmed) => {
                if (!isConfirmed) {
                    return;
                }

                this.start('sync', this.l('SyncMissingTimeAttendanceApiData'));
                this._systemAdminToolsService
                    .syncMissingTimeAttendanceApiData(
                        new FetchMissingTimeAttendanceDataInput({
                            fromDate: this.syncFromDate() ? DateTime.fromISO(this.syncFromDate()) : undefined,
                            employeeCodes: this.parseCodes(this.syncEmployeeCodes()),
                            accessControlConnectionString: undefined,
                        })
                    )
                    .pipe(finalize(() => this.running.set('')))
                    .subscribe({
                        next: (r) => this.succeeded(r),
                        error: (e) => this.failed(e),
                    });
            }
        );
    }

    private start(key: string, title: string): void {
        this.running.set(key);
        this.resultTitle.set(title);
        this.result.set(undefined);
        this.errorMessage.set('');
    }

    private succeeded(result: AdminToolResultDto): void {
        this.result.set(result);
        this.notify.success(this.l('AdminToolResultSummary', result.affectedCount, result.skippedCount));
    }

    private failed(error: any): void {
        this.errorMessage.set(error?.message || error?.details || '');
    }
}
