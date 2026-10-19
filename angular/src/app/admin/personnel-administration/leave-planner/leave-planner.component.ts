import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import {
    DashboardScopeReason,
    GetLeavePlannerInput,
    LeavePlannerDto,
    LeavePlannerServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { DateTime } from 'luxon';
import { finalize } from 'rxjs/operators';
import { SubHeaderComponent } from '../../../shared/common/sub-header/sub-header.component';
import { LeavePlannerGridComponent } from '../../../shared/common/leave-planner/leave-planner-grid.component';
import { BusyIfDirective } from '../../../../shared/utils/busy-if.directive';
import { scopeLabel } from '../../dashboards/shared/dashboard-shared';

/**
 * The Leave Planner (legacy <c>HCM10.Web/UserControls/Reports/Analysis/Transaction/uscLeavePlanner.ascx</c>): who
 * plans to be away, and who is actually away, by organization unit for one month.
 *
 * <p>Legacy's filters were a Department combo, an Employee combo and four check boxes ("Show Actual Leave
 * Approved", "... Unapproved", "Show Leave Planner Approved", "... Unapproved"). All four switches are here with
 * their legacy captions. The department becomes the organization unit (with the units below it), and the employee
 * combo a code-or-name filter. Whose people are shown follows the same rule as every dashboard -- see the
 * "Showing" line.</p>
 */
import { HelpPageDirective } from '@app/shared/help/help-page.directive';

@Component({
    templateUrl: './leave-planner.component.html',
    animations: [appModuleAnimation()],
    imports: [
        HelpPageDirective,
        SubHeaderComponent, BusyIfDirective, FormsModule, LocalizePipe, LeavePlannerGridComponent],
})
export class LeavePlannerComponent extends AppComponentBase implements OnInit {
    private readonly _service = inject(LeavePlannerServiceProxy);

    readonly month = signal(DateTime.local().startOf('month'));
    readonly planner = signal<LeavePlannerDto | undefined>(undefined);
    readonly loading = signal(false);

    organizationUnitId: number | undefined;
    filterText = '';

    // Legacy defaults: only "Show Actual Leave Approved" was ticked. Approved plans are ticked here too -- a
    // planner that opens showing no plans reads as broken.
    includePlanned = true;
    includePlannedPending = false;
    includeLeave = true;
    includeLeavePending = false;

    readonly scopeText = computed(() => scopeLabel(this.planner()?.scope, (key) => this.l(key)));
    readonly scopeIsNobody = computed(() => this.planner()?.scope?.reason === DashboardScopeReason.Nobody);

    ngOnInit(): void {
        this.load();
    }

    moveMonth(months: number): void {
        this.month.set(this.month().plus({ months }));
        this.load();
    }

    load(): void {
        this.loading.set(true);

        this._service
            .getLeavePlanner(
                new GetLeavePlannerInput({
                    // Mid-month, so no time-zone conversion on the way can move it into the next or previous month.
                    month: this.month().plus({ days: 14 }),
                    organizationUnitId: this.organizationUnitId,
                    filter: this.filterText?.trim() || undefined,
                    includePlanned: this.includePlanned,
                    includePlannedPending: this.includePlannedPending,
                    includeLeave: this.includeLeave,
                    includeLeavePending: this.includeLeavePending,
                })
            )
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe((result) => this.planner.set(result));
    }

    indent(depth: number): string {
        return '   '.repeat(depth);
    }
}
