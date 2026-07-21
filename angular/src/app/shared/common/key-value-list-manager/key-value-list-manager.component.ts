import { Component, input, model } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { findIndex as _findIndex, remove as _remove } from 'lodash-es';
import { FormsModule } from '@angular/forms';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

interface KeyValueItem {
    key: string;
    value: string;
}

@Component({
    selector: 'key-value-list-manager',
    templateUrl: './key-value-list-manager.component.html',
    styleUrls: ['./key-value-list-manager.component.css'],
    imports: [FormsModule, LocalizePipe],
})
export class KeyValueListManagerComponent extends AppComponentBase {
    readonly header = input<string>(undefined);
    readonly keyPlaceHolder = input<string>(undefined);
    readonly valuePlaceHolder = input<string>(undefined);
    items = model<KeyValueItem[]>([], { alias: 'items' });

    addOrEditKey = '';
    addOrEditValue = '';

    isEdit = false;

    constructor() {
        super();
        if (!this.keyPlaceHolder()) {
            this.l('Key');
        }

        if (!this.valuePlaceHolder()) {
            this.l('Value');
        }
    }

    onKeyChange() {
        let itemIndex = _findIndex(this.items(), (item) => item.key === this.addOrEditKey);
        this.isEdit = itemIndex !== -1;
        if (this.isEdit) {
            this.addOrEditValue = this.items()[itemIndex].value;
        }
    }

    openItemEdit(keyValueItem: { key: string; value: string }) {
        this.addOrEditKey = keyValueItem.key;
        this.addOrEditValue = keyValueItem.value;

        this.isEdit = true;
    }

    removeItem(keyValueItem: KeyValueItem) {
        _remove(this.items(), (item) => item.key === keyValueItem.key);
        this.onKeyChange();
    }

    addOrEdit() {
        if (!this.addOrEditKey || !this.addOrEditValue) {
            return;
        }

        const newItem = {
            key: this.addOrEditKey,
            value: this.addOrEditValue,
        };

        const indexOfItemInArray = _findIndex(this.items(), (item) => item.key === newItem.key);

        if (indexOfItemInArray !== -1) {
            // Edit
            const newItems = this.items().map((item) => (item.key === newItem.key ? newItem : item));
            this.items.set(newItems);
        } else {
            // Add
            const newItems = [...this.items(), newItem];
            this.items.set(newItems);
        }

        this.addOrEditKey = '';
        this.addOrEditValue = '';
    }

    getItems(): KeyValueItem[] {
        return this.items();
    }
}
