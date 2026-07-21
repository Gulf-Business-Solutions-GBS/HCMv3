import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { DashboardCustomizationServiceProxy, WidgetOutput } from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { DashboardCustomizationConst } from '../DashboardCustomizationConsts';
import { AppBsModalDirective } from '../../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';

import { AutoFocusDirective } from '../../../../../shared/utils/auto-focus.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'add-widget-modal',
    templateUrl: './add-widget-modal.component.html',
    styleUrls: ['./add-widget-modal.component.css'],
    imports: [AppBsModalDirective, FormsModule, AutoFocusDirective, LocalizePipe],
})
export class AddWidgetModalComponent extends AppComponentBase {
    private _dashboardCustomizationServiceProxy = inject(DashboardCustomizationServiceProxy);

    readonly onClose = output<any>();
    readonly modal = viewChild<ModalDirective>('addWidgetModal');

    readonly widgets = signal<WidgetOutput[]>([]);
    readonly saving = signal(false);
    readonly selectedWidgetId = signal<string>(null);

    close(): void {
        this.onClose.emit(null);
        this.hide();
    }

    save(): void {
        this.onClose.emit(this.selectedWidgetId());
        this.hide();
    }

    show(dashboardName: string, pageId: string): void {
        this._dashboardCustomizationServiceProxy
            .getAllAvailableWidgetDefinitionsForPage(
                dashboardName,
                DashboardCustomizationConst.Applications.Angular,
                pageId
            )
            .subscribe((availableWidgets) => {
                this.widgets.set(availableWidgets);
                if (availableWidgets && availableWidgets.length) {
                    this.selectedWidgetId.set(availableWidgets[0].id);
                } else {
                    this.selectedWidgetId.set(null);
                }
                this.modal().show();
            });
    }

    hide(): void {
        this.modal().hide();
    }
}
