import { Component, inject, output, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { DateTime } from 'luxon';
import { AppComponentBase } from '@shared/common/app-component-base';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { NewsServiceProxy, CreateOrEditNewsDto } from '@shared/service-proxies/service-proxies';

/**
 * Post or amend an announcement.
 *
 * ISSUE AND EXPIRY ARE REQUIRED, and default to today and a month out. The dashboard only shows an
 * announcement between those dates, so leaving either blank produces something that silently never
 * appears -- the most common way a notice board quietly stops working.
 */
@Component({
    selector: 'createOrEditAnnouncementModal',
    templateUrl: './create-or-edit-announcement-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditAnnouncementModalComponent extends AppComponentBase {
    private _newsService = inject(NewsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly announcement = signal<CreateOrEditNewsDto>(new CreateOrEditNewsDto());

    show(id?: number): void {
        this.active.set(true);

        if (!id) {
            const item = new CreateOrEditNewsDto();
            item.issueDate = DateTime.now() as any;
            // A month is a guess, but a defensible one: it is long enough to be useful and short enough
            // that a forgotten announcement drops off rather than lingering for a year.
            item.expiryDate = DateTime.now().plus({ months: 1 }) as any;
            item.sendNotification = false;
            item.sendEmail = false;
            this.announcement.set(item);
            this.modal().show();
            return;
        }

        this._newsService.getNewsForEdit(id).subscribe((result) => {
            this.announcement.set(result.news);
            this.modal().show();
        });
    }

    save(): void {
        this.saving.set(true);
        this._newsService
            .createOrEdit(this.announcement())
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe(() => {
                this.notify.info(this.l('SavedSuccessfully'));
                this.close();
                this.modalSave.emit();
            });
    }

    close(): void {
        this.active.set(false);
        this.modal().hide();
    }

    updateField(field: keyof CreateOrEditNewsDto, value: any): void {
        const next = this.announcement();
        (next as any)[field] = value;
        this.announcement.set(next);
    }

    // Dates: the proxies use luxon DateTime and <input type="date"> wants 'YYYY-MM-DD'. Converting in
    // both directions here keeps the template free of the any-casts that hide real type errors.

    dateText(value: any): string {
        if (!value) {
            return '';
        }

        const dt = value instanceof DateTime ? value : DateTime.fromISO(String(value));
        return dt.isValid ? dt.toFormat('yyyy-MM-dd') : '';
    }

    setDate(field: 'issueDate' | 'expiryDate', value: string): void {
        this.updateField(field, value ? (DateTime.fromISO(value) as any) : (undefined as any));
    }

    /**
     * Expiry before issue means the announcement can never appear. Blocked at save rather than
     * explained afterwards, because the failure is invisible: it saves cleanly and simply never shows.
     */
    get isDateRangeValid(): boolean {
        const issue = this.dateText(this.announcement().issueDate);
        const expiry = this.dateText(this.announcement().expiryDate);

        return !!issue && !!expiry && issue <= expiry;
    }
}
