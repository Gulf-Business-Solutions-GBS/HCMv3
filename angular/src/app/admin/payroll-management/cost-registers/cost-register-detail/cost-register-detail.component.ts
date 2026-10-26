import { Component, OnInit, computed, inject, signal, viewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { CostRegisterDto, CostRegistersServiceProxy } from '@shared/service-proxies/service-proxies';
import { finalize } from 'rxjs/operators';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import { CreateOrEditCostRegisterModalComponent } from '../create-or-edit-cost-register-modal.component';
import { CostRegisterDetailsTabComponent } from './cost-register-details-tab.component';

interface RecordTab {
    /** Goes in the URL fragment (`/cost-registers/12#details`), so a refresh keeps the tab. */
    id: string;
    labelKey: string;
    permission: string;
}

const COST_REGISTER_TABS: RecordTab[] = [
    { id: 'details', labelKey: 'CostRegisterDetails', permission: 'Pages.PayrollManagement.CostRegisterDetails' },
];

/**
 * One cost register: its header read-only (Edit opens the usual dialog) and its cost distribution lines as a
 * tab below, filtered to this register. Same layout as the absence deduction rule page.
 */
@Component({
    templateUrl: './cost-register-detail.component.html',
    styleUrl: '../../../../shared/common/detail-tabs/detail-tabs.css',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        BusyIfDirective,
        CreateOrEditCostRegisterModalComponent,
        CostRegisterDetailsTabComponent,
        LocalizePipe,
        LuxonFormatPipe,
    ],
})
export class CostRegisterDetailComponent extends AppComponentBase implements OnInit {
    private _route = inject(ActivatedRoute);
    private _router = inject(Router);
    private _registersService = inject(CostRegistersServiceProxy);

    readonly editModal = viewChild<CreateOrEditCostRegisterModalComponent>('editModal');

    readonly registerId = signal<number | undefined>(undefined);
    readonly register = signal<CostRegisterDto | undefined>(undefined);
    readonly loading = signal<boolean>(false);

    readonly title = computed(() => {
        const register = this.register();
        return register?.employeeCode
            ? `${this.l('CostRegisters')} - ${register.employeeCode}`
            : this.l('CostRegisters');
    });

    readonly canEdit = this.permission.isGranted('Pages.PayrollManagement.CostRegisters.Edit');

    readonly tabs = COST_REGISTER_TABS.filter((tab) => this.permission.isGranted(tab.permission));
    readonly activeTabId = signal<string | undefined>(this.tabs[0]?.id);

    ngOnInit(): void {
        this._route.paramMap.subscribe((params) => {
            const id = Number(params.get('id'));
            if (id) {
                this.registerId.set(id);
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
        this._registersService
            .getCostRegister(id)
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe((result) => this.register.set(result));
    }

    selectTab(tab: RecordTab): void {
        if (this.activeTabId() === tab.id) {
            return;
        }
        this.activeTabId.set(tab.id);
        this._router.navigate([], { relativeTo: this._route, fragment: tab.id, replaceUrl: true });
    }

    edit(): void {
        if (this.canEdit && this.registerId()) {
            this.editModal().show(this.registerId());
        }
    }

    reload(): void {
        if (this.registerId()) {
            this.load(this.registerId());
        }
    }

    goBack(): void {
        this._router.navigate(['/app/admin/cost-registers']);
    }
}
