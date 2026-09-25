import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { CreateOrEditRequestMasterDto, RequestMastersServiceProxy } from '@shared/service-proxies/service-proxies';
import { finalize } from 'rxjs/operators';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { RequestMasterFormComponent } from '../request-masters/request-master-form.component';
import { RequestMasterStepsComponent } from './request-master-steps/request-master-steps.component';

interface RequestMasterDetailTab {
    /** Goes in the URL fragment (`/request-masters/12#steps`), so a refresh keeps the tab. */
    id: string;
    labelKey: string;
    permission: string;
}

const REQUEST_MASTER_DETAIL_TABS: RequestMasterDetailTab[] = [
    { id: 'steps', labelKey: 'ApprovalSteps', permission: 'Pages.Workflow.RequestDetails' },
];

/**
 * One approval chain: its own fields, editable in place, and its steps in a tab below.
 *
 * <p>Replaces the standalone Request Details screen. A step is meaningless away from the chain it belongs to,
 * and keeping them apart meant configuring a chain took two screens and a dropdown to pair them back up --
 * with nothing stopping a step being filed against the wrong chain in between.</p>
 */
@Component({
    templateUrl: './request-master-detail.component.html',
    styleUrl: './request-master-detail.component.css',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        BusyIfDirective,
        ButtonBusyDirective,
        FormsModule,
        RequestMasterFormComponent,
        RequestMasterStepsComponent,
        LocalizePipe,
    ],
})
export class RequestMasterDetailComponent extends AppComponentBase implements OnInit {
    private _route = inject(ActivatedRoute);
    private _router = inject(Router);
    private _service = inject(RequestMastersServiceProxy);

    readonly requestMasterId = signal<number | undefined>(undefined);
    readonly requestMaster = signal<CreateOrEditRequestMasterDto>(new CreateOrEditRequestMasterDto());
    readonly loaded = signal<boolean>(false);
    readonly loading = signal<boolean>(false);
    readonly saving = signal<boolean>(false);

    /**
     * Whether the fields can be changed. The page opens read-only -- looking a chain up should not leave it
     * one keystroke from being changed -- and only a user with the Edit permission is offered the button.
     */
    readonly editing = signal<boolean>(false);

    /** The chain as last loaded or saved, so Cancel can put back whatever was typed since. */
    private _saved: CreateOrEditRequestMasterDto | undefined;

    /** Code and name as last loaded or saved, so the title does not change while the name is being typed. */
    readonly savedCode = signal<string>('');
    readonly savedName = signal<string>('');

    readonly title = computed(() =>
        this.savedCode() ? `${this.savedCode()} - ${this.savedName()}` : this.l('RequestMasters')
    );

    readonly canEdit = this.permission.isGranted('Pages.Workflow.RequestMasters.Edit');

    readonly tabs = REQUEST_MASTER_DETAIL_TABS.filter((tab) => this.permission.isGranted(tab.permission));
    readonly activeTabId = signal<string | undefined>(this.tabs[0]?.id);

    ngOnInit(): void {
        this._route.paramMap.subscribe((params) => {
            const id = Number(params.get('id'));
            if (id) {
                this.requestMasterId.set(id);
                this.loadRequestMaster(id);
            }
        });

        // Browser back/forward and a refresh both land on the tab named in the fragment.
        this._route.fragment.subscribe((fragment) => {
            const requested = this.tabs.find((tab) => tab.id === fragment);
            this.activeTabId.set((requested ?? this.tabs[0])?.id);
        });
    }

    loadRequestMaster(id: number): void {
        this.loading.set(true);
        this._service
            .getRequestMasterForEdit(id)
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe((result) => {
                this.requestMaster.set(result.requestMaster);
                this._saved = this.copy(result.requestMaster);
                this.editing.set(this.canEdit && this._route.snapshot.queryParamMap.get('edit') === 'true');
                this.dropEditParam();
                this.setSavedTitle(result.requestMaster);
                this.loaded.set(true);
            });
    }

    selectTab(tab: RequestMasterDetailTab): void {
        if (this.activeTabId() === tab.id) {
            return;
        }

        this.activeTabId.set(tab.id);
        // replaceUrl: switching tabs on one chain is not a separate place for the back button to visit.
        this._router.navigate([], { relativeTo: this._route, fragment: tab.id, replaceUrl: true });
    }

    save(): void {
        if (!this.canEdit) {
            return;
        }

        this.saving.set(true);
        this._service
            .createOrEdit(this.requestMaster())
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe(() => {
                this.setSavedTitle(this.requestMaster());
                this._saved = this.copy(this.requestMaster());
                this.editing.set(false);
                this.notify.info(this.l('SavedSuccessfully'));
            });
    }

    goBack(): void {
        this._router.navigate(['/app/admin/request-masters']);
    }

    startEditing(): void {
        if (this.canEdit) {
            this.editing.set(true);
        }
    }

    /**
     * The list's Edit action opens this page with ?edit=true. Taken off once read, so a refresh or a shared
     * link opens the chain for viewing again rather than straight into editing.
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
            this.requestMaster.set(this.copy(this._saved));
        }
        this.editing.set(false);
    }

    /** A detached copy: the form writes into the object it is given, and the saved version must not follow it. */
    private copy(value: CreateOrEditRequestMasterDto): CreateOrEditRequestMasterDto {
        return CreateOrEditRequestMasterDto.fromJS(value.toJSON());
    }

    private setSavedTitle(requestMaster: CreateOrEditRequestMasterDto): void {
        this.savedCode.set(requestMaster?.code ?? '');
        this.savedName.set(requestMaster?.nameEn ?? '');
    }
}
