import { AppStatus } from '@shared/service-proxies/service-proxies';

/** Localization keys of the application statuses, as the Create/Edit dialog lists them. */
export const APP_STATUS_KEYS: Record<number, string> = {
    [AppStatus.Applied]: 'Applied',
    [AppStatus.Shortlisted]: 'Shortlisted',
    [AppStatus.Interviewed]: 'Interviewed',
    [AppStatus.JobOffer]: 'JobOffer',
    [AppStatus.Hired]: 'Hired',
    [AppStatus.Rejected]: 'Rejected',
    [AppStatus.Joined]: 'Joined',
};

/**
 * The status key for a value as ABP entity history stores it: the JSON of the property, so an enum arrives
 * as "3" (or "\"3\"" / a name, depending on the serializer). Unknown values are returned as they are.
 */
export function appStatusKeyFromHistoryValue(raw: string | undefined): string {
    if (raw === undefined || raw === null) {
        return '';
    }
    const value = raw.replace(/^"+|"+$/g, '').trim();
    if (value === '' || value === 'null') {
        return '';
    }
    const asNumber = Number(value);
    if (!Number.isNaN(asNumber) && APP_STATUS_KEYS[asNumber]) {
        return APP_STATUS_KEYS[asNumber];
    }
    return Object.values(APP_STATUS_KEYS).find((key) => key.toLowerCase() === value.toLowerCase()) ?? value;
}
