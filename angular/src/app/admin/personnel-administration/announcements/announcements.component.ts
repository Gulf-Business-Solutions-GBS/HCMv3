import { Component, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { finalize } from 'rxjs/operators';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { NewsServiceProxy, NewsDto, GetAllNewsInput } from '@shared/service-proxies/service-proxies';
import { CreateOrEditAnnouncementModalComponent } from './create-or-edit-announcement-modal.component';

/**
 * Post and manage announcements -- the rows behind the dashboard's Announcements panel.
 *
 * The entity (News), its service and its permissions all existed; only this screen was missing, which
 * is why the panel had nothing to show.
 *
 * The list deliberately surfaces the ISSUE and EXPIRY dates as the primary columns. An announcement is
 * only visible on the dashboard between those two dates, so "why can nobody see my announcement" is
 * almost always answered by one of them, and burying them behind an edit dialog would make that
 * question need a support call.
 */
@Component({
    templateUrl: './announcements.component.html',
    styleUrl: './announcements.component.css',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        BusyIfDirective,
        FormsModule,
        LocalizePipe,
        DatePipe,
        CreateOrEditAnnouncementModalComponent,
    ],
})
export class AnnouncementsComponent extends AppComponentBase {
    private _newsService = inject(NewsServiceProxy);

    readonly createOrEditModal = viewChild<CreateOrEditAnnouncementModalComponent>('createOrEditModal');

    readonly isLoading = signal<boolean>(false);
    readonly items = signal<NewsDto[]>([]);
    readonly totalCount = signal<number>(0);

    readonly filter = signal<string>('');
    readonly skipCount = signal<number>(0);
    readonly pageSize = 20;

    constructor() {
        super();
        this.load();
    }

    load(): void {
        const input = new GetAllNewsInput();
        input.filter = this.filter();
        input.skipCount = this.skipCount();
        input.maxResultCount = this.pageSize;

        this.isLoading.set(true);
        this._newsService
            .getAll(input)
            .pipe(finalize(() => this.isLoading.set(false)))
            .subscribe((result) => {
                this.items.set(result.items ?? []);
                this.totalCount.set(result.totalCount ?? 0);
            });
    }

    search(): void {
        // Back to page one: staying on page 4 of the previous results shows an empty table and reads as
        // "your search found nothing".
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

    create(): void {
        this.createOrEditModal().show();
    }

    edit(item: NewsDto): void {
        this.createOrEditModal().show(item.id);
    }

    delete(item: NewsDto): void {
        this.message.confirm(this.l('AnnouncementDeleteWarning'), this.l('AreYouSure'), (isConfirmed) => {
            if (!isConfirmed) {
                return;
            }

            this.isLoading.set(true);
            this._newsService
                .delete(item.id)
                .pipe(finalize(() => this.isLoading.set(false)))
                .subscribe(() => {
                    this.notify.success(this.l('SuccessfullyDeleted'));
                    this.load();
                });
        });
    }

    /**
     * Whether this announcement is on the dashboard right now.
     *
     * Computed on the CLIENT from the dates rather than stored, because "live" is a function of the
     * current time -- a stored flag would need something to flip it at midnight and would be wrong in
     * between.
     */
    isLive(item: NewsDto): boolean {
        const now = Date.now();
        const issue = this.toDate(item.issueDate)?.getTime();
        const expiry = this.toDate(item.expiryDate)?.getTime();

        return !!issue && !!expiry && issue <= now && now <= expiry;
    }

    isExpired(item: NewsDto): boolean {
        const expiry = this.toDate(item.expiryDate)?.getTime();
        return !!expiry && expiry < Date.now();
    }

    /** Proxies type dates as luxon DateTime, which the date pipe refuses. */
    toDate(value: any): Date | null {
        if (!value) {
            return null;
        }

        return typeof value.toJSDate === 'function' ? value.toJSDate() : new Date(value);
    }
}
