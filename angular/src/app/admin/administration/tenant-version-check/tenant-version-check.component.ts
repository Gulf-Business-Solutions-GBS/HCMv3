import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs/operators';

import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import {
    BuildComparisonDto,
    CompareBuildsInput,
    SetupItemDto,
    SetupItemKind,
    SetupItemStatus,
    TenantVersionCheckServiceProxy,
    TenantVersionCheckStatusDto,
} from '@shared/service-proxies/service-proxies';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { FileDownloadService } from '@shared/utils/file-download.service';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';

/**
 * Tenant Version Check: which build this company is on, which it was updated from, what changed in between, and what
 * that asks the company to set up.
 *
 * The version and the change log are not typed by anyone: the build reads them from git. Opening the page is what
 * records an update -- the server keeps the last version it saw per tenant, and when the build has moved on it
 * keeps the old one as "previous". Always about the signed-in tenant.
 */
@Component({
    templateUrl: './tenant-version-check.component.html',
    animations: [appModuleAnimation()],
    imports: [SubHeaderComponent, BusyIfDirective, FormsModule, RouterLink, LocalizePipe, LuxonFormatPipe],
})
export class TenantVersionCheckComponent extends AppComponentBase implements OnInit {
    private readonly _service = inject(TenantVersionCheckServiceProxy);
    private readonly _fileDownloadService = inject(FileDownloadService);

    readonly Status = SetupItemStatus;

    readonly status = signal<TenantVersionCheckStatusDto | undefined>(undefined);
    readonly loading = signal<boolean>(false);

    readonly fromVersion = signal<string>('');
    readonly comparison = signal<BuildComparisonDto | undefined>(undefined);
    readonly comparing = signal<boolean>(false);
    readonly exporting = signal<boolean>(false);

    /** Off: only what needs somebody -- action required and review. On: completed and information items too. */
    readonly showAll = signal<boolean>(false);
    readonly showChanges = signal<boolean>(false);

    readonly needAttention = computed(() => (this.comparison()?.setupItems ?? []).filter((i) => this.needsSomebody(i)));

    readonly visibleItems = computed(() => (this.showAll() ? (this.comparison()?.setupItems ?? []) : this.needAttention()));

    ngOnInit(): void {
        this.loading.set(true);
        this._service
            .getStatus()
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe((status) => {
                this.status.set(status);
                // Straight after an update, the comparison people came for is "previous to installed".
                if (status.canComparePrevious) {
                    this.fromVersion.set(status.previousVersion);
                    this.compare();
                }
            });
    }

    compare(): void {
        if (!this.fromVersion()) {
            return;
        }

        this.comparing.set(true);
        this._service
            .compare(new CompareBuildsInput({ fromVersion: this.fromVersion() }))
            .pipe(finalize(() => this.comparing.set(false)))
            .subscribe((comparison) => this.comparison.set(comparison));
    }

    exportToExcel(): void {
        this.exporting.set(true);
        this._service
            .exportToExcel(new CompareBuildsInput({ fromVersion: this.comparison().fromVersion }))
            .pipe(finalize(() => this.exporting.set(false)))
            .subscribe((file) => this._fileDownloadService.downloadTempFile(file));
    }

    needsSomebody(item: SetupItemDto): boolean {
        return item.status === SetupItemStatus.ActionRequired || item.status === SetupItemStatus.Review;
    }

    statusName(status: SetupItemStatus): string {
        return this.l('SetupItemStatus_' + SetupItemStatus[status]);
    }

    statusBadge(status: SetupItemStatus): string {
        switch (status) {
            case SetupItemStatus.ActionRequired:
                return 'badge-light-danger';
            case SetupItemStatus.Review:
                return 'badge-light-warning';
            case SetupItemStatus.Done:
                return 'badge-light-success';
            default:
                return 'badge-light-secondary';
        }
    }

    kindName(kind: SetupItemKind): string {
        return this.l('SetupItemKind_' + SetupItemKind[kind]);
    }
}
