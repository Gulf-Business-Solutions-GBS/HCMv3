import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { TableModule, TableLazyLoadEvent } from 'primeng/table';
import { Select } from 'primeng/select';
import { Tooltip } from 'primeng/tooltip';
import { finalize } from 'rxjs/operators';
import { Subscription, interval } from 'rxjs';
import { SubHeaderComponent } from '../../../shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '../../../../shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { LuxonFormatPipe } from '../../../../shared/utils/luxon-format.pipe';

// PROXY REGENERATION REQUIRED.
// BulkImportExportServiceProxy / BulkImportEntityDto / BulkExportInput / StartBulkImportInput /
// BulkImportStatusOutput / BulkUploadLogEntryDto / GetBulkUploadLogEntriesInput / BulkImportType /
// StartProfilePhotoImportInput / ProfilePhotoFileDto / ImportTimesheetInput do not exist in
// service-proxies.ts yet -- the backend for this screen
// (HCMv3.Application/ImportExport/BulkImportExportAppService.cs) was added in the same change and NSwag
// regeneration was deliberately NOT run because several agents were editing service-proxies.ts
// concurrently. Run `angular/nswag/refresh.bat` against a running Web.Host once, then this file compiles
// as-is.
import {
    BulkExportInput,
    BulkImportEntityDto,
    BulkImportExportServiceProxy,
    BulkImportStatusOutput,
    BulkImportType,
    BulkUploadLogEntryDto,
    GetBulkUploadLogEntriesInput,
    ImportTimesheetInput,
    ProfilePhotoFileDto,
    StartBulkImportInput,
    StartProfilePhotoImportInput,
} from '@shared/service-proxies/service-proxies';

/** Legacy cmbAction items, verbatim (uscBulkImportExport.BindAction, lines 810-813). */
type BulkAction = 'validate' | 'upload' | 'template' | 'data';

/**
 * Bulk Import / Export screen.
 *
 * Ported from legacy HCM10.Web/UserControls/ImportExport/uscBulkImportExport.ascx (+ uscBulkImport for the
 * profile-photo tab and uscImportTimesheet for the timesheet tab). The fourth control in that folder,
 * uscImportExport.ascx.cs, is dead code -- ~860 of its 922 lines are commented out and the remainder are
 * empty lifecycle stubs -- so it contributes nothing here.
 *
 * Legacy's DevExpress presentation layer is NOT reproduced: Page_Load's hand-written Arabic/English label
 * swapping is replaced by the localize pipe, HideShowControl()'s manual Visible toggling by @if blocks, and
 * the ASPxCallbackPanel + timer1 polling by the pollStatus() interval below.
 */
@Component({
    templateUrl: './bulk-import-export.component.html',
    animations: [appModuleAnimation()],
    // Every directive/pipe used by the template must be listed here -- `tsc --noEmit` does not catch a
    // missing one; only a real `ng build` does.
    imports: [
        SubHeaderComponent,
        BusyIfDirective,
        FormsModule,
        TableModule,
        Select,
        Tooltip,
        LocalizePipe,
        PermissionPipe,
        LuxonFormatPipe,
    ],
})
export class BulkImportExportComponent extends AppComponentBase implements OnInit {
    private _service = inject(BulkImportExportServiceProxy);

    // ---- Tabs (one per legacy control) -------------------------------------------------------------
    readonly activeTab = signal<'bulk' | 'photos' | 'timesheet'>('bulk');

    // ---- Bulk import/export state (legacy cmbAction / cmbModule / cmbImportType) --------------------
    readonly entities = signal<BulkImportEntityDto[]>([]);
    readonly selectedEntityFullName = signal<string | undefined>(undefined);
    readonly action = signal<BulkAction>('validate');
    readonly importType = signal<BulkImportType>(BulkImportType.Incremental);
    readonly selectedFileName = signal<string>('');
    private selectedFileBase64: string | undefined;

    // ---- Status panel (legacy CalculateProcessStatus) ----------------------------------------------
    readonly status = signal<BulkImportStatusOutput | undefined>(undefined);
    readonly busy = signal<boolean>(false);
    private pollSubscription: Subscription | undefined;

    // ---- Log grid (legacy btnSuccess / btnFailed / btnAll "Show Details") --------------------------
    readonly logEntries = signal<BulkUploadLogEntryDto[]>([]);
    readonly logTotalCount = signal<number>(0);
    readonly logStatusFilter = signal<number | undefined>(undefined);
    readonly logLoading = signal<boolean>(false);

    // ---- Profile photo tab (legacy uscBulkImport) -------------------------------------------------
    readonly photoFiles = signal<ProfilePhotoFileDto[]>([]);

    // ---- Timesheet tab (legacy uscImportTimesheet) ------------------------------------------------
    readonly timesheetFileName = signal<string>('');
    private timesheetFileBase64: string | undefined;
    readonly timesheetErrors = signal<string[]>([]);

    /** Legacy BindAction(). The four items are exactly legacy's, in legacy's order. */
    readonly actionOptions = [
        { label: this.l('ValidateData'), value: 'validate' as BulkAction },
        { label: this.l('UploadData'), value: 'upload' as BulkAction },
        { label: this.l('DownloadTemplate'), value: 'template' as BulkAction },
        { label: this.l('DownloadData'), value: 'data' as BulkAction },
    ];

    /** Legacy BindImportType(): ("Incremental","I") and ("Purge","P"). */
    readonly importTypeOptions = [
        { label: this.l('Incremental'), value: BulkImportType.Incremental },
        { label: this.l('Purge'), value: BulkImportType.Purge },
    ];

    /** Legacy HideShowControl(): the upload box + import type only show for the two import actions. */
    readonly isImportAction = computed(() => this.action() === 'validate' || this.action() === 'upload');
    readonly showImportType = computed(() => this.action() === 'upload');

    readonly selectedEntity = computed(() => this.entities().find((e) => e.fullName === this.selectedEntityFullName()));

    readonly canRunImport = computed(
        () => !this.busy() && !!this.selectedEntityFullName() && !!this.selectedFileBase64
    );

    readonly canDownload = computed(() => !this.busy() && !!this.selectedEntityFullName());

    ngOnInit(): void {
        this._service.getBulkImportEntities().subscribe((result) => {
            this.entities.set(result.entities ?? []);
        });

        this.refreshStatus();
    }

    // ================================================================================================
    // File selection
    // ================================================================================================

    /**
     * Legacy ASPxUploadControl1_FileUploadComplete stashed the upload in Session and
     * GetFilePath() then wrote it under ~/TempExcel (deleting and recreating that whole directory each
     * time -- a race between concurrent users). Nothing is written to disk here; the bytes are base64'd
     * into the request.
     */
    onFileSelected(event: Event): void {
        const input = event.target as HTMLInputElement;
        const file = input.files?.[0];
        if (!file) {
            return;
        }

        this.selectedFileName.set(file.name);
        this.readAsBase64(file).then((base64) => (this.selectedFileBase64 = base64));
    }

    onTimesheetFileSelected(event: Event): void {
        const input = event.target as HTMLInputElement;
        const file = input.files?.[0];
        if (!file) {
            return;
        }

        this.timesheetFileName.set(file.name);
        this.readAsBase64(file).then((base64) => (this.timesheetFileBase64 = base64));
    }

    /** Legacy read a whole server FOLDER of {EmployeeCode}.jpg files; a browser can only offer a
     * multi-select, so the operator selects the images instead. */
    onPhotoFilesSelected(event: Event): void {
        const input = event.target as HTMLInputElement;
        const files = Array.from(input.files ?? []);
        if (files.length === 0) {
            return;
        }

        Promise.all(
            files.map(async (file) => {
                const dto = new ProfilePhotoFileDto();
                dto.fileName = file.name;
                dto.fileBase64 = await this.readAsBase64(file);
                return dto;
            })
        ).then((dtos) => this.photoFiles.set(dtos));
    }

    private readAsBase64(file: File): Promise<string> {
        return new Promise<string>((resolve, reject) => {
            const reader = new FileReader();
            reader.onerror = () => reject(reader.error);
            reader.onload = () => {
                const result = reader.result as string;
                // readAsDataURL yields "data:<mime>;base64,<payload>". The backend tolerates the prefix,
                // but stripping it here keeps the payload small and unambiguous.
                const comma = result.indexOf(',');
                resolve(comma >= 0 ? result.substring(comma + 1) : result);
            };
            reader.readAsDataURL(file);
        });
    }

    // ================================================================================================
    // Bulk import / export actions
    // ================================================================================================

    /** Dispatches the selected legacy cmbAction. */
    run(): void {
        switch (this.action()) {
            case 'validate':
                this.startImport(true);
                break;
            case 'upload':
                this.startImport(false);
                break;
            case 'template':
                this.download(false);
                break;
            case 'data':
                this.download(true);
                break;
        }
    }

    private startImport(validateOnly: boolean): void {
        if (!this.canRunImport()) {
            return;
        }

        const go = () => {
            const input = new StartBulkImportInput();
            input.entityFullName = this.selectedEntityFullName();
            input.fileName = this.selectedFileName();
            input.fileBase64 = this.selectedFileBase64;
            input.importType = validateOnly ? BulkImportType.Incremental : this.importType();
            input.validateOnly = validateOnly;

            this.busy.set(true);
            this._service
                .startBulkImport(input)
                .pipe(finalize(() => this.busy.set(false)))
                .subscribe((result) => {
                    this.notify.info(result.message);
                    this.startPolling();
                });
        };

        // Purge deletes every existing row of the selected entity first. Legacy asked for no
        // confirmation at all; importing bad data over a purged table is unrecoverable, so it is
        // confirmed here.
        if (!validateOnly && this.importType() === BulkImportType.Purge) {
            this.message.confirm(
                this.l('PurgeWarning', this.selectedEntity()?.name ?? ''),
                this.l('AreYouSure'),
                (confirmed) => {
                    if (confirmed) {
                        go();
                    }
                }
            );
            return;
        }

        go();
    }

    private download(withData: boolean): void {
        if (!this.canDownload()) {
            return;
        }

        const input = new BulkExportInput();
        input.entityFullName = this.selectedEntityFullName();
        input.withData = withData;

        this.busy.set(true);
        this._service
            .downloadBulkExcel(input)
            .pipe(finalize(() => this.busy.set(false)))
            .subscribe((result) => this.saveXlsx(result.fileName, result.base64Content));
    }

    /** Legacy streamed the file with gridExport.WriteXlsToResponse(); the backend returns base64 in the
     * DTO (the ReportPdfOutput convention), so the download is assembled client-side. */
    private saveXlsx(fileName: string, base64: string): void {
        const binary = atob(base64);
        const bytes = new Uint8Array(binary.length);
        for (let i = 0; i < binary.length; i++) {
            bytes[i] = binary.charCodeAt(i);
        }

        const blob = new Blob([bytes], {
            type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
        });

        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = fileName;
        link.click();
        URL.revokeObjectURL(url);
    }

    /** Legacy btnStopUploadingFile. */
    stop(): void {
        this.busy.set(true);
        this._service
            .stopBulkImport()
            .pipe(finalize(() => this.busy.set(false)))
            .subscribe(() => this.refreshStatus());
    }

    /** Legacy btnClearStatus -> TruncateLog(). */
    clearStatus(): void {
        this.busy.set(true);
        this._service
            .clearBulkImportStatus()
            .pipe(finalize(() => this.busy.set(false)))
            .subscribe(() => {
                this.stopPolling();
                this.logEntries.set([]);
                this.logTotalCount.set(0);
                this.refreshStatus();
            });
    }

    // ================================================================================================
    // Status polling (legacy timer1 + ASPxCallbackPanel_Callback)
    // ================================================================================================

    private startPolling(): void {
        this.stopPolling();
        // Legacy timer1 ticked every few seconds against a static LongOperationStatus field.
        this.pollSubscription = interval(3000).subscribe(() => this.refreshStatus());
    }

    private stopPolling(): void {
        this.pollSubscription?.unsubscribe();
        this.pollSubscription = undefined;
    }

    refreshStatus(): void {
        this._service.getBulkImportStatus().subscribe((result) => {
            this.status.set(result);

            // Legacy disabled timer1 as soon as LongOperationStatus reached "Completed".
            if (result.executionStatus && result.executionStatus !== 'INPRECESS') {
                this.stopPolling();
            }
        });
    }

    // ================================================================================================
    // Log grid
    // ================================================================================================

    /** Legacy btnSuccess / btnFailed / btnAll. */
    showDetails(status: number | undefined): void {
        this.logStatusFilter.set(status);
        this.loadLogEntries({ first: 0, rows: 20 });
    }

    /**
     * Bound to the table's (onLazyLoad); deliberately NOT called from ngOnInit, per this repo's PrimeNG
     * rule.
     */
    loadLogEntries(event: TableLazyLoadEvent): void {
        const input = new GetBulkUploadLogEntriesInput();
        input.status = this.logStatusFilter();
        input.maxResultCount = event.rows ?? 20;
        input.skipCount = event.first ?? 0;
        input.sorting = this.primengTableHelper.getSorting(event as never);

        this.logLoading.set(true);
        this._service
            .getBulkUploadLogEntries(input)
            .pipe(finalize(() => this.logLoading.set(false)))
            .subscribe((result) => {
                this.logEntries.set(result.items ?? []);
                this.logTotalCount.set(result.totalCount);
            });
    }

    // ================================================================================================
    // Profile photo tab (legacy uscBulkImport)
    // ================================================================================================

    startPhotoImport(): void {
        if (this.photoFiles().length === 0) {
            return;
        }

        const input = new StartProfilePhotoImportInput();
        input.files = this.photoFiles();

        this.busy.set(true);
        this._service
            .startProfilePhotoImport(input)
            .pipe(finalize(() => this.busy.set(false)))
            .subscribe((result) => {
                this.notify.info(result.message);
                this.startPolling();
            });
    }

    // ================================================================================================
    // Timesheet tab (legacy uscImportTimesheet)
    // ================================================================================================

    importTimesheet(): void {
        if (!this.timesheetFileBase64) {
            return;
        }

        const input = new ImportTimesheetInput();
        input.fileName = this.timesheetFileName();
        input.fileBase64 = this.timesheetFileBase64;

        this.busy.set(true);
        this.timesheetErrors.set([]);
        this._service
            .importTimesheet(input)
            .pipe(finalize(() => this.busy.set(false)))
            .subscribe((result) => {
                this.timesheetErrors.set(result.errors ?? []);
                this.notify.info(this.l('TimesheetImported', result.inserted, result.skipped, result.failed));
            });
    }
}
