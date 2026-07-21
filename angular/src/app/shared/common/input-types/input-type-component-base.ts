import { AppComponentBase } from '@shared/common/app-component-base';
import { SelectedValuesOptions, AllValuesOptions, ComponentInstanceOptions } from './InputTypeConsts';
import { inject } from '@node_modules/@angular/core';

export abstract class InputTypeComponentBase extends AppComponentBase {
    selectedValues: string[];
    allValues: string[];

    constructor() {
        super();
        this.selectedValues = inject(SelectedValuesOptions);
        this.allValues = inject(AllValuesOptions);
        (inject(ComponentInstanceOptions) as any)(this);
    }

    abstract getSelectedValues(): string[];
}
