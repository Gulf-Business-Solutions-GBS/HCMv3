import { Component, OnInit } from '@angular/core';
import { InputTypeComponentBase } from '../input-type-component-base';
import { FormsModule } from '@angular/forms';

@Component({
    selector: 'app-single-line-string-input-type',
    templateUrl: './single-line-string-input-type.component.html',
    styleUrls: ['./single-line-string-input-type.component.css'],
    imports: [FormsModule],
})
export class SingleLineStringInputTypeComponent extends InputTypeComponentBase implements OnInit {
    selectedValue: string;

    getSelectedValues(): string[] {
        if (!this.selectedValue) {
            return [];
        }
        return [this.selectedValue];
    }

    ngOnInit(): void {
        this.selectedValue = this.selectedValues[0];
    }
}
