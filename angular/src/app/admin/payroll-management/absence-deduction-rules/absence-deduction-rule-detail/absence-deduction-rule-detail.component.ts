import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    AbsenceDeductionRulesServiceProxy,
    CreateOrEditAbsenceDeductionRuleDto,
} from '@shared/service-proxies/service-proxies';
import { finalize } from 'rxjs/operators';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { AbsenceDeductionRuleFormComponent } from '../absence-deduction-rule-form.component';
import { AbsenceDeductionRuleDetailsTabComponent } from './absence-deduction-rule-details-tab.component';

interface AbsenceDeductionRuleDetailTab {
    /** Goes in the URL fragment (`/absence-deduction-rules/12#details`), so a refresh keeps the tab. */
    id: string;
    labelKey: string;
    permission: string;
}

const ABSENCE_DEDUCTION_RULE_DETAIL_TABS: AbsenceDeductionRuleDetailTab[] = [
    { id: 'details', labelKey: 'AbsenceDedRuleDetails', permission: 'Pages.PayrollManagement.AbsenceDedRuleDetails' },
];

/**
 * One absence deduction rule: its own fields, editable in place, and its child lists as tabs below.
 * Only the active tab's grid is built.
 */
@Component({
    templateUrl: './absence-deduction-rule-detail.component.html',
    styleUrl: './absence-deduction-rule-detail.component.css',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        BusyIfDirective,
        ButtonBusyDirective,
        FormsModule,
        AbsenceDeductionRuleFormComponent,
        AbsenceDeductionRuleDetailsTabComponent,
        LocalizePipe,
    ],
})
export class AbsenceDeductionRuleDetailComponent extends AppComponentBase implements OnInit {
    private _route = inject(ActivatedRoute);
    private _router = inject(Router);
    private _absenceDeductionRulesService = inject(AbsenceDeductionRulesServiceProxy);

    readonly absenceDeductionRuleId = signal<number | undefined>(undefined);
    readonly absenceDeductionRule = signal<CreateOrEditAbsenceDeductionRuleDto>(
        new CreateOrEditAbsenceDeductionRuleDto()
    );
    readonly loaded = signal<boolean>(false);
    readonly loading = signal<boolean>(false);
    readonly saving = signal<boolean>(false);

    /**
     * Whether the fields can be changed. The page opens read-only -- looking a record up should not leave it one
     * keystroke from being changed -- and only a user with the Edit permission is offered the Edit button.
     */
    readonly editing = signal<boolean>(false);

    /** The record as last loaded or saved, so Cancel can put back whatever was typed since. */
    private _saved: CreateOrEditAbsenceDeductionRuleDto | undefined;

    /** Code and name as last loaded or saved, so the title does not change while the name is being typed. */
    readonly savedCode = signal<string>('');
    readonly savedName = signal<string>('');

    // Code is optional on absence deduction rules (auto-generated if empty), so fall back to the name alone.
    readonly title = computed(() =>
        this.savedCode()
            ? `${this.savedCode()} - ${this.savedName()}`
            : this.savedName() || this.l('AbsenceDeductionRules')
    );

    readonly canEdit = this.permission.isGranted('Pages.PayrollManagement.AbsenceDeductionRules.Edit');

    readonly tabs = ABSENCE_DEDUCTION_RULE_DETAIL_TABS.filter((tab) => this.permission.isGranted(tab.permission));
    readonly activeTabId = signal<string | undefined>(this.tabs[0]?.id);

    ngOnInit(): void {
        this._route.paramMap.subscribe((params) => {
            const id = Number(params.get('id'));
            if (id) {
                this.absenceDeductionRuleId.set(id);
                this.loadAbsenceDeductionRule(id);
            }
        });

        // Browser back/forward and a refresh both land on the tab named in the fragment.
        this._route.fragment.subscribe((fragment) => {
            const requested = this.tabs.find((tab) => tab.id === fragment);
            this.activeTabId.set((requested ?? this.tabs[0])?.id);
        });
    }

    loadAbsenceDeductionRule(id: number): void {
        this.loading.set(true);
        this._absenceDeductionRulesService
            .getAbsenceDeductionRuleForEdit(id)
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe((result) => {
                this.absenceDeductionRule.set(result.absenceDeductionRule);
                this._saved = this.copy(result.absenceDeductionRule);
                this.editing.set(this.canEdit && this._route.snapshot.queryParamMap.get('edit') === 'true');
                this.dropEditParam();
                this.setSavedTitle(result.absenceDeductionRule);
                this.loaded.set(true);
            });
    }

    selectTab(tab: AbsenceDeductionRuleDetailTab): void {
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
        this._absenceDeductionRulesService
            .createOrEdit(this.absenceDeductionRule())
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe(() => {
                this.setSavedTitle(this.absenceDeductionRule());
                this._saved = this.copy(this.absenceDeductionRule());
                this.editing.set(false);
                this.notify.info(this.l('SavedSuccessfully'));
            });
    }

    goBack(): void {
        this._router.navigate(['/app/admin/absence-deduction-rules']);
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
            this.absenceDeductionRule.set(this.copy(this._saved));
        }
        this.editing.set(false);
    }

    /** A detached copy: the form writes into the object it is given, and the saved version must not follow it. */
    private copy(value: CreateOrEditAbsenceDeductionRuleDto): CreateOrEditAbsenceDeductionRuleDto {
        return CreateOrEditAbsenceDeductionRuleDto.fromJS(value.toJSON());
    }

    private setSavedTitle(absenceDeductionRule: CreateOrEditAbsenceDeductionRuleDto): void {
        this.savedCode.set(absenceDeductionRule?.code ?? '');
        this.savedName.set(absenceDeductionRule?.ruleName ?? '');
    }
}
