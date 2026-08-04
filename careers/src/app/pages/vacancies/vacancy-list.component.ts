import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs/operators';

import {
    CareersServiceProxy,
    GetPublishedVacanciesInput,
    PublicVacancyFilterOptionDto,
    PublicVacancyListDto,
} from '@proxies/service-proxies';
import { BrandingService } from '@core/branding.service';
import { LocalizePipe } from '@core/localize.pipe';
import { LocalizationService } from '@core/localization.service';
import { LuxonDatePipe } from '@core/luxon-date.pipe';
import { ToastService } from '@core/toast.service';
import { EMPLOYMENT_TYPE_LABELS } from '@core/enum-labels';

/** The sort expressions the backend whitelists for anonymous callers (see `GetPublishedVacanciesInput`). */
const SORT_OPTIONS = [
    { value: 'PublishOn DESC', labelKey: 'CareersSortNewest' },
    { value: 'ClosingDate ASC', labelKey: 'CareersSortClosingSoon' },
    { value: 'VacancyTitle ASC', labelKey: 'CareersSortTitle' },
];

const PAGE_SIZE = 10;

@Component({
    selector: 'careers-vacancy-list',
    imports: [FormsModule, RouterLink, LocalizePipe, LuxonDatePipe],
    templateUrl: './vacancy-list.component.html',
    styleUrl: './vacancy-list.component.css',
})
export class VacancyListComponent implements OnInit {
    private _careersService = inject(CareersServiceProxy);
    private _toast = inject(ToastService);
    private _localization = inject(LocalizationService);
    readonly branding = inject(BrandingService);

    readonly sortOptions = SORT_OPTIONS;

    readonly loading = signal(true);
    readonly vacancies = signal<PublicVacancyListDto[]>([]);
    readonly totalCount = signal(0);

    readonly jobTitleFacets = signal<PublicVacancyFilterOptionDto[]>([]);
    readonly workLocationFacets = signal<PublicVacancyFilterOptionDto[]>([]);

    /** Bound to the search box; only applied to the query when the user submits. */
    readonly searchText = signal('');
    readonly appliedFilter = signal('');
    readonly selectedJobTitleIds = signal<number[]>([]);
    readonly selectedWorkLocationIds = signal<number[]>([]);
    readonly sorting = signal(SORT_OPTIONS[0].value);
    readonly page = signal(0);

    readonly filtersOpen = signal(false);

    readonly totalPages = computed(() => Math.ceil(this.totalCount() / PAGE_SIZE));

    readonly hasActiveFilters = computed(
        () =>
            !!this.appliedFilter() ||
            this.selectedJobTitleIds().length > 0 ||
            this.selectedWorkLocationIds().length > 0
    );

    readonly heroTitle = computed(() =>
        this._localization.localize('CareersHeroTitle', this.branding.displayName())
    );

    readonly resultLabel = computed(() =>
        this._localization.localize('CareersResultCount', this.totalCount())
    );

    ngOnInit(): void {
        this.loadFacets();
        this.loadVacancies();
    }

    employmentTypeKey(vacancy: PublicVacancyListDto): string {
        return EMPLOYMENT_TYPE_LABELS[vacancy.employmentType] ?? 'CareersNotSpecified';
    }

    private loadFacets(): void {
        // Facets are derived server-side from the currently-published vacancies, so an option never points
        // at a job title or location with nothing to show.
        this._careersService.getVacancyFilters().subscribe({
            next: (result) => {
                this.jobTitleFacets.set(result.jobTitles ?? []);
                this.workLocationFacets.set(result.workLocations ?? []);
            },
            error: () => {
                // A missing facet list must not stop the job list from rendering; it is a refinement, not
                // the content. The list call reports any real outage.
                this.jobTitleFacets.set([]);
                this.workLocationFacets.set([]);
            },
        });
    }

    loadVacancies(): void {
        this.loading.set(true);

        const input = new GetPublishedVacanciesInput({
            filter: this.appliedFilter() || undefined,
            jobTitleIds: this.selectedJobTitleIds().length ? this.selectedJobTitleIds() : undefined,
            workLocationIds: this.selectedWorkLocationIds().length ? this.selectedWorkLocationIds() : undefined,
            sorting: this.sorting(),
            maxResultCount: PAGE_SIZE,
            skipCount: this.page() * PAGE_SIZE,
        });

        this._careersService
            .getPublishedVacancies(input)
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe({
                next: (result) => {
                    this.vacancies.set(result.items ?? []);
                    this.totalCount.set(result.totalCount ?? 0);
                },
                error: (error) => {
                    this.vacancies.set([]);
                    this.totalCount.set(0);
                    this._toast.apiError(error);
                },
            });
    }

    search(): void {
        this.appliedFilter.set(this.searchText().trim());
        this.page.set(0);
        this.loadVacancies();
    }

    toggleJobTitle(id: number): void {
        this.selectedJobTitleIds.update((ids) =>
            ids.includes(id) ? ids.filter((x) => x !== id) : [...ids, id]
        );
        this.page.set(0);
        this.loadVacancies();
    }

    toggleWorkLocation(id: number): void {
        this.selectedWorkLocationIds.update((ids) =>
            ids.includes(id) ? ids.filter((x) => x !== id) : [...ids, id]
        );
        this.page.set(0);
        this.loadVacancies();
    }

    changeSorting(value: string): void {
        this.sorting.set(value);
        this.page.set(0);
        this.loadVacancies();
    }

    clearFilters(): void {
        this.searchText.set('');
        this.appliedFilter.set('');
        this.selectedJobTitleIds.set([]);
        this.selectedWorkLocationIds.set([]);
        this.page.set(0);
        this.loadVacancies();
    }

    goToPage(page: number): void {
        if (page < 0 || page >= this.totalPages() || page === this.page()) {
            return;
        }

        this.page.set(page);
        this.loadVacancies();
        window.scrollTo({ top: 0, behavior: 'smooth' });
    }

    toggleFilters(): void {
        this.filtersOpen.update((open) => !open);
    }
}
