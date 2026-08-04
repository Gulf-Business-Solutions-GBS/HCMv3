import { Pipe, PipeTransform, inject } from '@angular/core';
import { LocalizationService } from './localization.service';

/**
 * `{{ 'JobOpenings' | localize }}`. Pure: the localization dictionary is loaded once before bootstrap and
 * never mutates, so there is nothing to re-evaluate.
 */
@Pipe({ name: 'localize' })
export class LocalizePipe implements PipeTransform {
    private _localizationService = inject(LocalizationService);

    transform(value: string, ...args: any[]): string {
        return this._localizationService.localize(value, ...args);
    }
}
