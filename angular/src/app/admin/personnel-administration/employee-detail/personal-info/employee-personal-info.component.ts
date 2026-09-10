import { Component, OnInit, inject, input, signal, computed, ViewChild } from '@angular/core';
import { DatePipe } from '@angular/common';
import { finalize } from 'rxjs/operators';
import { AppComponentBase } from '@shared/common/app-component-base';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import {
    EmployeesServiceProxy,
    EmployeeProfileDto,
    Gender,
    EmployeeStatus,
    EmploymentType,
} from '@shared/service-proxies/service-proxies';
import { CreateOrEditEmployeeModalComponent } from '../../employees/create-or-edit-employee-modal.component';
import { HelpPageDirective } from '@app/shared/help/help-page.directive';
import { HelpService } from '@app/shared/help/help.service';

/**
 * The employee's own particulars, as a read view with Edit in reach.
 *
 * WHY THIS EXISTS. Everything else on the record is a list of things that happen TO an employee --
 * absences, loans, contracts -- and the person themselves was the one thing the screen did not show.
 * Their name, gender and nationality were only visible by leaving the record and opening the edit
 * form, which meant looking something up cost you your place.
 *
 * READ FIRST, EDIT ON DEMAND. The fields are shown as text, not inputs, because the common case is
 * looking something up rather than changing it, and a screen of 58 inputs invites accidental edits to
 * a record several other screens depend on. Edit opens the EXISTING create-or-edit form rather than a
 * second one: two forms over one entity drift apart, and the one with the validation would not be
 * this one.
 */
@Component({
    selector: 'employee-personal-info',
    templateUrl: './employee-personal-info.component.html',
    styleUrl: './employee-personal-info.component.css',
    imports: [
        BusyIfDirective,
        LocalizePipe,
        PermissionPipe,
        DatePipe,
        CreateOrEditEmployeeModalComponent,
        HelpPageDirective,
    ],
})
export class EmployeePersonalInfoComponent extends AppComponentBase implements OnInit {
    private _employeesService = inject(EmployeesServiceProxy);

    readonly help = inject(HelpService);

    readonly employeeId = input<number>();

    @ViewChild('createOrEditModal') createOrEditModal: CreateOrEditEmployeeModalComponent;

    readonly isLoading = signal<boolean>(false);
    readonly profile = signal<EmployeeProfileDto | undefined>(undefined);

    readonly genderText = computed(() => this.enumText(Gender, this.profile()?.gender));
    readonly statusText = computed(() => this.enumText(EmployeeStatus, this.profile()?.status));
    readonly employmentTypeText = computed(() =>
        this.enumText(EmploymentType, this.profile()?.employmentType)
    );

    ngOnInit(): void {
        this.load();
    }

    load(): void {
        const id = this.employeeId();
        if (!id) {
            return;
        }

        this.isLoading.set(true);
        this._employeesService
            .getEmployeeProfile(id)
            .pipe(finalize(() => this.isLoading.set(false)))
            .subscribe((result) => this.profile.set(result));
    }

    edit(): void {
        this.createOrEditModal.show(this.employeeId());
    }

    /** Re-reads after the modal saves, so the view never shows what was just replaced. */
    onSaved(): void {
        this.load();
    }

    /**
     * Formats a phone number stored as a long.
     *
     * Zero is how "not recorded" is spelled in a non-nullable numeric column, and rendering it as "0"
     * would read as a real value.
     */
    phone(value: number | undefined): string {
        return !value ? '' : String(value);
    }

    /** Same reasoning as {@link phone}: a Civil ID of 0 means absent, not zero. */
    idNumber(value: number | undefined): string {
        return !value ? '' : String(value);
    }

    private enumText(enumType: Record<number, string>, value: number | undefined): string {
        return value === undefined || value === null ? '' : enumType[value];
    }
}
