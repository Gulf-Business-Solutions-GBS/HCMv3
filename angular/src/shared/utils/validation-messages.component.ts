import { Component, OnInit, inject, input, effect } from '@angular/core';
import { AppLocalizationService } from '@app/shared/common/localization/app-localization.service';
import { filter as _filter, find as _find, concat as _concat } from 'lodash-es';

class ErrorDef {
    error: string;
    localizationKey: string;
    errorProperty: string;
}

@Component({
    selector: '<validation-messages>',
    template: `
        @if (formCtrl().invalid && (formCtrl().dirty || formCtrl().touched)) {
            <div class="has-danger">
                @for (errorDef of errorDefsInternal; track errorDef) {
                    <div>
                        @if (getErrorDefinitionIsInValid(errorDef)) {
                            <div class="form-control-feedback">
                                {{ getErrorDefinitionMessage(errorDef) }}
                            </div>
                        }
                    </div>
                }
            </div>
        }
    `,
    imports: [],
})
export class ValidationMessagesComponent implements OnInit {
    private appLocalizationService = inject(AppLocalizationService);

    readonly formCtrl = input(undefined);
    readonly customMessages = input<{
        [key: string]: string;
    }>({});
    readonly errorDefs = input<ErrorDef[]>([]);

    _errorDefs: ErrorDef[] = [];

    readonly standartErrorDefs: ErrorDef[] = [
        { error: 'required', localizationKey: 'ThisFieldIsRequired' } as ErrorDef,
        {
            error: 'minlength',
            localizationKey: 'PleaseEnterAtLeastNCharacter',
            errorProperty: 'requiredLength',
        } as ErrorDef,
        {
            error: 'maxlength',
            localizationKey: 'PleaseEnterNoMoreThanNCharacter',
            errorProperty: 'requiredLength',
        } as ErrorDef,
        { error: 'email', localizationKey: 'InvalidEmailAddress' } as ErrorDef,
        { error: 'min', localizationKey: 'ValueMustBeBiggerThanOrEqualToX', errorProperty: 'min' } as ErrorDef,
        { error: 'max', localizationKey: 'ValueMustBeSmallerThanOrEqualToX', errorProperty: 'max' } as ErrorDef,
        { error: 'pattern', localizationKey: 'InvalidPattern', errorProperty: 'requiredPattern' } as ErrorDef,
    ];

    constructor() {
        effect(() => {
            this._errorDefs = this.errorDefs();
        });
    }

    get errorDefsInternal(): ErrorDef[] {
        let standarts = _filter(
            this.standartErrorDefs,
            (ed) => !_find(this._errorDefs, (edC) => edC.error === ed.error)
        );
        let all = <ErrorDef[]>_concat(standarts, this._errorDefs);

        return all;
    }

    ngOnInit() {
        for (const [error, message] of Object.entries(this.customMessages())) {
            const existingErrorIndex = this.standartErrorDefs.findIndex((def) => def.error === error);
            if (existingErrorIndex !== -1) {
                this.standartErrorDefs.splice(existingErrorIndex, 1);
            }
            this.standartErrorDefs.push({ error, localizationKey: message } as ErrorDef);
        }
    }

    getErrorDefinitionIsInValid(errorDef: ErrorDef): boolean {
        return !!this.formCtrl().errors[errorDef.error];
    }

    getErrorDefinitionMessage(errorDef: ErrorDef): string {
        let errorRequirement = this.formCtrl().errors[errorDef.error][errorDef.errorProperty];
        return errorRequirement
            ? this.appLocalizationService.l(errorDef.localizationKey, errorRequirement)
            : this.appLocalizationService.l(errorDef.localizationKey);
    }
}
