import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';

import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import {
    GetAllPAPeriodsInput,
    GetPerformanceAppraisalDashboardInput,
    PAPeriodDto,
    PAPeriodsServiceProxy,
    PerformanceAppraisalDashboardDto,
    PerformanceAppraisalDashboardServiceProxy,
    PerformanceAppraisalStageStatusDto,
} from '@shared/service-proxies/service-proxies';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';

/**
 * Port of the legacy Performance Appraisal dashboard
 * (HCM10.Web/UserControls/PerformanceAppraisal/uscPerformanceAppraisal.ascx + .ascx.cs).
 *
 * Legacy drew a DevExpress full-stacked bar chart with three series (Completed / Pending by Appraiser /
 * Pending by Appraisee) across three stages (PLAN / MID-YEAR / END-YEAR). The DevExpress chart plumbing does
 * not port; the same three figures per stage are rendered here as stacked progress bars, which needs no
 * charting dependency.
 *
 * NOTE, matching the backend: "Pending by Appraiser" is a SUBTRACTION from the employee headcount, so it
 * counts employees with no appraisal form at all and can legitimately be negative. See
 * PerformanceAppraisalDashboardAppService for why that is preserved.
 */
@Component({
    templateUrl: './performance-appraisal-dashboard.component.html',
    animations: [appModuleAnimation()],
    imports: [SubHeaderComponent, BusyIfDirective, FormsModule, LocalizePipe],
})
export class PerformanceAppraisalDashboardComponent extends AppComponentBase implements OnInit {
    private _dashboardService = inject(PerformanceAppraisalDashboardServiceProxy);
    private _paPeriodsService = inject(PAPeriodsServiceProxy);

    readonly periods = signal<PAPeriodDto[]>([]);
    readonly selectedPeriodId = signal<number | undefined>(undefined);
    readonly dashboard = signal<PerformanceAppraisalDashboardDto | undefined>(undefined);
    readonly loading = signal<boolean>(false);

    ngOnInit(): void {
        // Legacy BindPAPeriod (lines 82-91).
        this._paPeriodsService
            .getAll(
                new GetAllPAPeriodsInput({
                    filter: undefined,
                    sorting: undefined,
                    maxResultCount: 1000,
                    skipCount: 0,
                })
            )
            .subscribe((result) => this.periods.set(result.items));
    }

    /** Legacy btnShow_Chart -> BindChart (lines 93-192). */
    show(): void {
        const paPeriodId = this.selectedPeriodId();
        if (!paPeriodId) {
            this.dashboard.set(undefined);
            return;
        }

        this.loading.set(true);
        this._dashboardService
            .getPerformanceAppraisalDashboard(new GetPerformanceAppraisalDashboardInput({ paPeriodId }))
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe((result) => this.dashboard.set(result));
    }

    stages(dashboard: PerformanceAppraisalDashboardDto): PerformanceAppraisalStageStatusDto[] {
        return [dashboard.plan, dashboard.midYearReview, dashboard.endYearReview];
    }

    /** Bar width as a percentage of the employee headcount; clamped to 0 so a negative figure is not drawn. */
    percent(value: number, total: number): number {
        if (!total || total <= 0) {
            return 0;
        }
        return Math.max(0, Math.round((value / total) * 100));
    }
}
