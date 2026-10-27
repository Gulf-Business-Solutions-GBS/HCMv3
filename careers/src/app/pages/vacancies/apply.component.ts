import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { finalize } from 'rxjs/operators';

import {
    CareersApplyInput,
    CareersCandidateServiceProxy,
    CareersCvListDto,
    CareersServiceProxy,
    PublicVacancyDetailDto,
} from '@proxies/service-proxies';
import { LocalizePipe } from '@core/localize.pipe';
import { LocalizationService } from '@core/localization.service';
import { ToastService } from '@core/toast.service';

/**
 * Apply to a vacancy. Ported from legacy `VacancyController.QuickApplyForJob`.
 *
 * The input carries a vacancy id and OPTIONALLY one of the caller's own CV ids -- never an applicant id;
 * the applicant is resolved server-side from `AbpSession.UserId`. Leaving the CV unselected is a supported
 * path: the server then creates a placeholder CV, which is exactly what legacy quick-apply did for a
 * first-time applicant.
 */
@Component({
    selector: 'careers-apply',
    imports: [FormsModule, RouterLink, LocalizePipe],
    templateUrl: './apply.component.html',
    styleUrl: './apply.component.css',
})
export class ApplyComponent implements OnInit {
    private _careersService = inject(CareersServiceProxy);
    private _candidateService = inject(CareersCandidateServiceProxy);
    private _route = inject(ActivatedRoute);
    private _router = inject(Router);
    private _toast = inject(ToastService);
    private _localization = inject(LocalizationService);

    readonly vacancyId = Number(this._route.snapshot.paramMap.get('id'));

    readonly loading = signal(true);
    readonly submitting = signal(false);
    readonly submitted = signal(false);

    readonly vacancy = signal<PublicVacancyDetailDto | null>(null);
    readonly cvs = signal<CareersCvListDto[]>([]);

    /** Empty string means "let the server create a placeholder CV". */
    readonly selectedCvId = signal<string>('');
    readonly coverLetter = signal('');

    readonly title = computed(() =>
        this._localization.localize('CareersApplyTitle', this.vacancy()?.vacancyTitle ?? '')
    );

    ngOnInit(): void {
        if (!this.vacancyId) {
            void this._router.navigate(['/']);
            return;
        }

        forkJoin({
            // NSwag flattens EntityDto for GET: raw id, not `{ id }`.
            vacancy: this._careersService.getPublishedVacancy(this.vacancyId),
            cvs: this._candidateService.getMyCvs(),
        })
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe({
                next: (result) => {
                    this.vacancy.set(result.vacancy);
                    const items = result.cvs.items ?? [];
                    this.cvs.set(items);
                    if (items.length) {
                        this.selectedCvId.set(String(items[0].id));
                    }
                },
                error: (error) => this._toast.apiError(error),
            });
    }

    submit(): void {
        if (this.submitting() || this.submitted()) {
            return;
        }

        this.submitting.set(true);

        const applicantCvId = this.selectedCvId() ? Number(this.selectedCvId()) : undefined;

        this._candidateService
            .applyToVacancy(
                new CareersApplyInput({
                    vacancyId: this.vacancyId,
                    applicantCvId,
                    coverLetter: this.coverLetter().trim() || undefined,
                })
            )
            .pipe(finalize(() => this.submitting.set(false)))
            .subscribe({
                next: () => {
                    this.submitted.set(true);
                    this._toast.success(this._localization.localize('CareersApplicationSubmitted'));
                },
                // Covers "you have already applied to this job opening" and "that CV is not yours",
                // both of which the server reports as friendly messages.
                error: (error) => this._toast.apiError(error),
            });
    }
}
