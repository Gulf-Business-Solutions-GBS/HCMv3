import {
    AfterViewInit,
    Component,
    DestroyRef,
    ElementRef,
    HostListener,
    OnInit,
    ViewContainerRef,
    computed,
    inject,
    signal,
    viewChild,
} from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import {
    EmployeeStatus,
    EmployeeWorkbenchHeaderDto,
    EmployeeWorkbenchServiceProxy,
    SearchWorkbenchEmployeesInput,
    WorkbenchEmployeeSearchItemDto,
} from '@shared/service-proxies/service-proxies';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import { Subject, Subscription } from 'rxjs';
import { debounceTime, finalize } from 'rxjs/operators';
import { WORKBENCH_TILES, WorkbenchSubView, WorkbenchTile } from './employee-workbench.tiles';
import { WorkbenchGridComponent } from './workbench-grid.component';

export type WorkbenchViewMode = 'drawer' | 'popup' | 'split';

const VIEW_MODES: WorkbenchViewMode[] = ['drawer', 'popup', 'split'];

/** Per browser, as agreed: a convenience, not a setting anyone else needs to see. */
const VIEW_MODE_STORAGE_KEY = 'hcmv3.employeeWorkbench.viewMode';

const SEARCH_DEBOUNCE_MS = 300;

/** Tiles never shrink below this, however short the window. */
const MIN_TILE_HEIGHT_PX = 160;

/** Space kept under the second row of tiles so the page does not scroll by a few pixels. */
const TILE_BOTTOM_GAP_PX = 24;

/** Tiles stack one per row below this width; their height then stops following the window. */
const STACKED_BREAKPOINT_PX = 768;

/**
 * Employee Workbench: find one employee, then work on them from six tiles.
 *
 * A tile shows nothing but its icon and name until it is clicked. Its details open as a side drawer, a
 * popup or a split view -- each user's own choice, kept in the browser. Inside, each sub-link is a
 * `WorkbenchGridComponent` driven by `WORKBENCH_TILES`.
 *
 * A tile appears only when the user may see at least one of its sub-links, so the same screen is an HR
 * workbench for one person and a payroll workbench for another.
 */
@Component({
    templateUrl: './employee-workbench.component.html',
    styleUrls: ['./employee-workbench.component.less'],
    animations: [appModuleAnimation()],
    imports: [FormsModule, NgTemplateOutlet, SubHeaderComponent, LocalizePipe, LuxonFormatPipe, WorkbenchGridComponent],
})
export class EmployeeWorkbenchComponent extends AppComponentBase implements OnInit, AfterViewInit {
    private readonly _service = inject(EmployeeWorkbenchServiceProxy);
    private readonly _route = inject(ActivatedRoute);
    private readonly _router = inject(Router);

    readonly tilesGrid = viewChild<ElementRef<HTMLElement>>('tilesGrid');
    // The anchor is named differently from this property on purpose: a template reference variable shadows a
    // component member of the same name, so with both called "formHost" the template read the <ng-container>
    // element instead of this signal, and "formHost()" would not compile.
    readonly formHost = viewChild.required('formHostAnchor', { read: ViewContainerRef });

    readonly viewModes = VIEW_MODES;
    readonly exEmployeeStatus = EmployeeStatus.Inactive;
    readonly isArabic = abp.localization.currentLanguage.name?.startsWith('ar') ?? false;

    readonly searchText = signal('');
    readonly includeExEmployees = signal(false);
    readonly searchResults = signal<WorkbenchEmployeeSearchItemDto[]>([]);
    readonly searching = signal(false);
    readonly showResults = signal(false);

    readonly employee = signal<EmployeeWorkbenchHeaderDto | null>(null);
    readonly loadingEmployee = signal(false);

    readonly viewMode = signal<WorkbenchViewMode>(this.readViewMode());
    readonly openTileId = signal<string | null>(null);
    readonly activeSubViewId = signal<string | null>(null);
    readonly tileHeight = signal(MIN_TILE_HEIGHT_PX);

    /** Tiles, each narrowed to the sub-links this user may see; a tile with none left is not shown. */
    readonly tiles = computed<WorkbenchTile[]>(() =>
        WORKBENCH_TILES.map((tile) => ({
            ...tile,
            subViews: tile.subViews.filter((view) => this.permission.isGranted(view.permission)),
        })).filter((tile) => tile.subViews.length > 0),
    );

    readonly openTile = computed(() => this.tiles().find((t) => t.id === this.openTileId()) ?? null);

    readonly activeSubView = computed<WorkbenchSubView | null>(() => {
        const tile = this.openTile();
        return tile?.subViews.find((v) => v.id === this.activeSubViewId()) ?? tile?.subViews[0] ?? null;
    });

    readonly isExEmployee = computed(() => this.employee()?.status === EmployeeStatus.Inactive);

    readonly isSplitOpen = computed(() => this.viewMode() === 'split' && !!this.openTile());

    private readonly _searchInput = new Subject<void>();
    private _search: Subscription | undefined;

    constructor() {
        super();

        const debounced = this._searchInput.pipe(debounceTime(SEARCH_DEBOUNCE_MS)).subscribe(() => this.search());
        inject(DestroyRef).onDestroy(() => {
            debounced.unsubscribe();
            this._search?.unsubscribe();
        });
    }

    ngOnInit(): void {
        // The employee is in the URL, so a refresh or a shared link comes back to the same person.
        const employeeId = Number(this._route.snapshot.queryParamMap.get('employeeId'));
        if (employeeId > 0) {
            this.loadEmployee(employeeId);
        }
    }

    ngAfterViewInit(): void {
        this.fitTiles();
    }

    @HostListener('window:resize')
    fitTiles(): void {
        const grid = this.tilesGrid()?.nativeElement;
        if (!grid || window.innerWidth < STACKED_BREAKPOINT_PX) {
            this.tileHeight.set(MIN_TILE_HEIGHT_PX);
            return;
        }

        // The rows of tiles share whatever the window has left below the search and the header strip:
        // two rows of three on a wide screen, three rows of two on a narrower one (see the stylesheet).
        const rows = window.innerWidth < 1200 ? 3 : 2;
        const gap = parseFloat(getComputedStyle(grid).rowGap) || 0;
        const available = window.innerHeight - grid.getBoundingClientRect().top + window.scrollY - TILE_BOTTOM_GAP_PX;
        this.tileHeight.set(Math.max(MIN_TILE_HEIGHT_PX, Math.floor((available - gap * (rows - 1)) / rows)));
    }

    @HostListener('document:keydown.escape')
    onEscape(): void {
        // A form open on top of the drawer takes Escape for itself; closing the drawer under it would
        // leave the form orphaned over an empty page.
        if (document.querySelector('.modal.show')) {
            return;
        }
        if (this.openTile() && this.viewMode() !== 'split') {
            this.closeTile();
        }
    }

    onSearchChanged(text: string): void {
        this.searchText.set(text);
        this.showResults.set(true);
        this._searchInput.next();
    }

    onIncludeExEmployeesChanged(value: boolean): void {
        this.includeExEmployees.set(value);
        if (this.searchText().trim()) {
            this.showResults.set(true);
            this.search();
        }
    }

    search(): void {
        const filter = this.searchText().trim();
        this._search?.unsubscribe();

        if (!filter) {
            this.searchResults.set([]);
            this.searching.set(false);
            return;
        }

        const input = new SearchWorkbenchEmployeesInput({
            filter,
            includeExEmployees: this.includeExEmployees(),
            maxResultCount: 20,
        });

        this.searching.set(true);
        this._search = this._service
            .searchEmployees(input)
            .pipe(finalize(() => this.searching.set(false)))
            .subscribe((result) => this.searchResults.set(result.items));
    }

    pickFirstResult(): void {
        const first = this.searchResults()[0];
        if (first) {
            this.pick(first);
        }
    }

    pick(item: WorkbenchEmployeeSearchItemDto): void {
        this.showResults.set(false);
        this.searchText.set('');
        this.searchResults.set([]);
        this._router.navigate([], {
            relativeTo: this._route,
            queryParams: { employeeId: item.id },
            replaceUrl: true,
        });
        this.loadEmployee(item.id);
    }

    hideResultsSoon(): void {
        // After the click on a result has landed, not before it.
        setTimeout(() => this.showResults.set(false), 200);
    }

    loadEmployee(id: number): void {
        this.loadingEmployee.set(true);
        this.closeTile();
        this._service
            .getHeader(id)
            .pipe(finalize(() => this.loadingEmployee.set(false)))
            .subscribe((header) => {
                this.employee.set(header);
                // The header strip has just changed height, which moves the tiles.
                setTimeout(() => this.fitTiles());
            });
    }

    openTileById(id: string): void {
        this.openTileId.set(id);
        this.activeSubViewId.set(null);
    }

    closeTile(): void {
        this.openTileId.set(null);
        this.activeSubViewId.set(null);
    }

    setViewMode(mode: WorkbenchViewMode): void {
        this.viewMode.set(mode);
        try {
            localStorage.setItem(VIEW_MODE_STORAGE_KEY, mode);
        } catch {
            // Private windows and blocked storage: the choice just lasts until the page is left.
        }
        setTimeout(() => this.fitTiles());
    }

    name(en: string | undefined, ar: string | undefined): string {
        return (this.isArabic ? ar || en : en || ar) ?? '';
    }

    initials(): string {
        const name = this.employee()?.fullNameEn ?? '';
        return name
            .split(/\s+/)
            .filter((part) => !!part)
            .slice(0, 2)
            .map((part) => part[0].toUpperCase())
            .join('');
    }

    private readViewMode(): WorkbenchViewMode {
        try {
            const stored = localStorage.getItem(VIEW_MODE_STORAGE_KEY) as WorkbenchViewMode;
            return VIEW_MODES.includes(stored) ? stored : 'drawer';
        } catch {
            return 'drawer';
        }
    }
}
