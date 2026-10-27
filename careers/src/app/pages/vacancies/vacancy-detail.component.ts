import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DateTime } from 'luxon';
import { finalize } from 'rxjs/operators';

import { CareersServiceProxy, PublicVacancyDetailDto } from '@proxies/service-proxies';
import { AuthService } from '@core/auth.service';
import { LocalizePipe } from '@core/localize.pipe';
import { LocalizationService } from '@core/localization.service';
import { LuxonDatePipe } from '@core/luxon-date.pipe';
import { ToastService } from '@core/toast.service';
import { EMPLOYMENT_TYPE_LABELS } from '@core/enum-labels';

@Component({
    selector: 'careers-vacancy-detail',
    imports: [RouterLink, LocalizePipe, LuxonDatePipe],
    templateUrl: './vacancy-detail.component.html',
    styleUrl: './vacancy-detail.component.css',
})
export class VacancyDetailComponent implements OnInit {
    private _careersService = inject(CareersServiceProxy);
    private _route = inject(ActivatedRoute);
    private _toast = inject(ToastService);
    private _localization = inject(LocalizationService);
    readonly auth = inject(AuthService);

    readonly loading = signal(true);
    readonly vacancy = signal<PublicVacancyDetailDto | null>(null);
    readonly notFound = signal(false);

    readonly employmentTypeKey = computed(() => {
        const vacancy = this.vacancy();
        return vacancy ? EMPLOYMENT_TYPE_LABELS[vacancy.employmentType] ?? 'CareersNotSpecified' : '';
    });

    readonly isClosed = computed(() => {
        const closingDate = this.vacancy()?.closingDate;
        return !!closingDate && closingDate < DateTime.now();
    });

    readonly applyTitle = computed(() =>
        this._localization.localize('CareersApplyTitle', this.vacancy()?.vacancyTitle ?? '')
    );

    ngOnInit(): void {
        const id = Number(this._route.snapshot.paramMap.get('id'));

        if (!id) {
            this.notFound.set(true);
            this.loading.set(false);
            return;
        }

        // NSwag flattens ABP's `EntityDto` for GET operations, so this takes the raw id -- NOT `{ id }`.
        this._careersService
            .getPublishedVacancy(id)
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe({
                next: (result) => this.vacancy.set(result),
                error: (error) => {
                    this.notFound.set(true);
                    this._toast.apiError(error);
                },
            });
    }
}
