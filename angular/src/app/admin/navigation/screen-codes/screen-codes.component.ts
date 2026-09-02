import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { ScreensServiceProxy, ScreenDto, GetAllScreensInput, UpdateScreenCodeInput } from '@shared/service-proxies/service-proxies';

/**
 * Re-map Screen Codes.
 *
 * WHY THIS SCREEN EXISTS. Codes are seeded automatically and are perfectly usable as generated, so the
 * only reason to edit one is vocabulary: a customer who has always called payroll "PR" should be able
 * to make SPY001 read their way rather than learn ours. That is a small need, but without this screen
 * the codes are ours permanently and the table might as well have been a constant.
 *
 * Edited in place rather than through a modal. There is exactly one editable field, and a dialog to
 * change one text box is ceremony that makes a five-second job feel like a transaction.
 */
@Component({
    templateUrl: './screen-codes.component.html',
    styleUrl: './screen-codes.component.css',
    animations: [appModuleAnimation()],
    imports: [SubHeaderComponent, BusyIfDirective, FormsModule, LocalizePipe],
})
export class ScreenCodesComponent extends AppComponentBase {
    private _screensService = inject(ScreensServiceProxy);

    readonly isLoading = signal<boolean>(false);
    readonly screens = signal<ScreenDto[]>([]);
    readonly totalCount = signal<number>(0);
    readonly modules = signal<string[]>([]);

    readonly filter = signal<string>('');
    readonly module = signal<string>('');
    readonly skipCount = signal<number>(0);
    readonly pageSize = 20;

    /** Which row is being edited, and the value typed so far. */
    readonly editingId = signal<number | undefined>(undefined);
    readonly editingCode = signal<string>('');

    constructor() {
        super();
        this.loadModules();
        this.load();
    }

    load(): void {
        const input = new GetAllScreensInput();
        input.filter = this.filter();
        input.module = this.module() || undefined;
        input.skipCount = this.skipCount();
        input.maxResultCount = this.pageSize;

        this.isLoading.set(true);
        this._screensService
            .getAllScreens(input)
            .pipe(finalize(() => this.isLoading.set(false)))
            .subscribe((result) => {
                this.screens.set(result.items ?? []);
                this.totalCount.set(result.totalCount ?? 0);
            });
    }

    search(): void {
        // Back to page one: staying on page 7 of the previous result set shows an empty table and looks
        // like the search found nothing.
        this.skipCount.set(0);
        this.load();
    }

    nextPage(): void {
        if (this.skipCount() + this.pageSize < this.totalCount()) {
            this.skipCount.set(this.skipCount() + this.pageSize);
            this.load();
        }
    }

    previousPage(): void {
        if (this.skipCount() > 0) {
            this.skipCount.set(Math.max(0, this.skipCount() - this.pageSize));
            this.load();
        }
    }

    startEdit(screen: ScreenDto): void {
        this.editingId.set(screen.id);
        this.editingCode.set(screen.code);
    }

    cancelEdit(): void {
        this.editingId.set(undefined);
        this.editingCode.set('');
    }

    saveEdit(screen: ScreenDto): void {
        const code = this.editingCode().trim();

        if (!code || code === screen.code) {
            // Nothing typed, or nothing changed. Sending it would ask the server to reject a duplicate
            // of the row's own code, which reads to the user as a spurious error.
            this.cancelEdit();
            return;
        }

        const input = new UpdateScreenCodeInput();
        input.id = screen.id;
        input.code = code;

        this.isLoading.set(true);
        this._screensService
            .updateScreenCode(input)
            .pipe(finalize(() => this.isLoading.set(false)))
            .subscribe(() => {
                this.notify.success(this.l('SavedSuccessfully'));
                this.cancelEdit();
                this.load();
            });
    }

    private loadModules(): void {
        this._screensService.getScreenModules().subscribe((result) => this.modules.set(result ?? []));
    }
}
