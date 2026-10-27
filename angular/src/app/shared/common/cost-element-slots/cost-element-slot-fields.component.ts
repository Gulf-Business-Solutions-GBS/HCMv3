import { Component, OnInit, inject, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CostElementSlotDto } from '@shared/service-proxies/service-proxies';
import { CostElementSlotField, CostElementSlotsService } from './cost-element-slots.service';

export interface CostElementSlotChange {
    field: CostElementSlotField;
    value: number | undefined;
}

/**
 * The cost element dropdowns of a wage account row: one per configured slot, captioned with the slot's cost
 * element type and listing only cost elements of that type. Unconfigured slots are not rendered.
 *
 * Lives inside the host modal's ngForm, so it uses standalone ngModels (the fields are optional and the
 * host owns the record): the host applies (fieldChange) to its own record signal.
 *
 * Usage: <costElementSlotFields [record]="record()" idPrefix="EmployeeWageAccount_" (fieldChange)="updateField($event.field, $event.value)" />
 */
@Component({
    selector: 'costElementSlotFields',
    template: `
        @if (slotsService.slots().length > 0) {
            <hr />
            <div class="row">
                @for (slot of slotsService.slots(); track slot.slot) {
                    <div class="col-md-3 mb-5">
                        <label class="form-label" [for]="idPrefix() + fieldOf(slot)">
                            {{ slotsService.label(slot) }}
                        </label>
                        <select
                            [id]="idPrefix() + fieldOf(slot)"
                            class="form-select"
                            [ngModel]="valueOf(slot)"
                            [ngModelOptions]="{ standalone: true }"
                            [disabled]="disabled()"
                            (ngModelChange)="fieldChange.emit({ field: fieldOf(slot), value: $event })">
                            <option [ngValue]="undefined"></option>
                            @for (option of slot.options ?? []; track option.id) {
                                <option [ngValue]="option.id">
                                    {{ optionLabel(option.code, option.nameEn, option.nameAr) }}
                                </option>
                            }
                            @if (isOrphan(slot)) {
                                <option [ngValue]="valueOf(slot)">#{{ valueOf(slot) }}</option>
                            }
                        </select>
                    </div>
                }
            </div>
        }
    `,
    imports: [FormsModule],
})
export class CostElementSlotFieldsComponent implements OnInit {
    readonly slotsService = inject(CostElementSlotsService);

    /** The row being edited; read for the current CostElementNId values. */
    readonly record = input.required<unknown>();
    /** Prefix for element ids, e.g. "CostRegisterDetail_". */
    readonly idPrefix = input<string>('CostElement_');
    readonly disabled = input<boolean>(false);

    readonly fieldChange = output<CostElementSlotChange>();

    ngOnInit(): void {
        // Fresh on every open: the modal is short-lived, and a type may have been set up since the last one.
        this.slotsService.refresh().subscribe();
    }

    fieldOf(slot: CostElementSlotDto): CostElementSlotField {
        return this.slotsService.fieldOf(slot);
    }

    valueOf(slot: CostElementSlotDto): number | undefined {
        return (this.record() as Record<string, number | undefined>)?.[this.fieldOf(slot)] ?? undefined;
    }

    /** A saved value that is no longer of the slot's type: kept selectable so opening the row does not blank it. */
    isOrphan(slot: CostElementSlotDto): boolean {
        const value = this.valueOf(slot);
        return !!value && !slot.options?.some((o) => o.id === value);
    }

    optionLabel(code: string | undefined, nameEn: string | undefined, nameAr: string | undefined): string {
        const name = this.slotsService.pick(nameEn, nameAr);
        return code && name ? `${code} - ${name}` : name || code || '';
    }
}
