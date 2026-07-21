import { Component, ElementRef, OnInit, inject, model, viewChild, signal } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { ComboboxItemDto, EditionServiceProxy } from '@shared/service-proxies/service-proxies';
import { FormsModule } from '@angular/forms';

@Component({
    selector: 'edition-combo',
    template: `
        <select #EditionCombobox class="form-select" [(ngModel)]="selectedEdition">
            @for (edition of editions(); track edition) {
                <option [value]="edition.value">{{ edition.displayText }}</option>
            }
        </select>
    `,
    imports: [FormsModule],
})
export class EditionComboComponent extends AppComponentBase implements OnInit {
    private _editionService = inject(EditionServiceProxy);

    readonly editionComboboxElement = viewChild<ElementRef>('EditionCombobox');

    selectedEdition = model<string | undefined>(undefined);

    readonly editions = signal<ComboboxItemDto[]>([]);

    ngOnInit(): void {
        this._editionService.getEditionComboboxItems(0, true, false).subscribe((editions) => {
            this.editions.set(editions);
        });
    }
}
