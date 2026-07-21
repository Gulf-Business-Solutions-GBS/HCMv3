import { Component, inject, viewChild, signal } from '@angular/core';
import { DateTimeService } from '@app/shared/common/timing/date-time.service';
import { AppComponentBase } from '@shared/common/app-component-base';
import { AuditLogListDto } from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { AppBsModalDirective } from '../../../shared/common/appBsModal/app-bs-modal.directive';

import { FormsModule } from '@angular/forms';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'auditLogDetailModal',
    templateUrl: './audit-log-detail-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, LocalizePipe],
})
export class AuditLogDetailModalComponent extends AppComponentBase {
    private _dateTimeService = inject(DateTimeService);

    readonly modal = viewChild<ModalDirective>('auditLogDetailModal');

    readonly active = signal(false);
    auditLog: AuditLogListDto;

    getExecutionTime(): string {
        const self = this;
        return (
            this._dateTimeService.fromNow(self.auditLog.executionTime) +
            ' (' +
            this._dateTimeService.formatDate(self.auditLog.executionTime, 'yyyy-LL-dd HH:mm:ss') +
            ')'
        );
    }

    getDurationAsMs(): string {
        const self = this;
        return self.l('Xms', self.auditLog.executionDuration);
    }

    getFormattedParameters(): string {
        const self = this;
        try {
            const json = JSON.parse(self.auditLog.parameters);
            return JSON.stringify(json, null, 4);
        } catch (e) {
            return self.auditLog.parameters;
        }
    }

    show(record: AuditLogListDto): void {
        const self = this;
        self.active.set(true);
        self.auditLog = record;

        self.modal().show();
    }

    close(): void {
        this.active.set(false);
        this.modal().hide();
    }
}
