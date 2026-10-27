import { Component, inject, signal, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import {
    ScreensServiceProxy,
    SharedQuickLinkDto,
    ScreenDto,
    GetAllScreensInput,
    CreateOrEditSharedQuickLinkInput,
} from '@shared/service-proxies/service-proxies';

/**
 * Curate the tenant-wide quick links shown in the "Shared" band of everyone's dashboard.
 *
 * SHARED LINKS ONLY, NOT SHARED FAVOURITES. An administrator may offer a link; they may not reach into
 * somebody's personal favourites and pin something there. That distinction was decided deliberately and
 * this screen is the whole of the administrator's reach.
 *
 * The list is not permission-filtered, unlike the dashboard's copy: an administrator has to see a link
 * they published to a screen they personally cannot open, or they would be unable to remove it.
 */
@Component({
    templateUrl: './quick-links-admin.component.html',
    styleUrl: './quick-links-admin.component.css',
    animations: [appModuleAnimation()],
    imports: [SubHeaderComponent, BusyIfDirective, FormsModule, LocalizePipe],
})
export class QuickLinksAdminComponent extends AppComponentBase {
    private _screensService = inject(ScreensServiceProxy);

    readonly isLoading = signal<boolean>(false);
    readonly links = signal<SharedQuickLinkDto[]>([]);

    /** Screen picker for adding a link. */
    readonly screenSearch = signal<string>('');
    readonly searchResults = signal<ScreenDto[]>([]);
    readonly isSearching = signal<boolean>(false);

    /**
     * Six are shown on the dashboard. Publishing more is allowed -- an administrator may be mid-edit,
     * or keeping a spare below the line -- but they need to be told which ones will actually appear.
     */
    readonly visibleCap = 6;
    readonly hasHiddenLinks = computed(() => this.links().length > this.visibleCap);

    constructor() {
        super();
        this.load();
    }

    load(): void {
        this.isLoading.set(true);
        this._screensService
            .getSharedQuickLinks()
            .pipe(finalize(() => this.isLoading.set(false)))
            .subscribe((result) => this.links.set(result ?? []));
    }

    searchScreens(): void {
        const term = this.screenSearch().trim();
        if (!term) {
            this.searchResults.set([]);
            return;
        }

        const input = new GetAllScreensInput();
        input.filter = term;
        input.maxResultCount = 10;
        input.skipCount = 0;

        this.isSearching.set(true);
        this._screensService
            .getAllScreens(input)
            .pipe(finalize(() => this.isSearching.set(false)))
            .subscribe((result) => this.searchResults.set(result.items ?? []));
    }

    add(screen: ScreenDto): void {
        if (this.links().some((l) => l.screenKey === screen.screenKey)) {
            // Already published. A second row would render the same tile twice.
            this.notify.info(this.l('QuickLinkAlreadyPublished'));
            return;
        }

        const input = new CreateOrEditSharedQuickLinkInput();
        input.screenKey = screen.screenKey;
        // Appended, not inserted: a new link should not displace an order the administrator set.
        input.displayOrder = this.links().length;

        this.save(input, () => {
            this.screenSearch.set('');
            this.searchResults.set([]);
        });
    }

    moveUp(index: number): void {
        if (index === 0) {
            return;
        }

        this.swap(index, index - 1);
    }

    moveDown(index: number): void {
        if (index >= this.links().length - 1) {
            return;
        }

        this.swap(index, index + 1);
    }

    remove(link: SharedQuickLinkDto): void {
        this.message.confirm(this.l('QuickLinkDeleteWarning'), this.l('AreYouSure'), (isConfirmed) => {
            if (!isConfirmed) {
                return;
            }

            this.isLoading.set(true);
            this._screensService
                .deleteSharedQuickLink(link.id)
                .pipe(finalize(() => this.isLoading.set(false)))
                .subscribe(() => {
                    this.notify.success(this.l('SuccessfullyDeleted'));
                    this.load();
                });
        });
    }

    /**
     * Swaps two links' order by writing BOTH rows.
     *
     * Writing only the moved row would leave two links claiming the same DisplayOrder, and the
     * dashboard's ordering would then be decided by whatever the database returned first -- stable
     * enough in testing to look correct and arbitrary in production.
     */
    private swap(a: number, b: number): void {
        const links = [...this.links()];
        const first = links[a];
        const second = links[b];

        const firstInput = new CreateOrEditSharedQuickLinkInput();
        firstInput.id = first.id;
        firstInput.screenKey = first.screenKey;
        firstInput.displayOrder = b;

        const secondInput = new CreateOrEditSharedQuickLinkInput();
        secondInput.id = second.id;
        secondInput.screenKey = second.screenKey;
        secondInput.displayOrder = a;

        this.isLoading.set(true);
        this._screensService.createOrEditSharedQuickLink(firstInput).subscribe(() => {
            this._screensService
                .createOrEditSharedQuickLink(secondInput)
                .pipe(finalize(() => this.isLoading.set(false)))
                .subscribe(() => this.load());
        });
    }

    private save(input: CreateOrEditSharedQuickLinkInput, onDone: () => void): void {
        this.isLoading.set(true);
        this._screensService
            .createOrEditSharedQuickLink(input)
            .pipe(finalize(() => this.isLoading.set(false)))
            .subscribe(() => {
                this.notify.success(this.l('SavedSuccessfully'));
                onDone();
                this.load();
            });
    }
}
