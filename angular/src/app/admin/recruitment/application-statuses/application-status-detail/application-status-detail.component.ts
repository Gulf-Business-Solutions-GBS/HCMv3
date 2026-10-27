import { Component, OnInit, computed, inject, signal, viewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    ApplicationStatusSummaryDto,
    ApplicationStatusesServiceProxy,
    Gender,
} from '@shared/service-proxies/service-proxies';
import { finalize } from 'rxjs/operators';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import { CreateOrEditApplicationStatusModalComponent } from '../create-or-edit-application-status-modal.component';
import { InterviewDetailsComponent } from '../../interview-details/interview-details.component';
import { ApplicationStatusHistoryTabComponent } from './application-status-history-tab.component';
import { APP_STATUS_KEYS } from './app-status-names';

interface RecordTab {
    /** Goes in the URL fragment (`/application-statuses/12#interviews`), so a refresh keeps the tab. */
    id: string;
    labelKey: string;
    permission: string;
}

const APPLICATION_STATUS_TABS: RecordTab[] = [
    { id: 'applicant', labelKey: 'ApplicantSummary', permission: 'Pages.Recruitment.ApplicationStatuses' },
    { id: 'interviews', labelKey: 'InterviewDetails', permission: 'Pages.Recruitment.InterviewDetails' },
    { id: 'history', labelKey: 'StatusHistory', permission: 'Pages.Recruitment.ApplicationStatuses' },
];

/**
 * One application (an applicant's CV against a vacancy): status and vacancy at the top, Edit opens the usual
 * dialog, and below it the applicant's summary, the interviews held for this application, and how its status
 * moved over time.
 */
@Component({
    templateUrl: './application-status-detail.component.html',
    styleUrl: '../../../../shared/common/detail-tabs/detail-tabs.css',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        BusyIfDirective,
        CreateOrEditApplicationStatusModalComponent,
        InterviewDetailsComponent,
        ApplicationStatusHistoryTabComponent,
        LocalizePipe,
        LuxonFormatPipe,
    ],
})
export class ApplicationStatusDetailComponent extends AppComponentBase implements OnInit {
    private _route = inject(ActivatedRoute);
    private _router = inject(Router);
    private _applicationStatusesService = inject(ApplicationStatusesServiceProxy);

    readonly editModal = viewChild<CreateOrEditApplicationStatusModalComponent>('editModal');

    readonly applicationStatusId = signal<number | undefined>(undefined);
    readonly summary = signal<ApplicationStatusSummaryDto | undefined>(undefined);
    readonly loading = signal<boolean>(false);
    /** Bumped after a save so the Status History tab reads the change just made. */
    readonly historyRefresh = signal<number>(0);

    readonly isArabic = abp.localization.currentLanguage.name?.startsWith('ar') ?? false;

    readonly applicantName = computed(() => {
        const summary = this.summary();
        return this.pick(summary?.applicantFullNameEn, summary?.applicantFullNameAr);
    });

    readonly title = computed(() => this.applicantName() || this.l('ApplicationStatus'));

    readonly canEdit = this.permission.isGranted('Pages.Recruitment.ApplicationStatuses.Edit');

    readonly tabs = APPLICATION_STATUS_TABS.filter((tab) => this.permission.isGranted(tab.permission));
    readonly activeTabId = signal<string | undefined>(this.tabs[0]?.id);

    ngOnInit(): void {
        this._route.paramMap.subscribe((params) => {
            const id = Number(params.get('id'));
            if (id) {
                this.applicationStatusId.set(id);
                this.load(id);
            }
        });

        this._route.fragment.subscribe((fragment) => {
            const requested = this.tabs.find((tab) => tab.id === fragment);
            this.activeTabId.set((requested ?? this.tabs[0])?.id);
        });
    }

    load(id: number): void {
        this.loading.set(true);
        this._applicationStatusesService
            .getApplicationStatusSummary(id)
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe((result) => this.summary.set(result));
    }

    selectTab(tab: RecordTab): void {
        if (this.activeTabId() === tab.id) {
            return;
        }
        this.activeTabId.set(tab.id);
        this._router.navigate([], { relativeTo: this._route, fragment: tab.id, replaceUrl: true });
    }

    edit(): void {
        if (this.canEdit && this.applicationStatusId()) {
            this.editModal().show(this.applicationStatusId());
        }
    }

    onSaved(): void {
        if (this.applicationStatusId()) {
            this.load(this.applicationStatusId());
            this.historyRefresh.update((n) => n + 1);
        }
    }

    goBack(): void {
        this._router.navigate(['/app/admin/application-statuses']);
    }

    statusKey(value: number | undefined): string {
        return value === undefined || value === null ? '' : (APP_STATUS_KEYS[value] ?? String(value));
    }

    genderKey(value: Gender | undefined): string {
        switch (value) {
            case Gender.Male:
                return 'Male';
            case Gender.Female:
                return 'Female';
            default:
                return '';
        }
    }

    pick(en: string | undefined, ar: string | undefined): string {
        return this.isArabic ? ar || en || '' : en || '';
    }
}
