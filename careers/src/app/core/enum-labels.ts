import { AppStatus, Gender, ProficiencyType, VacancyEmploymentType } from '@proxies/service-proxies';

/**
 * Localization keys for the backend enums the portal renders.
 *
 * NOTE ON THE GENERATED ENUMS: NSwag emits real member names for these (`AppStatus.Applied`,
 * `VacancyEmploymentType.FullTime`, ...), NOT the numeric `._0` fallback -- verified against the generated
 * `service-proxies.ts`. Indexing by the enum member is therefore safe and refactor-proof.
 */
export const APP_STATUS_LABELS: Record<AppStatus, string> = {
    [AppStatus.Applied]: 'CareersAppStatusApplied',
    [AppStatus.Shortlisted]: 'CareersAppStatusShortlisted',
    [AppStatus.Interviewed]: 'CareersAppStatusInterviewed',
    [AppStatus.JobOffer]: 'CareersAppStatusJobOffer',
    [AppStatus.Hired]: 'CareersAppStatusHired',
    [AppStatus.Rejected]: 'CareersAppStatusRejected',
    [AppStatus.Joined]: 'CareersAppStatusJoined',
};

/** Drives the colour of the status chip on "my applications". */
export const APP_STATUS_TONE: Record<AppStatus, 'neutral' | 'progress' | 'positive' | 'negative'> = {
    [AppStatus.Applied]: 'neutral',
    [AppStatus.Shortlisted]: 'progress',
    [AppStatus.Interviewed]: 'progress',
    [AppStatus.JobOffer]: 'positive',
    [AppStatus.Hired]: 'positive',
    [AppStatus.Rejected]: 'negative',
    [AppStatus.Joined]: 'positive',
};

export const EMPLOYMENT_TYPE_LABELS: Record<VacancyEmploymentType, string> = {
    [VacancyEmploymentType.FullTime]: 'CareersEmploymentTypeFullTime',
    [VacancyEmploymentType.PartTime]: 'CareersEmploymentTypePartTime',
};

export const PROFICIENCY_LABELS: Record<ProficiencyType, string> = {
    [ProficiencyType.Novice]: 'CareersProficiencyNovice',
    [ProficiencyType.Intermediate]: 'CareersProficiencyIntermediate',
    [ProficiencyType.Advanced]: 'CareersProficiencyAdvanced',
};

export const GENDER_LABELS: Record<Gender, string> = {
    [Gender.Male]: 'Male',
    [Gender.Female]: 'Female',
};

export const PROFICIENCY_OPTIONS = [
    ProficiencyType.Novice,
    ProficiencyType.Intermediate,
    ProficiencyType.Advanced,
];

export const GENDER_OPTIONS = [Gender.Male, Gender.Female];
