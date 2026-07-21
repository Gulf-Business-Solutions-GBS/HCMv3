import { Component, OnInit } from '@angular/core';
import { InputTypeComponentBase } from '../input-type-component-base';
import { FormsModule } from '@angular/forms';

@Component({
    selector: 'app-combobox-input-type',
    templateUrl: './combobox-input-type.component.html',
    styleUrls: ['./combobox-input-type.component.css'],
    imports: [FormsModule],
})
export class ComboboxInputTypeComponent extends InputTypeComponentBase implements OnInit {
    selectedValue: string;

    ngOnInit() {
        this.selectedValue = this.selectedValues[0];
    }

    getSelectedValues(): string[] {
        if (!this.selectedValue) {
            return [];
        }
        return [this.selectedValue];
    }
}
