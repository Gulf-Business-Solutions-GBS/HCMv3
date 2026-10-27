import { Pipe, PipeTransform, inject } from '@angular/core';
import { PermissionCheckerService } from 'abp-ng2-module';

@Pipe({ name: 'permission' })
export class PermissionPipe implements PipeTransform {
    private permission = inject(PermissionCheckerService);

    transform(permission: string): boolean {
        return this.permission.isGranted(permission);
    }
}
