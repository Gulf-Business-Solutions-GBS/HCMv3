import { ChangeDetectionStrategy, Component, OnInit, inject, signal, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import {
    GetMyAppraisalsInput,
    MyAppraisalListDto,
    MyAppraisalListScope,
    MyAppraisalServiceProxy,
    RequestStatus,
} from '@shared/service-proxies/service-proxies';
import { finalize } from 'rxjs/operators';
import { SubHeaderComponent } from '../../../shared/common/sub-header/sub-header.component';
import { EssList } from '../shared/ess-list';
import { EssListHeadComponent } from '../shared/ess-list-head.component';
import { EssListPagerComponent } from '../shared/ess-list-pager.component';
import { AppraisalReviewModalComponent } from './appraisal-review-modal.component';
import { HelpPageDirective } from '@app/shared/help/help-page.directive';

/**
 * Self-service 360 appraisals (legacy ESS "360 Degree Appraisal"): three lists -- my own appraisals, the
 * ones I review or approve now, and my team's -- each row opening the appraisal in its Details dialog,
 * where the review is done.
 *
 * <p>The Exit Permit layout, less what does not apply: an appraisal is opened FOR an employee by HR, so
 * there is no create form and nothing to withdraw. Bulk launch for an organization unit stays on the admin
 * screen (owner's decision). One status per row, the standard set.</p>
 */
@Component({
    templateUrl: './my-appraisal.component.html',
    animations: [appModuleAnimation()],
    imports: [
        CommonModule,
        FormsModule,
        SubHeaderComponent,
        LocalizePipe,
        LuxonFormatPipe,
        EssListHeadComponent,
        EssListPagerComponent,
        AppraisalReviewModalComponent,
        HelpPageDirective,
    ],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyAppraisalComponent extends AppComponentBase implements OnInit {
    private readonly _service = inject(MyAppraisalServiceProxy);

    readonly review = viewChild<AppraisalReviewModalComponent>('review');

    readonly scopes = [MyAppraisalListScope.Mine, MyAppraisalListScope.ToReview, MyAppraisalListScope.Team];
    readonly scope = signal<MyAppraisalListScope>(MyAppraisalListScope.Mine);

    readonly loading = signal(false);
    readonly notLinked = signal(false);
    readonly rows = signal<MyAppraisalListDto[]>([]);

    readonly list = new EssList<MyAppraisalListDto>(this.rows, () => [
        { key: 'number', label: 'RequestNumber', text: (row) => row.requestNumber },
        { key: 'employee', label: 'Employee', text: (row) => this.employeeLabel(row) },
        {
            key: 'period',
            label: 'Period',
            text: (row) => this.pick(row.paCycleNameEn, row.paCycleNameAr),
            filter: 'select',
        },
        {
            key: 'formType',
            label: 'FormType',
            text: (row) => this.pick(row.formTypeNameEn, row.formTypeNameAr),
            filter: 'select',
        },
        {
            key: 'createdOn',
            label: 'CreatedOn',
            text: (row) => row.creationTime?.toFormat('yyyy-MM-dd'),
            sort: (row) => row.creationTime?.toMillis(),
        },
        { key: 'status', label: 'RequestStatus', text: (row) => this.statusLabel(row), filter: 'select' },
    ]);

    ngOnInit(): void {
        this.reload();
    }

    scopeKey(scope: MyAppraisalListScope): string {
        return 'MyAppraisalListScope_' + MyAppraisalListScope[scope];
    }

    scopeLabel(scope: MyAppraisalListScope): string {
        return this.l(this.scopeKey(scope));
    }

    selectScope(scope: MyAppraisalListScope): void {
        if (this.scope() === scope) {
            return;
        }

        this.scope.set(scope);
        this.list.clearFilters();
        this.reload();
    }

    reload(): void {
        this.loading.set(true);

        this._service
            .getMyAppraisals(
                new GetMyAppraisalsInput({ scope: this.scope(), maxResultCount: 200, skipCount: 0, sorting: undefined })
            )
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe({
                next: (result) => this.rows.set(result.items ?? []),
                // An unlinked account is a setup gap for HR, explained on the page rather than as an error.
                error: () => this.notLinked.set(true),
            });
    }

    openDetail(row: MyAppraisalListDto): void {
        this.review()?.show(
            { paEmployeeId: row.id, employeeDisplay: this.employeeLabel(row) },
            this.l('EssMyAppraisals')
        );
    }

    employeeLabel(row: MyAppraisalListDto): string {
        return [row.employeeCode, this.pick(row.employeeFullNameEn, row.employeeFullNameAr)]
            .filter((p) => !!p)
            .join(' - ');
    }

    statusLabel(row: MyAppraisalListDto): string {
        if (row.workflowStatus === undefined || row.workflowStatus === null) {
            return this.l('Recorded');
        }

        return this.l('WorkflowRequestStatus_' + RequestStatus[row.workflowStatus]);
    }

    statusClass(row: MyAppraisalListDto): string {
        switch (row.workflowStatus) {
            case RequestStatus.Open:
                return 'badge-warning';
            case RequestStatus.Approved:
            case RequestStatus.Closed:
                return 'badge-success';
            case RequestStatus.Rejected:
                return 'badge-danger';
            case RequestStatus.Draft:
                return 'badge-light-warning';
            default:
                return 'badge-secondary';
        }
    }

    pick(en: string | undefined, ar: string | undefined): string {
        const rtl = document?.documentElement?.getAttribute('dir') === 'rtl';
        return rtl ? ar || en || '' : en || '';
    }
}
