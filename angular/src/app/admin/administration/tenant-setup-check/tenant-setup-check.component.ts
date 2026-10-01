import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs/operators';

import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import {
    ExportReleaseChangesInput,
    FeatureSetupStatus,
    ReleaseChangesDto,
    ReleaseFeatureDto,
    ReleaseUpdateStatusDto,
    SetupCheckItemDto,
    SetupCheckOutcome,
    TenantSetupCheckServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { FileDownloadService } from '@shared/utils/file-download.service';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';

/**
 * What's New & Setup: which release this company is on, which it was updated from, what the update added, and
 * whether the setup each new feature needs has been done.
 *
 * Always about the signed-in tenant -- the server takes the tenant from the session. Opening the page is also what
 * records an update: the server keeps the last release it saw per tenant, and when the code has moved on it keeps
 * the old one as "previous", so the comparison stays here until the next update.
 */
@Component({
    templateUrl: './tenant-setup-check.component.html',
    animations: [appModuleAnimation()],
    imports: [SubHeaderComponent, BusyIfDirective, FormsModule, NgTemplateOutlet, RouterLink, LocalizePipe, LuxonFormatPipe],
})
export class TenantSetupCheckComponent extends AppComponentBase implements OnInit {
    private readonly _service = inject(TenantSetupCheckServiceProxy);
    private readonly _fileDownloadService = inject(FileDownloadService);

    readonly SetupStatus = FeatureSetupStatus;
    readonly Outcome = SetupCheckOutcome;

    readonly status = signal<ReleaseUpdateStatusDto | undefined>(undefined);
    readonly loading = signal<boolean>(false);
    readonly exporting = signal<boolean>(false);
    readonly onlyNeedsSetup = signal<boolean>(false);

    readonly lookupVersion = signal<string>('');
    readonly lookedUp = signal<ReleaseChangesDto | undefined>(undefined);
    readonly lookingUp = signal<boolean>(false);

    /** Counts over every feature the update brought. */
    readonly summary = computed(() => {
        const features = (this.status()?.releases ?? []).flatMap((r) => r.features);
        return {
            total: features.length,
            needsSetup: features.filter((f) => f.setupStatus === FeatureSetupStatus.NeedsSetup).length,
            done: features.filter((f) => f.setupStatus === FeatureSetupStatus.Done).length,
            noSetup: features.filter((f) => f.setupStatus === FeatureSetupStatus.NoSetupNeeded).length,
            failed: features.filter((f) => f.setupStatus === FeatureSetupStatus.CheckFailed).length,
        };
    });

    ngOnInit(): void {
        this.load();
    }

    load(): void {
        this.loading.set(true);
        this._service
            .getUpdateStatus()
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe((status) => this.status.set(status));
    }

    lookUp(version: string): void {
        this.lookupVersion.set(version);
        this.lookedUp.set(undefined);
        if (!version) {
            return;
        }

        this.lookingUp.set(true);
        this._service
            .getRelease(version)
            .pipe(finalize(() => this.lookingUp.set(false)))
            .subscribe((release) => this.lookedUp.set(release));
    }

    /** Exports what the update brought, or one looked-up release when a version is given. */
    exportToExcel(version?: string): void {
        this.exporting.set(true);
        this._service
            .exportToExcel(new ExportReleaseChangesInput({ version: version ?? '' }))
            .pipe(finalize(() => this.exporting.set(false)))
            .subscribe((file) => this._fileDownloadService.downloadTempFile(file));
    }

    visibleFeatures(release: ReleaseChangesDto): ReleaseFeatureDto[] {
        return this.onlyNeedsSetup()
            ? release.features.filter(
                  (f) => f.setupStatus === FeatureSetupStatus.NeedsSetup || f.setupStatus === FeatureSetupStatus.CheckFailed
              )
            : release.features;
    }

    statusName(status: FeatureSetupStatus): string {
        return this.l('FeatureSetupStatus_' + FeatureSetupStatus[status]);
    }

    statusBadge(status: FeatureSetupStatus): string {
        switch (status) {
            case FeatureSetupStatus.Done:
                return 'badge-light-success';
            case FeatureSetupStatus.NeedsSetup:
                return 'badge-light-danger';
            case FeatureSetupStatus.CheckFailed:
                return 'badge-light-warning';
            default:
                return 'badge-light-secondary';
        }
    }

    outcomeName(outcome: SetupCheckOutcome): string {
        return this.l('SetupCheckOutcome_' + SetupCheckOutcome[outcome]);
    }

    outcomeBadge(outcome: SetupCheckOutcome): string {
        switch (outcome) {
            case SetupCheckOutcome.Ok:
                return 'badge-light-success';
            case SetupCheckOutcome.Missing:
                return 'badge-light-danger';
            case SetupCheckOutcome.Error:
                return 'badge-light-warning';
            default:
                return 'badge-light-secondary';
        }
    }

    /** Codes the check counted but did not list (it lists at most twenty). */
    moreCodes(item: SetupCheckItemDto): number {
        return Math.max(0, item.missingCount - item.examples.length);
    }
}
