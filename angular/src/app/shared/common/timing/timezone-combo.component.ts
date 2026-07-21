import { Component, OnInit, forwardRef, inject, input } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { SettingScopes, NameValueDto, TimingServiceProxy } from '@shared/service-proxies/service-proxies';
import {
    ControlValueAccessor,
    UntypedFormControl,
    NG_VALUE_ACCESSOR,
    FormsModule,
    ReactiveFormsModule,
} from '@angular/forms';

@Component({
    selector: 'timezone-combo',
    template: `
        <select class="form-select" [formControl]="selectedTimeZone">
            @for (timeZone of timeZones; track timeZone) {
                <option [value]="timeZone.value">{{ timeZone.name }}</option>
            }
        </select>
    `,
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef(() => TimeZoneComboComponent),
            multi: true,
        },
    ],
    imports: [FormsModule, ReactiveFormsModule],
})
export class TimeZoneComboComponent extends AppComponentBase implements OnInit, ControlValueAccessor {
    private _timingService = inject(TimingServiceProxy);

    readonly defaultTimezoneScope = input<SettingScopes>(undefined);

    timeZones: NameValueDto[] = [];
    selectedTimeZone = new UntypedFormControl('');

    onTouched: any = () => {};

    ngOnInit(): void {
        let self = this;
        self._timingService.getTimezones(self.defaultTimezoneScope()).subscribe((result) => {
            self.timeZones = result.items;
        });
    }

    writeValue(obj: any): void {
        if (this.selectedTimeZone) {
            this.selectedTimeZone.setValue(obj);
        }
    }

    registerOnChange(fn: any): void {
        this.selectedTimeZone.valueChanges.subscribe(fn);
    }

    registerOnTouched(fn: any): void {
        this.onTouched = fn;
    }

    setDisabledState?(isDisabled: boolean): void {
        if (isDisabled) {
            this.selectedTimeZone.disable();
        } else {
            this.selectedTimeZone.enable();
        }
    }
}
