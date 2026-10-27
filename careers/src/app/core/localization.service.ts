import { Injectable } from '@angular/core';
import { AppConsts } from './app-consts';

/**
 * Reads localized texts out of the ABP localization dictionary that `CareersPreBootstrap` downloaded from
 * `/AbpUserConfiguration/GetAll`.
 *
 * The portal deliberately reuses the server's `HCMv3` localization source rather than shipping its own
 * translation files, so a text added to `HCMv3.xml` is immediately available here with no portal redeploy.
 */
@Injectable({ providedIn: 'root' })
export class LocalizationService {
    /**
     * Returns the localized text for `key`, or the key itself when it is missing so that an untranslated
     * string is obvious in the UI rather than rendering as an empty element.
     */
    localize(key: string, ...args: any[]): string {
        let value = AppConsts.localization[key];

        if (value === undefined || value === null) {
            return key;
        }

        args.forEach((arg, index) => {
            value = value.replace(new RegExp('\\{' + index + '\\}', 'g'), arg == null ? '' : String(arg));
        });

        return value;
    }
}
