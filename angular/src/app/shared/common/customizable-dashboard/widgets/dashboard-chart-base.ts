import { signal } from '@angular/core';

export abstract class DashboardChartBase {
    readonly loading = signal(true);

    showLoading() {
        setTimeout(() => {
            this.loading.set(true);
        });
    }

    hideLoading() {
        setTimeout(() => {
            this.loading.set(false);
        });
    }
}
