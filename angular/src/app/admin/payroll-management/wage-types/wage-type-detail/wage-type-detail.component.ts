import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { CreateOrEditWageTypeDto, WageTypesServiceProxy } from '@shared/service-proxies/service-proxies';
import { finalize } from 'rxjs/operators';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { WageTypeFormComponent } from '../wage-type-form.component';
import { WageTypeLeavesTabComponent } from './wage-type-leaves-tab.component';
import { WageTypeSecurityRolesTabComponent } from './wage-type-security-roles-tab.component';
import { WageTypeExceptionsTabComponent } from './wage-type-exceptions-tab.component';

interface WageTypeDetailTab {
    /** Goes in the URL fragment (`/wage-types/12#security-roles`), so a refresh keeps the tab. */
    id: string;
    labelKey: string;
    permission: string;
}

const WAGE_TYPE_DETAIL_TABS: WageTypeDetailTab[] = [
    { id: 'leave-types', labelKey: 'LeaveTypes', permission: 'Pages.PayrollManagement.WageTypeLeaves' },
    { id: 'security-roles', labelKey: 'SecurityRoles', permission: 'Pages.PayrollManagement.WageTypeSecurityRoles' },
    { id: 'exceptions', labelKey: 'WageTypeExceptions', permission: 'Pages.PayrollManagement.WageTypeExceptions' },
];

/**
 * One wage type: its own fields, editable in place, and its child lists as tabs below.
 * Only the active tab's grid is built.
 */
@Component({
    templateUrl: './wage-type-detail.component.html',
    styleUrl: './wage-type-detail.component.css',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        BusyIfDirective,
        ButtonBusyDirective,
        FormsModule,
        WageTypeFormComponent,
        WageTypeLeavesTabComponent,
        WageTypeSecurityRolesTabComponent,
        WageTypeExceptionsTabComponent,
        LocalizePipe,
    ],
})
export class WageTypeDetailComponent extends AppComponentBase implements OnInit {
    private _route = inject(ActivatedRoute);
    private _router = inject(Router);
    private _wageTypesService = inject(WageTypesServiceProxy);

    readonly wageTypeId = signal<number | undefined>(undefined);
    readonly wageType = signal<CreateOrEditWageTypeDto>(new CreateOrEditWageTypeDto());
    readonly loaded = signal<boolean>(false);
    readonly loading = signal<boolean>(false);
    readonly saving = signal<boolean>(false);

    /**
     * Whether the fields can be changed. The page opens read-only -- looking a record up should not leave it one
     * keystroke from being changed -- and only a user with the Edit permission is offered the Edit button.
     */
    readonly editing = signal<boolean>(false);

    /** The record as last loaded or saved, so Cancel can put back whatever was typed since. */
    private _saved: CreateOrEditWageTypeDto | undefined;

    /** Code and name as last loaded or saved, so the title does not change while the name is being typed. */
    readonly savedCode = signal<string>('');
    readonly savedName = signal<string>('');

    readonly title = computed(() =>
        this.savedCode() ? `${this.savedCode()} - ${this.savedName()}` : this.l('WageTypes')
    );

    readonly canEdit = this.permission.isGranted('Pages.PayrollManagement.WageTypes.Edit');

    readonly tabs = WAGE_TYPE_DETAIL_TABS.filter((tab) => this.permission.isGranted(tab.permission));
    readonly activeTabId = signal<string | undefined>(this.tabs[0]?.id);

    ngOnInit(): void {
        this._route.paramMap.subscribe((params) => {
            const id = Number(params.get('id'));
            if (id) {
                this.wageTypeId.set(id);
                this.loadWageType(id);
            }
        });

        // Browser back/forward and a refresh both land on the tab named in the fragment.
        this._route.fragment.subscribe((fragment) => {
            const requested = this.tabs.find((tab) => tab.id === fragment);
            this.activeTabId.set((requested ?? this.tabs[0])?.id);
        });
    }

    loadWageType(id: number): void {
        this.loading.set(true);
        this._wageTypesService
            .getWageTypeForEdit(id)
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe((result) => {
                this.wageType.set(result.wageType);
                this._saved = this.copy(result.wageType);
                this.editing.set(this.canEdit && this._route.snapshot.queryParamMap.get('edit') === 'true');
                this.dropEditParam();
                this.setSavedTitle(result.wageType);
                this.loaded.set(true);
            });
    }

    selectTab(tab: WageTypeDetailTab): void {
        if (this.activeTabId() === tab.id) {
            return;
        }

        this.activeTabId.set(tab.id);
        // replaceUrl: switching tabs on one record is not a separate place for the back button to visit.
        this._router.navigate([], { relativeTo: this._route, fragment: tab.id, replaceUrl: true });
    }

    save(): void {
        if (!this.canEdit) {
            return;
        }

        this.saving.set(true);
        this._wageTypesService
            .createOrEdit(this.wageType())
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe(() => {
                this.setSavedTitle(this.wageType());
                this._saved = this.copy(this.wageType());
                this.editing.set(false);
                this.notify.info(this.l('SavedSuccessfully'));
            });
    }

    goBack(): void {
        this._router.navigate(['/app/admin/wage-types']);
    }

    startEditing(): void {
        if (this.canEdit) {
            this.editing.set(true);
        }
    }

    /**
     * The list's Edit action opens this page with ?edit=true. Taken off once read, so a refresh or a shared
     * link opens the record for viewing again rather than straight into editing.
     */
    private dropEditParam(): void {
        if (this._route.snapshot.queryParamMap.has('edit')) {
            this._router.navigate([], {
                relativeTo: this._route,
                queryParams: { edit: null },
                queryParamsHandling: 'merge',
                preserveFragment: true,
                replaceUrl: true,
            });
        }
    }

    /** Throws away unsaved changes and goes back to viewing. */
    cancelEditing(): void {
        if (this._saved) {
            this.wageType.set(this.copy(this._saved));
        }
        this.editing.set(false);
    }

    /** A detached copy: the form writes into the object it is given, and the saved version must not follow it. */
    private copy(value: CreateOrEditWageTypeDto): CreateOrEditWageTypeDto {
        return CreateOrEditWageTypeDto.fromJS(value.toJSON());
    }

    private setSavedTitle(wageType: CreateOrEditWageTypeDto): void {
        this.savedCode.set(wageType?.code ?? '');
        this.savedName.set(wageType?.nameEn ?? '');
    }
}
