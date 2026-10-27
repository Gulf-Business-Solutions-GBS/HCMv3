import { Component, OnInit, computed, inject, signal, viewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { ClearanceRequestFieldDto, ClearanceRequestFieldsServiceProxy } from '@shared/service-proxies/service-proxies';
import { finalize } from 'rxjs/operators';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { CreateOrEditClearanceRequestFieldModalComponent } from '../create-or-edit-clearance-request-field-modal.component';
import { ClearanceRequestFieldDetailsTabComponent } from './clearance-request-field-details-tab.component';

interface RecordTab {
    /** Goes in the URL fragment (`/clearance-request-fields/12#details`), so a refresh keeps the tab. */
    id: string;
    labelKey: string;
    permission: string;
}

const CLEARANCE_REQUEST_FIELD_TABS: RecordTab[] = [
    {
        id: 'details',
        labelKey: 'ClearanceRequestFieldDetails',
        permission: 'Pages.GeneralMasterInfo.ClearanceRequestFieldDetails',
    },
];

/**
 * One clearance checklist line: its own fields read-only (Edit opens the usual dialog) and, as a tab, the
 * workflow roles that answer it -- the way legacy showed Field Details inside the field.
 */
@Component({
    templateUrl: './clearance-request-field-detail.component.html',
    styleUrl: '../../../../shared/common/detail-tabs/detail-tabs.css',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        BusyIfDirective,
        CreateOrEditClearanceRequestFieldModalComponent,
        ClearanceRequestFieldDetailsTabComponent,
        LocalizePipe,
    ],
})
export class ClearanceRequestFieldDetailComponent extends AppComponentBase implements OnInit {
    private _route = inject(ActivatedRoute);
    private _router = inject(Router);
    private _fieldsService = inject(ClearanceRequestFieldsServiceProxy);

    readonly editModal = viewChild<CreateOrEditClearanceRequestFieldModalComponent>('editModal');

    readonly fieldId = signal<number | undefined>(undefined);
    readonly field = signal<ClearanceRequestFieldDto | undefined>(undefined);
    readonly loading = signal<boolean>(false);

    readonly title = computed(() => {
        const field = this.field();
        if (!field) {
            return this.l('ClearanceRequestFields');
        }
        return field.code ? `${field.code} - ${field.nameEn}` : field.nameEn;
    });

    readonly canEdit = this.permission.isGranted('Pages.GeneralMasterInfo.ClearanceRequestFields.Edit');

    readonly tabs = CLEARANCE_REQUEST_FIELD_TABS.filter((tab) => this.permission.isGranted(tab.permission));
    readonly activeTabId = signal<string | undefined>(this.tabs[0]?.id);

    ngOnInit(): void {
        this._route.paramMap.subscribe((params) => {
            const id = Number(params.get('id'));
            if (id) {
                this.fieldId.set(id);
                this.load(id);
            }
        });

        this._route.fragment.subscribe((fragment) => {
            const requested = this.tabs.find((tab) => tab.id === fragment);
            this.activeTabId.set((requested ?? this.tabs[0])?.id);
        });
    }

    load(id: number): void {
        this.loading.set(true);
        this._fieldsService
            .getClearanceRequestField(id)
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe((result) => this.field.set(result));
    }

    selectTab(tab: RecordTab): void {
        if (this.activeTabId() === tab.id) {
            return;
        }
        this.activeTabId.set(tab.id);
        this._router.navigate([], { relativeTo: this._route, fragment: tab.id, replaceUrl: true });
    }

    edit(): void {
        if (this.canEdit && this.fieldId()) {
            this.editModal().show(this.fieldId());
        }
    }

    reload(): void {
        if (this.fieldId()) {
            this.load(this.fieldId());
        }
    }

    goBack(): void {
        this._router.navigate(['/app/admin/clearance-request-fields']);
    }
}
