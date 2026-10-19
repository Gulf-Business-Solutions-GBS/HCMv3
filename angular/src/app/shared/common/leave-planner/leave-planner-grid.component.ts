import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import {
    LeavePlannerDayFlags,
    LeavePlannerDto,
    LeavePlannerEntryDto,
    LeavePlannerEntryKind,
    LeavePlannerRowDto,
} from '@shared/service-proxies/service-proxies';
import { Tooltip } from 'primeng/tooltip';

/** One column of the grid: a day of the month. */
interface PlannerDay {
    index: number;
    label: string;
    weekday: string;
    isWeekend: boolean;
}

/**
 * The Leave Planner's grid: employees down, the days of one month across, grouped by organization unit, with the
 * number away each day on the unit's own line. Shared by HR's planner and the self-service team scheduler, so the
 * two read the same.
 *
 * <p>Legacy drew this with DevExpress's <c>ASPxScheduler</c> (HR) and Syncfusion's scheduler (ESS). HCMv3 has no
 * scheduler library, and the roster schedule viewer already renders legacy's other scheduler as a plain
 * employee-by-day matrix; this follows that screen rather than adding a dependency.</p>
 *
 * <p>A cell can hold a plan and a leave at once and shows both: the stripe is the plan, the fill is the leave.
 * Pending (still with an approver) is drawn lighter than approved.</p>
 */
@Component({
    selector: 'leave-planner-grid',
    templateUrl: './leave-planner-grid.component.html',
    styleUrls: ['./leave-planner-grid.component.less'],
    imports: [CommonModule, LocalizePipe, Tooltip],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LeavePlannerGridComponent extends AppComponentBase {
    readonly planner = input<LeavePlannerDto | undefined>(undefined);

    readonly days = computed<PlannerDay[]>(() => {
        const planner = this.planner();
        if (!planner?.fromDate || !planner?.toDate) {
            return [];
        }

        const result: PlannerDay[] = [];
        let day = planner.fromDate.startOf('day');
        const end = planner.toDate.startOf('day');
        let index = 0;

        while (day <= end && index < 31) {
            result.push({
                index,
                label: day.toFormat('d'),
                weekday: day.toFormat('ccc'),
                // Friday and Saturday: the working week of the region this product is built for. Only shading --
                // the planner counts calendar days, as legacy's did.
                isWeekend: day.weekday === 5 || day.weekday === 6,
            });
            day = day.plus({ days: 1 });
            index++;
        }

        return result;
    });

    readonly isEmpty = computed(() => (this.planner()?.groups ?? []).every((g) => !g.rows?.length));

    has(flags: LeavePlannerDayFlags | undefined, flag: LeavePlannerDayFlags): boolean {
        // eslint-disable-next-line no-bitwise
        return ((flags ?? 0) & flag) === flag;
    }

    cellClass(flags: LeavePlannerDayFlags | undefined): Record<string, boolean> {
        return {
            'lp-leave': this.has(flags, LeavePlannerDayFlags.LeaveApproved),
            'lp-leave-pending': this.has(flags, LeavePlannerDayFlags.LeavePending) && !this.has(flags, LeavePlannerDayFlags.LeaveApproved),
            'lp-plan': this.has(flags, LeavePlannerDayFlags.PlannedApproved),
            'lp-plan-pending':
                this.has(flags, LeavePlannerDayFlags.PlannedPending) && !this.has(flags, LeavePlannerDayFlags.PlannedApproved),
        };
    }

    /** What is on one employee's day, for the tooltip. */
    cellTooltip(row: LeavePlannerRowDto, dayIndex: number): string {
        const planner = this.planner();
        if (!planner?.fromDate || !row.days?.[dayIndex]) {
            return '';
        }

        const date = planner.fromDate.startOf('day').plus({ days: dayIndex });

        return (row.entries ?? [])
            .filter((e) => e.startDate.startOf('day') <= date && e.endDate.startOf('day') >= date)
            .map((e) => this.entryLabel(e))
            .join('\n');
    }

    employeeName(row: LeavePlannerRowDto): string {
        const name = this.isRtl ? row.employeeFullNameAr || row.employeeFullNameEn : row.employeeFullNameEn;
        return [row.employeeCode, name].filter((p) => !!p).join(' - ');
    }

    private entryLabel(entry: LeavePlannerEntryDto): string {
        const range = `${entry.startDate.toFormat('dd/MM/yyyy')} – ${entry.endDate.toFormat('dd/MM/yyyy')}`;
        const type = this.isRtl ? entry.leaveTypeNameAr || entry.leaveTypeNameEn : entry.leaveTypeNameEn;

        switch (entry.kind) {
            case LeavePlannerEntryKind.PlannedApproved:
                return `${this.l('LeavePlanner_Planned')}: ${range}`;
            case LeavePlannerEntryKind.PlannedPending:
                return `${this.l('LeavePlanner_PlannedPending')}: ${range}`;
            case LeavePlannerEntryKind.LeaveApproved:
                return `${type || this.l('LeavePlanner_Leave')}: ${range}`;
            case LeavePlannerEntryKind.LeavePending:
                return `${type || this.l('LeavePlanner_LeavePending')} (${this.l('WorkflowRequestStatus_Open')}): ${range}`;
            default:
                return range;
        }
    }

    private get isRtl(): boolean {
        return document?.documentElement?.getAttribute('dir') === 'rtl';
    }
}
