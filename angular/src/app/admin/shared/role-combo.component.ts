import { Component, OnInit, forwardRef, inject, signal } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { RoleListDto, GetRolesInput, RoleServiceProxy } from '@shared/service-proxies/service-proxies';
import {
    ControlValueAccessor,
    UntypedFormControl,
    NG_VALUE_ACCESSOR,
    FormsModule,
    ReactiveFormsModule,
} from '@angular/forms';

import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'role-combo',
    template: `
        <select class="form-select" [formControl]="selectedRole">
            <option value="">{{ 'FilterByRole' | localize }}</option>
            @for (role of roles(); track role.id) {
                <option [value]="role.id">{{ role.displayName }}</option>
            }
        </select>
    `,
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef(() => RoleComboComponent),
            multi: true,
        },
    ],
    imports: [FormsModule, ReactiveFormsModule, LocalizePipe],
})
export class RoleComboComponent extends AppComponentBase implements OnInit, ControlValueAccessor {
    private _roleService = inject(RoleServiceProxy);

    readonly roles = signal<RoleListDto[]>([]);
    selectedRole = new UntypedFormControl('');

    onTouched: any = () => {};

    ngOnInit(): void {
        this._roleService.getRoles(new GetRolesInput({ permissions: [] })).subscribe((result) => {
            this.roles.set(result.items);
        });
    }

    writeValue(obj: any): void {
        if (this.selectedRole) {
            this.selectedRole.setValue(obj);
        }
    }

    registerOnChange(fn: any): void {
        this.selectedRole.valueChanges.subscribe(fn);
    }

    registerOnTouched(fn: any): void {
        this.onTouched = fn;
    }

    setDisabledState?(isDisabled: boolean): void {
        if (isDisabled) {
            this.selectedRole.disable();
        } else {
            this.selectedRole.enable();
        }
    }
}
