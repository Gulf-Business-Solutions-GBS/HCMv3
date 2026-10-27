import { Component, computed, effect, inject, input, Signal, signal, WritableSignal } from '@angular/core';
import { ControlValueAccessor } from '@angular/forms';

// Not an abstract class on purpose. Do not change!
@Component({
    template: '',
    standalone: false,
})
export class AbstractNgModelComponent<T = any, U = T> implements ControlValueAccessor {
    private formsApiDisabled: WritableSignal<boolean> = signal(false);

    readonly disabled = input<boolean>(false);
    readonly effectiveDisabled: Signal<boolean> = computed(() => this.disabled() || this.formsApiDisabled());
    readonly readonly = input<boolean>(undefined);

    onChange?: (value: T) => void;
    onTouched?: () => void;
    protected _value!: T;

    readonly value = input<T>(undefined);
    readonly valueFn = input<(value: U, previousValue?: T) => T>((value) => value as any as T);
    readonly valueLimitFn = input<(value: T, previousValue?: T) => any>((value) => false);

    get defaultValue(): T {
        return this._value;
    }

    constructor() {
        effect(() => {
            let value = this.value();
            if (value === undefined) {
                return;
            }

            value = this.valueFn()(value as any as U, this._value);

            if (this.valueLimitFn()(value, this._value) !== false || this.readonly()) {
                return;
            }

            this._value = value;
            this.notifyValueChange();
        });
    }

    notifyValueChange(): void {
        if (this.onChange) {
            this.onChange(this._value || this.defaultValue);
        }
    }

    writeValue(value: T): void {
        this._value = this.valueLimitFn()(value, this._value) || value;
    }

    registerOnChange(fn: any): void {
        this.onChange = fn;
    }

    registerOnTouched(fn: any): void {
        this.onTouched = fn;
    }

    setDisabledState(isDisabled: boolean): void {
        this.formsApiDisabled.set(isDisabled);
    }
}
