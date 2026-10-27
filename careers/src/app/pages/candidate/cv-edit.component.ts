import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { DateTime } from 'luxon';
import { finalize } from 'rxjs/operators';

import {
    CareersCandidateServiceProxy,
    CareersCvDetailDto,
    CareersCvLanguageDto,
    CareersCvQualificationDto,
    CareersCvSkillDto,
    CareersCvWorkExperienceDto,
    CreateCareersCvInput,
    ProficiencyType,
    UpdateCareersCvInput,
} from '@proxies/service-proxies';
import { LocalizePipe } from '@core/localize.pipe';
import { LocalizationService } from '@core/localization.service';
import { ToastService } from '@core/toast.service';
import { PROFICIENCY_LABELS, PROFICIENCY_OPTIONS } from '@core/enum-labels';
import { CandidateNavComponent } from './candidate-nav.component';

/** Editable row wrappers keep the `<input type="date">` text next to the Luxon value it maps to. */
interface ExperienceRow {
    company: string;
    position: string;
    startDate: string;
    endDate: string;
    responsibilities: string;
}

@Component({
    selector: 'careers-cv-edit',
    imports: [FormsModule, RouterLink, LocalizePipe, CandidateNavComponent],
    templateUrl: './cv-edit.component.html',
    styleUrl: './cv-edit.component.css',
})
export class CvEditComponent implements OnInit {
    private _candidateService = inject(CareersCandidateServiceProxy);
    private _route = inject(ActivatedRoute);
    private _router = inject(Router);
    private _toast = inject(ToastService);
    private _localization = inject(LocalizationService);

    readonly proficiencyOptions = PROFICIENCY_OPTIONS;
    readonly proficiencyLabels = PROFICIENCY_LABELS;

    readonly cvId = signal<number | null>(null);
    readonly loading = signal(false);
    readonly saving = signal(false);

    readonly cvName = signal('');
    readonly qualifications = signal<CareersCvQualificationDto[]>([]);
    readonly experiences = signal<ExperienceRow[]>([]);
    readonly skills = signal<CareersCvSkillDto[]>([]);

    /**
     * READ-ONLY. `CareersCvLanguageDto` carries only `LanguageInfoId` + `Proficiency` (the name is derived
     * server-side), and there is no candidate-reachable language lookup: `LanguageInfosAppService` is behind
     * `Pages.GeneralMasterInfo.LanguageInfos`, which the `Candidate` role does not hold. A language
     * therefore cannot be ADDED from the portal.
     *
     * Existing rows are still carried in this signal and posted back on save, because
     * `UpdateCareersCvInput`'s child lists REPLACE the stored ones wholesale -- dropping them from the
     * payload would silently delete languages a recruiter had recorded.
     */
    readonly languages = signal<CareersCvLanguageDto[]>([]);

    /** JobTitleId is likewise preserved rather than edited: no candidate-visible job-title lookup exists. */
    private readonly jobTitleId = signal<number | undefined>(undefined);

    readonly isEdit = computed(() => this.cvId() !== null);

    readonly title = computed(() =>
        this._localization.localize(this.isEdit() ? 'CareersEditCv' : 'CareersCreateCv')
    );

    ngOnInit(): void {
        const idParam = this._route.snapshot.paramMap.get('id');
        if (!idParam) {
            return;
        }

        const id = Number(idParam);
        this.cvId.set(id);
        this.loading.set(true);

        // NSwag flattens `EntityDto` for GET: raw id, not `{ id }`.
        this._candidateService
            .getMyCv(id)
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe({
                next: (cv) => this.applyCv(cv),
                error: (error) => {
                    this._toast.apiError(error);
                    void this._router.navigate(['/my/cvs']);
                },
            });
    }

    private applyCv(cv: CareersCvDetailDto): void {
        this.cvName.set(cv.cvName ?? '');
        this.jobTitleId.set(cv.jobTitleId);
        this.qualifications.set([...(cv.qualifications ?? [])]);
        this.skills.set([...(cv.skills ?? [])]);
        this.languages.set([...(cv.languages ?? [])]);
        this.experiences.set(
            (cv.workExperiences ?? []).map((experience) => ({
                company: experience.company ?? '',
                position: experience.position ?? '',
                startDate: experience.startDate?.isValid ? experience.startDate.toISODate() ?? '' : '',
                endDate: experience.endDate?.isValid ? experience.endDate.toISODate() ?? '' : '',
                responsibilities: experience.responsibilities ?? '',
            }))
        );
    }

    // ---------------------------------------------------------------- qualifications --

    addQualification(): void {
        this.qualifications.update((rows) => [
            ...rows,
            new CareersCvQualificationDto({
                qualificationId: undefined,
                qualificationName: undefined,
                university: '',
                percentage: 0,
                passedYear: new Date().getFullYear(),
            }),
        ]);
    }

    patchQualification(index: number, changes: Partial<CareersCvQualificationDto>): void {
        this.qualifications.update((rows) =>
            rows.map((row, i) => {
                if (i !== index) {
                    return row;
                }
                const updated = new CareersCvQualificationDto();
                Object.assign(updated, row, changes);
                return updated;
            })
        );
    }

    removeQualification(index: number): void {
        this.qualifications.update((rows) => rows.filter((_, i) => i !== index));
    }

    // ------------------------------------------------------------------- experiences --

    addExperience(): void {
        this.experiences.update((rows) => [
            ...rows,
            { company: '', position: '', startDate: '', endDate: '', responsibilities: '' },
        ]);
    }

    patchExperience(index: number, changes: Partial<ExperienceRow>): void {
        this.experiences.update((rows) =>
            rows.map((row, i) => (i === index ? { ...row, ...changes } : row))
        );
    }

    removeExperience(index: number): void {
        this.experiences.update((rows) => rows.filter((_, i) => i !== index));
    }

    // ------------------------------------------------------------------------ skills --

    addSkill(): void {
        this.skills.update((rows) => [
            ...rows,
            new CareersCvSkillDto({ skill: '', proficiency: ProficiencyType.Novice }),
        ]);
    }

    patchSkill(index: number, changes: Partial<CareersCvSkillDto>): void {
        this.skills.update((rows) =>
            rows.map((row, i) => {
                if (i !== index) {
                    return row;
                }
                const updated = new CareersCvSkillDto();
                Object.assign(updated, row, changes);
                return updated;
            })
        );
    }

    removeSkill(index: number): void {
        this.skills.update((rows) => rows.filter((_, i) => i !== index));
    }

    // -------------------------------------------------------------------------- save --

    save(form: NgForm): void {
        if (form.invalid || this.saving()) {
            return;
        }

        this.saving.set(true);

        const workExperiences = this.experiences().map(
            (row) =>
                new CareersCvWorkExperienceDto({
                    company: row.company,
                    position: row.position,
                    startDate: row.startDate ? DateTime.fromISO(row.startDate) : (undefined as any),
                    endDate: row.endDate ? DateTime.fromISO(row.endDate) : (undefined as any),
                    responsibilities: row.responsibilities,
                })
        );

        const payload = {
            cvName: this.cvName().trim(),
            jobTitleId: this.jobTitleId(),
            qualifications: this.qualifications(),
            workExperiences,
            skills: this.skills(),
            languages: this.languages(),
        };

        const id = this.cvId();

        const request$ = id
            ? this._candidateService.updateMyCv(new UpdateCareersCvInput({ ...payload, id }))
            : this._candidateService.createMyCv(new CreateCareersCvInput(payload));

        request$.pipe(finalize(() => this.saving.set(false))).subscribe({
            next: () => {
                this._toast.success(this._localization.localize('CareersCvSaved'));
                void this._router.navigate(['/my/cvs']);
            },
            error: (error) => this._toast.apiError(error),
        });
    }
}
