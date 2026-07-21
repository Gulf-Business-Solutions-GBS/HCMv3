import { Component, OnInit } from '@angular/core';
import { InputTypeComponentBase } from '../input-type-component-base';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { FormsModule } from '@angular/forms';

@Component({
    selector: 'app-multiple-select-input-type',
    templateUrl: './multiple-select-combobox-input-type.component.html',
    imports: [AutoCompleteModule, FormsModule],
})
export class MultipleSelectComboboxInputTypeComponent extends InputTypeComponentBase implements OnInit {
    filteredValues: string[];

    ngOnInit() {
        this.filteredValues = this.allValues;
    }

    getSelectedValues(): string[] {
        if (!this.selectedValues) {
            return [];
        }
        return this.selectedValues;
    }

    filter(event) {
        this.filteredValues = this.allValues.filter((item) => item.toLowerCase().includes(event.query.toLowerCase()));
    }
}
