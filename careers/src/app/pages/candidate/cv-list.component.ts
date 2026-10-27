import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs/operators';

import { CareersCandidateServiceProxy, CareersCvListDto } from '@proxies/service-proxies';
import { LocalizePipe } from '@core/localize.pipe';
import { LocalizationService } from '@core/localization.service';
import { LuxonDatePipe } from '@core/luxon-date.pipe';
import { ToastService } from '@core/toast.service';
import { CandidateNavComponent } from './candidate-nav.component';

@Component({
    selector: 'careers-cv-list',
    imports: [RouterLink, LocalizePipe, LuxonDatePipe, CandidateNavComponent],
    templateUrl: './cv-list.component.html',
    styleUrl: './cv-list.component.css',
})
export class CvListComponent implements OnInit {
    private _candidateService = inject(CareersCandidateServiceProxy);
    private _toast = inject(ToastService);
    private _localization = inject(LocalizationService);

    readonly loading = signal(true);
    readonly cvs = signal<CareersCvListDto[]>([]);
    readonly deletingId = signal<number | null>(null);

    ngOnInit(): void {
        this.load();
    }

    load(): void {
        this.loading.set(true);

        this._candidateService
            .getMyCvs()
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe({
                next: (result) => this.cvs.set(result.items ?? []),
                error: (error) => this._toast.apiError(error),
            });
    }

    delete(cv: CareersCvListDto): void {
        // `CanBeDeleted` mirrors the server's own rule so the button can be hidden rather than offered and
        // then refused; the server still enforces it.
        if (!cv.canBeDeleted || this.deletingId()) {
            return;
        }

        if (!confirm(this._localization.localize('CareersConfirmDeleteCv', cv.cvName))) {
            return;
        }

        this.deletingId.set(cv.id);

        // NSwag flattens `EntityDto` for DELETE, so this takes the raw id -- NOT `{ id }`.
        this._candidateService
            .deleteMyCv(cv.id)
            .pipe(finalize(() => this.deletingId.set(null)))
            .subscribe({
                next: () => {
                    this._toast.success(this._localization.localize('CareersCvDeleted'));
                    this.load();
                },
                error: (error) => this._toast.apiError(error),
            });
    }
}
