import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { DateTime } from 'luxon';
import { finalize } from 'rxjs/operators';

import {
    CareersCandidateServiceProxy,
    CareersProfileDto,
    Gender,
    UpdateCareersProfileInput,
} from '@proxies/service-proxies';
import { LocalizePipe } from '@core/localize.pipe';
import { LocalizationService } from '@core/localization.service';
import { ToastService } from '@core/toast.service';
import { GENDER_LABELS, GENDER_OPTIONS } from '@core/enum-labels';
import { CandidateNavComponent } from './candidate-nav.component';

@Component({
    selector: 'careers-profile',
    imports: [FormsModule, LocalizePipe, CandidateNavComponent],
    templateUrl: './profile.component.html',
    styleUrl: './profile.component.css',
})
export class ProfileComponent implements OnInit {
    private _candidateService = inject(CareersCandidateServiceProxy);
    private _toast = inject(ToastService);
    private _localization = inject(LocalizationService);

    readonly genderOptions = GENDER_OPTIONS;
    readonly genderLabels = GENDER_LABELS;

    readonly loading = signal(true);
    readonly saving = signal(false);
    readonly profile = signal<CareersProfileDto | null>(null);

    /**
     * The editable form model.
     *
     * Note there is NO applicant id anywhere in it: `UpdateMyProfile` resolves the row from
     * `Applicant.UserId == AbpSession.UserId`. Sending one would be meaningless and the DTO has no field
     * for it.
     */
    readonly model = signal<UpdateCareersProfileInput>(new UpdateCareersProfileInput());

    /** `birthDate` is a Luxon DateTime on the DTO but `<input type="date">` speaks 'yyyy-MM-dd'. */
    readonly birthDateText = signal('');

    ngOnInit(): void {
        this._candidateService
            .getMyProfile()
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe({
                next: (result) => this.applyProfile(result),
                error: (error) => this._toast.apiError(error),
            });
    }

    private applyProfile(profile: CareersProfileDto): void {
        this.profile.set(profile);

        this.model.set(
            new UpdateCareersProfileInput({
                firstNameEn: profile.firstNameEn,
                secondNameEn: profile.secondNameEn,
                thirdNameEn: profile.thirdNameEn,
                lastNameEn: profile.lastNameEn,
                firstNameAr: profile.firstNameAr,
                secondNameAr: profile.secondNameAr,
                thirdNameAr: profile.thirdNameAr,
                lastNameAr: profile.lastNameAr,
                // Carried through unchanged: there is no anonymous or candidate-visible nationality lookup
                // on the careers surface, so the portal cannot offer a picker. Echoing the current value
                // keeps an unrelated profile edit from silently clearing it.
                nationalityId: profile.nationalityId,
                gender: profile.gender,
                birthDate: profile.birthDate,
                civilID: profile.civilID,
                mobile: profile.mobile,
                telephone: profile.telephone,
                currentAddress: profile.currentAddress,
                permanentAddress: profile.permanentAddress,
            })
        );

        this.birthDateText.set(profile.birthDate?.isValid ? profile.birthDate.toISODate() ?? '' : '');
    }

    /** Signals hold immutable references, so every edit produces a fresh DTO instance. */
    patch(changes: Partial<UpdateCareersProfileInput>): void {
        const updated = new UpdateCareersProfileInput();
        Object.assign(updated, this.model(), changes);
        this.model.set(updated);
    }

    setBirthDate(value: string): void {
        this.birthDateText.set(value);
        this.patch({ birthDate: value ? DateTime.fromISO(value) : (undefined as any) });
    }

    save(form: NgForm): void {
        if (form.invalid || this.saving()) {
            return;
        }

        this.saving.set(true);

        this._candidateService
            .updateMyProfile(this.model())
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe({
                next: (result) => {
                    this.applyProfile(result);
                    this._toast.success(this._localization.localize('CareersProfileSaved'));
                },
                error: (error) => this._toast.apiError(error),
            });
    }

    protected readonly Gender = Gender;
}
