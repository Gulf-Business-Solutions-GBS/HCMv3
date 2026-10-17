import { Component, inject, OnInit, signal, computed, Type } from '@angular/core';
import { NgComponentOutlet } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { CreateOrEditEmployeeDto, EmployeesServiceProxy } from '@shared/service-proxies/service-proxies';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import {
    RecordBreadcrumbComponent,
    BreadcrumbStep,
} from '@app/shared/common/breadcrumb/record-breadcrumb.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import {
    DEFAULT_EMPLOYEE_SECTION_ID,
    EMPLOYEE_SECTION_CATEGORIES,
    EMPLOYEE_SECTIONS,
    EmployeeSection,
    EmployeeSectionCategory,
} from './employee-detail.sections';

/**
 * One employee's record: identity header, a searchable category rail, and the section in view.
 *
 * REPLACES A 65-TAB STRIP. The tabs wrapped onto six rows, so the control meant to orient you was the
 * thing you had to read past, and the section you wanted was found by scanning ~60 labels that all
 * began with the same word. Three things follow from that, and they are the whole design:
 *
 *  1. GROUPED, not listed. "Attendance Quotas" is findable because Time is a short list, not because
 *     you scanned everything. Categories come from the catalog, so adding a section is a data change.
 *
 *  2. SEARCHABLE. With 65 sections, browsing stops working long before the list stops growing. The box
 *     filters on the LOCALIZED label, because that is the word the reader is actually looking for.
 *
 *  3. ONE SECTION BUILT AT A TIME. The tabset instantiated all 65 child grids on load -- 65 tables,
 *     their paging state and their lookup calls -- to show one. Each section is now a dynamic import
 *     resolved on selection, so opening an employee costs the shell plus the section you asked for.
 *
 * The section id lives in the URL fragment (`/employees/12#loans`), so a particular section can be
 * linked to and survives a refresh. That is the one thing tabs did badly that people notice daily.
 */
@Component({
    templateUrl: './employee-detail.component.html',
    styleUrl: './employee-detail.component.css',
    animations: [appModuleAnimation()],
    imports: [SubHeaderComponent, LocalizePipe, FormsModule, NgComponentOutlet, RecordBreadcrumbComponent],
})
export class EmployeeDetailComponent extends AppComponentBase implements OnInit {
    private _route = inject(ActivatedRoute);
    private _router = inject(Router);
    private _employeesService = inject(EmployeesServiceProxy);

    readonly employeeId = signal<number | undefined>(undefined);
    readonly employee = signal<CreateOrEditEmployeeDto | undefined>(undefined);

    readonly activeSectionId = signal<string>(DEFAULT_EMPLOYEE_SECTION_ID);
    readonly activeComponent = signal<Type<unknown> | undefined>(undefined);
    readonly sectionLoadFailed = signal<boolean>(false);
    readonly isSectionLoading = signal<boolean>(false);

    readonly searchTerm = signal<string>('');

    readonly isRailCollapsed = signal<boolean>(this.readCollapsedPreference());

    /**
     * Every category, unfiltered -- the collapsed icon strip is a fixed set of landmarks, so hiding
     * icons because a search is still typed in the box would make the strip change shape for a reason
     * the reader cannot see while it is collapsed.
     */
    readonly allCategories = EMPLOYEE_SECTION_CATEGORIES;

    /** Inputs handed to whichever section component is mounted. Every section takes exactly this. */
    readonly sectionInputs = computed(() => ({ employeeId: this.employeeId() }));

    /**
     * Module > employee > section. This is exactly the case a breadcrumb is for: three levels deep,
     * where the page title alone ("Loans") cannot say whose loans, and the section rail shows where you
     * are within the record but not how you got into it.
     */
    readonly breadcrumb = computed<BreadcrumbStep[]>(() => {
        const steps: BreadcrumbStep[] = [
            { label: 'PersonnelAdministration', isKey: true, route: '/app/admin/employees' },
        ];

        const name = this.displayName();
        if (name) {
            // No route on the employee step: it would lead back to this same record, which is where the
            // reader already is.
            steps.push({ label: name });
        }

        const section = this.activeSection();
        if (section) {
            steps.push({ label: section.labelKey, isKey: true });
        }

        return steps;
    });

    readonly activeSection = computed(() =>
        EMPLOYEE_SECTIONS.find((s) => s.id === this.activeSectionId())
    );

    /**
     * Categories with their sections filtered by the search box and sorted A-Z, empty categories
     * dropped.
     *
     * Both the filter and the sort key are the LOCALIZED label, not the id: typing "quota" should find
     * what the reader sees on screen, and A-Z should be the reader's alphabet. Sorting here rather than
     * in the catalog is what makes that possible -- a list pre-sorted at authoring time would be in
     * English order whatever language the page is in, and `localeCompare` gets the collation right for
     * accented and non-Latin scripts where a plain `<` comparison does not.
     *
     * Category ORDER is deliberately left alone. It runs roughly in the order someone works through a
     * record (Personal, Job, Time, Pay...); alphabetising it would open the rail on Assets and bury
     * Personal in the middle, which is worse for the browsing case than it is better for the seeking
     * case -- and seeking is what the search box is for.
     */
    readonly visibleCategories = computed<EmployeeSectionCategory[]>(() => {
        const term = this.searchTerm().trim().toLowerCase();

        return EMPLOYEE_SECTION_CATEGORIES.map((category) => ({
            ...category,
            sections: category.sections
                .filter((s) => !term || this.l(s.labelKey).toLowerCase().includes(term))
                .sort((a, b) => this.l(a.labelKey).localeCompare(this.l(b.labelKey))),
        })).filter((category) => category.sections.length > 0);
    });

    readonly hasSearchResults = computed(() => this.visibleCategories().length > 0);

    ngOnInit(): void {
        this._route.paramMap.subscribe((params) => {
            const id = Number(params.get('id'));
            if (id) {
                this.employeeId.set(id);
                this.loadEmployee(id);
            }
        });

        // The fragment is the section. Read it on arrival so a link into a section lands there, and
        // keep listening so browser back/forward moves between sections rather than off the record.
        this._route.fragment.subscribe((fragment) => {
            const requested = EMPLOYEE_SECTIONS.find((s) => s.id === fragment);
            const fallback = EMPLOYEE_SECTIONS.find((s) => s.id === DEFAULT_EMPLOYEE_SECTION_ID);
            this.selectSection(requested ?? fallback ?? EMPLOYEE_SECTIONS[0], { updateUrl: false });
        });
    }

    selectSection(section: EmployeeSection, options: { updateUrl?: boolean } = {}): void {
        const { updateUrl = true } = options;

        if (updateUrl) {
            // replaceUrl: moving between sections of one record is not a separate place to go back to;
            // pushing each one would make the back button walk the sections instead of leaving.
            this._router.navigate([], {
                relativeTo: this._route,
                fragment: section.id,
                replaceUrl: true,
            });
        }

        if (this.activeSectionId() === section.id && this.activeComponent()) {
            return;
        }

        this.activeSectionId.set(section.id);
        // Drop the previous section straight away. Kept on screen until the next one arrived, it sat under
        // the new section's title -- and stayed there for good if that load failed.
        this.activeComponent.set(undefined);
        this.sectionLoadFailed.set(false);
        this.isSectionLoading.set(true);

        section
            .load()
            .then((component) => {
                // Guard against a slow chunk resolving after the reader moved on -- otherwise clicking
                // two sections quickly can leave the second one selected and the first one rendered.
                if (this.activeSectionId() === section.id) {
                    this.activeComponent.set(component);
                }
            })
            .catch(() => {
                if (this.activeSectionId() === section.id) {
                    this.sectionLoadFailed.set(true);
                }
            })
            .finally(() => {
                if (this.activeSectionId() === section.id) {
                    this.isSectionLoading.set(false);
                }
            });
    }

    clearSearch(): void {
        this.searchTerm.set('');
    }

    /**
     * Collapses the rail to a narrow strip, giving the section the full width -- several of these
     * grids are wide enough to scroll horizontally at 258px less than the page.
     *
     * The choice is remembered because it is a working preference, not a per-page one: somebody
     * comparing the same wide grid across a dozen employees should not have to collapse the rail on
     * every record.
     */
    /**
     * Collapsed, a category icon expands the rail and brings that category into view.
     *
     * The icons are a shortcut back INTO the rail rather than a navigation of their own: eight icons
     * cannot say which of Personal's ten sections you want, and picking one for you would be a guess.
     */
    expandTo(category: EmployeeSectionCategory): void {
        if (this.isRailCollapsed()) {
            this.toggleRail();
        }

        // The rail's sections do not exist in the DOM until the expanded state has rendered, so the
        // scroll has to wait a turn for them.
        setTimeout(() => {
            document
                .getElementById('emp-cat-' + category.id)
                ?.scrollIntoView({ block: 'start', behavior: 'smooth' });
        });
    }

    toggleRail(): void {
        const collapsed = !this.isRailCollapsed();
        this.isRailCollapsed.set(collapsed);

        try {
            localStorage.setItem(EmployeeDetailComponent.RailCollapsedStorageKey, collapsed ? '1' : '0');
        } catch {
            // Private windows and blocked site data make this throw. Losing the preference is a fair
            // outcome; failing the click is not.
        }
    }

    private static readonly RailCollapsedStorageKey = 'hcmv3.employee-detail.rail-collapsed';

    private readCollapsedPreference(): boolean {
        try {
            return localStorage.getItem(EmployeeDetailComponent.RailCollapsedStorageKey) === '1';
        } catch {
            // Expanded is the safer default: it is the state in which everything is reachable.
            return false;
        }
    }

    /** "1234 - Yusuf Al-Rashed", or just the name until the record arrives. */
    displayName(): string {
        const employee = this.employee();
        if (!employee) {
            return '';
        }

        return [employee.firstNameEn, employee.lastNameEn].filter((p) => !!p).join(' ');
    }

    private loadEmployee(id: number): void {
        this._employeesService.getEmployeeForEdit(id).subscribe((result) => {
            this.employee.set(result.employee);
        });
    }
}
