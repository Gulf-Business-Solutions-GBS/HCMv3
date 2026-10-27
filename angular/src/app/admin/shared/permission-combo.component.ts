import { Component, OnInit, forwardRef, inject, signal } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { FlatPermissionWithLevelDto, PermissionServiceProxy } from '@shared/service-proxies/service-proxies';
import { forEach as _forEach } from 'lodash-es';
import {
    ControlValueAccessor,
    UntypedFormControl,
    NG_VALUE_ACCESSOR,
    FormsModule,
    ReactiveFormsModule,
} from '@angular/forms';

import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'permission-combo',
    template: `
        <select class="form-select" [formControl]="selectedPermission">
            <option value="">{{ 'FilterByPermission' | localize }}</option>
            @for (permission of permissions(); track permission) {
                <option [value]="permission.name">
                    {{ permission.displayName }}
                </option>
            }
        </select>
    `,
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef(() => PermissionComboComponent),
            multi: true,
        },
    ],
    imports: [FormsModule, ReactiveFormsModule, LocalizePipe],
})
export class PermissionComboComponent extends AppComponentBase implements OnInit, ControlValueAccessor {
    private _permissionService = inject(PermissionServiceProxy);

    readonly permissions = signal<FlatPermissionWithLevelDto[]>([]);
    selectedPermission = new UntypedFormControl('');

    onTouched: any = () => {};

    ngOnInit(): void {
        this._permissionService.getAllPermissions().subscribe((result) => {
            _forEach(result.items, (item) => {
                item.displayName = Array(item.level + 1).join('---') + ' ' + item.displayName;
            });

            this.permissions.set(result.items);
        });
    }

    writeValue(obj: any): void {
        if (this.selectedPermission) {
            this.selectedPermission.setValue(obj);
        }
    }

    registerOnChange(fn: any): void {
        this.selectedPermission.valueChanges.subscribe(fn);
    }

    registerOnTouched(fn: any): void {
        this.onTouched = fn;
    }

    setDisabledState?(isDisabled: boolean): void {
        if (isDisabled) {
            this.selectedPermission.disable();
        } else {
            this.selectedPermission.enable();
        }
    }
}
