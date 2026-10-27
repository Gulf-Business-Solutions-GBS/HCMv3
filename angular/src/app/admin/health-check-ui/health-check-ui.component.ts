import { Component, OnInit, OnDestroy, inject, signal, computed } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { HealthCheckServiceProxy, HealthCheckItemDto } from '@shared/service-proxies/service-proxies';
import { finalize } from 'rxjs/operators';
import { SubHeaderComponent } from '../../shared/common/sub-header/sub-header.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { AppConsts } from '@shared/AppConsts';

@Component({
    templateUrl: './health-check-ui.component.html',
    animations: [appModuleAnimation()],
    imports: [SubHeaderComponent, LocalizePipe],
})
export class HealthCheckUIComponent extends AppComponentBase implements OnInit, OnDestroy {
    private readonly REFRESH_INTERVAL_MS = AppConsts.healthCheck.refreshIntervalMs;
    private readonly _healthCheckService = inject(HealthCheckServiceProxy);
    private _refreshInterval: any;

    readonly loading = signal(false);
    readonly healthChecks = signal<HealthCheckItemDto[]>([]);

    readonly refreshIntervalSeconds = computed(() => AppConsts.healthCheck.refreshIntervalMs / 1000);

    readonly autoRefreshMessage = computed(() =>
        this.l('ThisTableIsRefreshedInEveryXSecondsAutomatically', this.refreshIntervalSeconds().toString())
    );

    ngOnInit(): void {
        this.getHealthChecks();
        this._refreshInterval = setInterval(() => {
            this.getHealthChecks();
        }, this.REFRESH_INTERVAL_MS);
    }

    ngOnDestroy(): void {
        if (this._refreshInterval) {
            clearInterval(this._refreshInterval);
        }
    }

    getHealthChecks(): void {
        this.loading.set(true);
        this._healthCheckService
            .getHealthChecks()
            .pipe(
                finalize(() => {
                    this.loading.set(false);
                })
            )
            .subscribe((result) => {
                this.healthChecks.set(result.items || []);
            });
    }

    refreshHealthChecks(): void {
        this.getHealthChecks();
        this.notify.success(this.l('HealthChecksRefreshed'));
    }

    getStatusBadgeClass(status: string | undefined): string {
        if (status === 'Healthy') {
            return 'badge badge-success';
        } else if (status === 'Degraded') {
            return 'badge badge-warning';
        } else {
            return 'badge badge-danger';
        }
    }

    getStatusText(status: string | undefined): string {
        if (status === 'Healthy') {
            return this.l('Healthy');
        } else if (status === 'Degraded') {
            return this.l('Degraded');
        } else {
            return this.l('Unhealthy');
        }
    }

    formatDuration(duration: string | undefined): string {
        if (!duration) {
            return '0 ms';
        }

        // Duration comes as a TimeSpan string like "00:00:00.0012345"
        const parts = duration.split(':');
        if (parts.length === 3) {
            const seconds = parseFloat(parts[2]);
            return (seconds * 1000).toFixed(2) + ' ms';
        }
        return duration;
    }
}
