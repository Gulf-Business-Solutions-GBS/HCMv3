import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs/operators';

import { CareersApplicationDto, CareersCandidateServiceProxy } from '@proxies/service-proxies';
import { LocalizePipe } from '@core/localize.pipe';
import { LuxonDatePipe } from '@core/luxon-date.pipe';
import { ToastService } from '@core/toast.service';
import { APP_STATUS_LABELS, APP_STATUS_TONE } from '@core/enum-labels';
import { CandidateNavComponent } from './candidate-nav.component';

@Component({
    selector: 'careers-applications',
    imports: [RouterLink, LocalizePipe, LuxonDatePipe, CandidateNavComponent],
    templateUrl: './applications.component.html',
    styleUrl: './applications.component.css',
})
export class ApplicationsComponent implements OnInit {
    private _candidateService = inject(CareersCandidateServiceProxy);
    private _toast = inject(ToastService);

    readonly loading = signal(true);
    readonly applications = signal<CareersApplicationDto[]>([]);

    ngOnInit(): void {
        this._candidateService
            .getMyApplications()
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe({
                next: (result) => this.applications.set(result.items ?? []),
                error: (error) => this._toast.apiError(error),
            });
    }

    /**
     * Status is READ-ONLY here by design: a candidate can never write `AppStatus`; only the recruiter's
     * `ApplicationStatusesAppService` moves an application along.
     */
    statusLabelKey(application: CareersApplicationDto): string {
        return APP_STATUS_LABELS[application.appStatus] ?? 'CareersNotSpecified';
    }

    statusTone(application: CareersApplicationDto): string {
        return APP_STATUS_TONE[application.appStatus] ?? 'neutral';
    }
}
