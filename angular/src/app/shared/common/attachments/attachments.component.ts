import { ChangeDetectionStrategy, Component, OnChanges, OnInit, inject, input, output, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { AppConsts } from '@shared/AppConsts';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import { FileDownloadService } from '@shared/utils/file-download.service';
import {
    AddAttachmentInput,
    AttachmentDto,
    AttachmentOwnerInput,
    AttachmentServiceProxy,
    AttachmentSettingsDto,
} from '@shared/service-proxies/service-proxies';
import { Observable, firstValueFrom } from 'rxjs';
import { finalize, map } from 'rxjs/operators';

/**
 * Files attached to one record, for any screen: `<app-attachments entityType="EmployeeDocument" [entityId]="id">`.
 *
 * <p>Two modes. With an `entityId` the files are loaded, and added or removed at once (if the server says the
 * caller may). Without one -- a form for a record that does not exist yet -- the chosen files are held in the
 * browser, and the parent calls {@link attachPending} with the new record's id after saving it. Held rather than
 * uploaded early because an upload waits in the server's temp cache for only a minute.</p>
 *
 * <p>Who may see or change the files is decided on the server per entity type; this component only reflects
 * the answer (`canEdit`).</p>
 */
@Component({
    selector: 'app-attachments',
    templateUrl: './attachments.component.html',
    imports: [LocalizePipe, LuxonFormatPipe],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AttachmentsComponent extends AppComponentBase implements OnInit, OnChanges {
    private readonly _service = inject(AttachmentServiceProxy);
    private readonly _http = inject(HttpClient);
    private readonly _download = inject(FileDownloadService);

    readonly entityType = input.required<string>();
    readonly entityId = input<number | string | null | undefined>(undefined);
    /** Forces read-only even when the server would allow changes (e.g. a view-only dialog). */
    readonly readonly = input(false);

    /** Whether the "allowed types, up to N MB" line is shown. A crowded form can leave it out: the picker
     *  itself only offers allowed types, and a file that breaks a limit is still refused with the reason. */
    readonly showLimitsHint = input(true);

    /** Emits the number of files attached (or pending) whenever it changes. */
    readonly countChange = output<number>();

    readonly settings = signal<AttachmentSettingsDto | null>(null);
    readonly items = signal<AttachmentDto[]>([]);
    readonly pending = signal<File[]>([]);
    readonly canEdit = signal(false);
    readonly loading = signal(false);
    readonly uploading = signal(false);

    ngOnInit(): void {
        this._service.getAttachmentSettings().subscribe((s) => this.settings.set(s));
    }

    ngOnChanges(): void {
        this.reload();
    }

    get isPendingMode(): boolean {
        const id = this.entityId();
        return id === null || id === undefined || id === '';
    }

    get acceptList(): string {
        return (this.settings()?.allowedExtensions ?? []).join(',');
    }

    get limitsHint(): string {
        const s = this.settings();
        return s ? this.l('AttachmentLimitsHint', s.allowedExtensions.join(', '), s.maxFileSizeMB) : '';
    }

    reload(): void {
        if (this.isPendingMode) {
            this.items.set([]);
            this.canEdit.set(!this.readonly());
            return;
        }

        this.loading.set(true);
        this._service
            .getAttachments(this.ownerInput())
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe((result) => {
                this.items.set(result.items ?? []);
                this.canEdit.set(result.canEdit && !this.readonly());
                this.countChange.emit(this.items().length);
            });
    }

    onFilesChosen(event: Event): void {
        const inputEl = event.target as HTMLInputElement;
        const files = Array.from(inputEl.files ?? []);
        inputEl.value = '';

        const accepted = files.filter((f) => this.checkLocally(f));
        if (accepted.length === 0) {
            return;
        }

        if (this.isPendingMode) {
            this.pending.update((list) => [...list, ...accepted]);
            this.countChange.emit(this.pending().length);
            return;
        }

        void this.uploadAll(accepted, this.entityId()!).then(() => this.reload());
    }

    removePending(file: File): void {
        this.pending.update((list) => list.filter((f) => f !== file));
        this.countChange.emit(this.pending().length);
    }

    /**
     * Attaches the files held in pending mode to the record just saved. Resolves once every file has been
     * tried; a file that fails is reported and skipped, so one bad file does not lose the others.
     */
    async attachPending(entityId: number | string): Promise<void> {
        const files = this.pending();
        if (files.length === 0) {
            return;
        }

        await this.uploadAll(files, entityId);
        this.pending.set([]);
    }

    /**
     * Uploads the files held in pending mode to the server's temp cache and hands back their tokens, WITHOUT
     * attaching them. For a record whose server-side save attaches the files itself -- a leave whose type
     * requires a document has to carry it at the moment it is submitted, not afterwards.
     */
    async uploadPendingToTokens(): Promise<string[]> {
        const tokens: string[] = [];
        this.uploading.set(true);
        try {
            for (const file of this.pending()) {
                try {
                    tokens.push(await firstValueFrom(this.uploadToTemp(file)));
                } catch (error: any) {
                    const reason = error?.error?.error?.message ?? error?.message ?? '';
                    this.notify.error(this.l('AttachmentUploadFailed', file.name, reason));
                }
            }
        } finally {
            this.uploading.set(false);
        }

        return tokens;
    }

    clearPending(): void {
        this.pending.set([]);
        this.countChange.emit(0);
    }

    download(item: AttachmentDto): void {
        this._service.getAttachmentDownload(item.id).subscribe((file) => this._download.downloadTempFile(file));
    }

    remove(item: AttachmentDto): void {
        this.message.confirm(item.fileName, this.l('AreYouSure'), (confirmed) => {
            if (!confirmed) {
                return;
            }

            this._service.deleteAttachment(item.id).subscribe(() => {
                this.notify.success(this.l('SuccessfullyDeleted'));
                this.reload();
            });
        });
    }

    formatSize(bytes: number): string {
        if (bytes >= 1024 * 1024) {
            return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
        }

        return Math.max(1, Math.round(bytes / 1024)) + ' KB';
    }

    private async uploadAll(files: File[], entityId: number | string): Promise<void> {
        this.uploading.set(true);
        try {
            for (const file of files) {
                try {
                    const token = await firstValueFrom(this.uploadToTemp(file));
                    const input = new AddAttachmentInput();
                    input.entityType = this.entityType();
                    input.entityId = String(entityId);
                    input.fileToken = token;
                    await firstValueFrom(this._service.addAttachment(input));
                } catch (error: any) {
                    const reason = error?.error?.error?.message ?? error?.message ?? '';
                    this.notify.error(this.l('AttachmentUploadFailed', file.name, reason));
                }
            }
        } finally {
            this.uploading.set(false);
        }
    }

    /** First half of the upload: the file goes to the server's temp cache and comes back as a token. */
    private uploadToTemp(file: File): Observable<string> {
        const form = new FormData();
        form.append('file', file, file.name);

        return this._http.post<any>(AppConsts.remoteServiceBaseUrl + '/Attachment/UploadFile', form).pipe(
            map((response) => {
                if (response?.success === false || !response?.result?.fileToken) {
                    throw new Error(response?.error?.message ?? '');
                }
                return response.result.fileToken as string;
            })
        );
    }

    /** Same checks the server makes, so an obviously wrong file is refused before it is sent. */
    private checkLocally(file: File): boolean {
        const s = this.settings();
        if (!s) {
            return true;
        }

        const dot = file.name.lastIndexOf('.');
        const ext = dot >= 0 ? file.name.substring(dot).toLowerCase() : '';
        if (!s.allowedExtensions.includes(ext)) {
            this.message.warn(this.l('AttachmentFileTypeNotAllowed', ext || '-', s.allowedExtensions.join(', ')));
            return false;
        }

        if (file.size > s.maxFileSizeMB * 1024 * 1024) {
            this.message.warn(this.l('AttachmentTooLarge', s.maxFileSizeMB));
            return false;
        }

        return true;
    }

    private ownerInput(): AttachmentOwnerInput {
        const input = new AttachmentOwnerInput();
        input.entityType = this.entityType();
        input.entityId = String(this.entityId());
        return input;
    }
}
