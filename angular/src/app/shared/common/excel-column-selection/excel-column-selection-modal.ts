import { Component, OnInit, inject, output, signal, viewChild } from '@angular/core';
import { FormArray, FormBuilder, FormGroup, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { AppComponentBase } from '@shared/common/app-component-base';
import { findIndex as _findIndex, remove as _remove } from 'lodash-es';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';

import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'excel-column-selection-modal',
    templateUrl: './excel-column-selection-modal.html',
    imports: [AppBsModalDirective, FormsModule, ReactiveFormsModule, ButtonBusyDirective, LocalizePipe],
})
export class ExcelColumnSelectionModalComponent extends AppComponentBase implements OnInit {
    private fb = inject(FormBuilder);

    readonly modal = viewChild<ModalDirective>('exportExcelModal');
    readonly modalSave = output<any>();

    readonly active = signal(false);
    readonly saving = signal(false);
    form: FormGroup;
    toggleAllBtnText: string;
    selectedColumnsList: string[];

    ngOnInit(): void {
        this.form = this.fb.group({
            selectedColumns: this.fb.array([]),
        });
    }

    show(excelColumns: string[]): void {
        this.active.set(true);
        this.toggleAllBtnText = this.l('SelectAll');

        this.selectedColumnsList = excelColumns;

        this.form = this.fb.group({
            selectedColumns: this.fb.array(Array(excelColumns.length).fill(false)),
        });

        this.modal().show();
    }

    toggleClick(): void {
        const formArray = this.form.controls.selectedColumns as FormArray;
        const allSelected = formArray.controls.every((control) => control.value);

        if (allSelected) {
            formArray.controls.forEach((control) => {
                control.setValue(false);
                this.toggleAllBtnText = this.l('SelectAll');
            });
        } else {
            formArray.controls.forEach((control) => {
                control.setValue(true);
                this.toggleAllBtnText = this.l('UnselectAll');
            });
        }
    }

    selectedColumnChange(): void {
        const formArray = this.form.controls.selectedColumns as FormArray;
        const allSelected = formArray.controls.every((control) => control.value);

        if (allSelected) {
            formArray.controls.forEach(() => {
                this.toggleAllBtnText = this.l('UnselectAll');
            });
        } else {
            formArray.controls.forEach(() => {
                this.toggleAllBtnText = this.l('SelectAll');
            });
        }
    }

    selectColumns(): void {
        this.saving.set(true);

        const formArray = this.form.controls.selectedColumns as FormArray;
        const selectedColumns = formArray.controls
            .map((control, index) => ({ index, value: control.value }))
            .filter((control) => control.value)
            .map((control) => this.selectedColumnsList[control.index]);

        this.modalSave.emit(selectedColumns);
        this.saving.set(false);
        this.close();
    }

    close(): void {
        this.active.set(false);
        this.modal().hide();
    }
}
