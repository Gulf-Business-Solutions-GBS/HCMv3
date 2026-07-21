import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CachingServiceProxy,
    EntityDtoOfString,
    NotificationServiceProxy,
    WebLogServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { FileDownloadService } from '@shared/utils/file-download.service';
import { escape as _escape } from 'lodash-es';
import { finalize } from 'rxjs/operators';
import { SubHeaderComponent } from '../../shared/common/sub-header/sub-header.component';

import { TabsetComponent, TabDirective } from 'ngx-bootstrap/tabs';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';

@Component({
    templateUrl: './maintenance.component.html',
    styleUrls: ['./maintenance.component.less'],
    animations: [appModuleAnimation()],
    imports: [SubHeaderComponent, TabsetComponent, TabDirective, LocalizePipe, PermissionPipe],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MaintenanceComponent extends AppComponentBase implements OnInit {
    private readonly _cacheService = inject(CachingServiceProxy);
    private readonly _webLogService = inject(WebLogServiceProxy);
    private readonly _notificationService = inject(NotificationServiceProxy);
    private readonly _fileDownloadService = inject(FileDownloadService);

    readonly loading = signal(false);
    readonly caches = signal<any>(null);
    readonly canClearAllCaches = signal(false);
    readonly logs = signal<any>('');

    ngOnInit(): void {
        this.setCanClearAllCaches();
        this.getCaches();
        this.getWebLogs();
    }

    sendNewVersionAvailableNotification(): void {
        abp.message.confirm(this.l('SendNewVersionNotificationWarningMessage'), null, (isConfirmed) => {
            if (isConfirmed) {
                this._notificationService.createNewVersionReleasedNotification().subscribe((result) => {
                    abp.notify.info(this.l('SuccessfullySentNewVersionNotification'));
                });
            }
        });
    }

    getCaches(): void {
        this.loading.set(true);
        this._cacheService
            .getAllCaches()
            .pipe(
                finalize(() => {
                    this.loading.set(false);
                })
            )
            .subscribe((result) => {
                this.caches.set(result.items);
            });
    }

    clearCache(cacheName): void {
        const input = new EntityDtoOfString();
        input.id = cacheName;

        this._cacheService.clearCache(input).subscribe(() => {
            this.notify.success(this.l('CacheSuccessfullyCleared'));
        });
    }

    clearAllCaches(): void {
        this._cacheService.clearAllCaches().subscribe(() => {
            this.notify.success(this.l('AllCachesSuccessfullyCleared'));
        });
    }

    getWebLogs(): void {
        this._webLogService.getLatestWebLogs().subscribe((result) => {
            this.logs.set(result.latestWebLogLines);
            this.fixWebLogsPanelHeight();
        });
    }

    setCanClearAllCaches(): void {
        this._cacheService.canClearAllCaches().subscribe((result) => {
            this.canClearAllCaches.set(result);
        });
    }

    downloadWebLogs(): void {
        this._webLogService.downloadWebLogs().subscribe((result) => {
            this._fileDownloadService.downloadTempFile(result);
        });
    }

    getLogClass(log: string): string {
        if (log.startsWith('DEBUG')) {
            return 'badge badge-dark';
        }

        if (log.startsWith('INFO')) {
            return 'badge badge-info';
        }

        if (log.startsWith('WARN')) {
            return 'badge badge-warning';
        }

        if (log.startsWith('ERROR')) {
            return 'badge badge-danger';
        }

        if (log.startsWith('FATAL')) {
            return 'badge badge-danger';
        }

        return '';
    }

    getLogType(log: string): string {
        if (log.startsWith('DEBUG')) {
            return 'DEBUG';
        }

        if (log.startsWith('INFO')) {
            return 'INFO';
        }

        if (log.startsWith('WARN')) {
            return 'WARN';
        }

        if (log.startsWith('ERROR')) {
            return 'ERROR';
        }

        if (log.startsWith('FATAL')) {
            return 'FATAL';
        }

        return '';
    }

    getRawLogContent(log: string): string {
        return _escape(log)
            .replace('DEBUG', '')
            .replace('INFO', '')
            .replace('WARN', '')
            .replace('ERROR', '')
            .replace('FATAL', '');
    }

    fixWebLogsPanelHeight(): void {
        let panel = document.getElementsByClassName('full-height')[0];
        const windowHeight = document.body.clientHeight;
        const panelHeight = panel.clientHeight;
        const difference = windowHeight - panelHeight;
        const fixedHeight = panelHeight + difference;
        (panel as any).style.height = fixedHeight - 420 + 'px';
    }

    onResize(event): void {
        this.fixWebLogsPanelHeight();
    }
}
