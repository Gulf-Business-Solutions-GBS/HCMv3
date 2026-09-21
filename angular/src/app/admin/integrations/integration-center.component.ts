import { Component, OnInit, computed, inject, signal, viewChild } from '@angular/core';
import { Router } from '@angular/router';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    IntegrationCategory,
    IntegrationCenterServiceProxy,
    IntegrationStatusType,
    IntegrationTileDto,
    SetIntegrationEnabledInput,
} from '@shared/service-proxies/service-proxies';
import { finalize } from 'rxjs/operators';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import { Tooltip } from 'primeng/tooltip';
import { ConfigureIntegrationModalComponent } from './configure-integration-modal.component';

/** One heading plus the cards under it. */
interface IntegrationSection {
    category: IntegrationCategory;
    titleKey: string;
    tiles: IntegrationTileDto[];
}

@Component({
    templateUrl: './integration-center.component.html',
    styleUrls: ['./integration-center.component.css'],
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        BusyIfDirective,
        Tooltip,
        ConfigureIntegrationModalComponent,
        LocalizePipe,
        LuxonFormatPipe,
    ],
})
export class IntegrationCenterComponent extends AppComponentBase implements OnInit {
    private _service = inject(IntegrationCenterServiceProxy);
    private _router = inject(Router);

    readonly configureModal = viewChild<ConfigureIntegrationModalComponent>('configureModal');

    readonly loading = signal<boolean>(false);
    readonly testingKey = signal<string>('');
    readonly tiles = signal<IntegrationTileDto[]>([]);

    /** Cards grouped under a heading, in the order the categories are declared. */
    readonly sections = computed<IntegrationSection[]>(() => {
        const order: { category: IntegrationCategory; titleKey: string }[] = [
            { category: IntegrationCategory.Identity, titleKey: 'IntegrationCategory_Identity' },
            { category: IntegrationCategory.Communication, titleKey: 'IntegrationCategory_Communication' },
            { category: IntegrationCategory.Finance, titleKey: 'IntegrationCategory_Finance' },
            { category: IntegrationCategory.TimeAttendance, titleKey: 'IntegrationCategory_TimeAttendance' },
            { category: IntegrationCategory.Outbound, titleKey: 'IntegrationCategory_Outbound' },
        ];

        const all = this.tiles();

        return order
            .map((o) => ({ ...o, tiles: all.filter((t) => t.category === o.category) }))
            .filter((s) => s.tiles.length > 0);
    });

    /**
     * Card icon by integration key. Deliberately generic glyphs rather than vendor logos: shipping a
     * third party's mark means keeping up with their brand guidelines, and a wrong or outdated logo looks
     * worse than an icon.
     */
    private readonly iconByKey: Record<string, string> = {
        AzureSso: 'fa fa-key',
        Smtp: 'fa fa-envelope',
        D365Finance: 'fa fa-file-invoice-dollar',
        BioTime: 'fa fa-fingerprint',
        Webhooks: 'fa fa-bolt',
    };

    ngOnInit(): void {
        this.load();
    }

    load(): void {
        this.loading.set(true);

        this._service
            .getIntegrations()
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe((result) => {
                this.tiles.set(result.items ?? []);
            });
    }

    icon(tile: IntegrationTileDto): string {
        return this.iconByKey[tile.key] ?? 'fa fa-plug';
    }

    statusKey(tile: IntegrationTileDto): string {
        switch (tile.status) {
            case IntegrationStatusType.Enabled:
                return 'IntegrationStatus_Enabled';
            case IntegrationStatusType.Configured:
                return 'IntegrationStatus_Configured';
            case IntegrationStatusType.Error:
                return 'IntegrationStatus_Error';
            default:
                return 'IntegrationStatus_NotConfigured';
        }
    }

    statusClass(tile: IntegrationTileDto): string {
        switch (tile.status) {
            case IntegrationStatusType.Enabled:
                return 'badge badge-light-success';
            case IntegrationStatusType.Configured:
                return 'badge badge-light-primary';
            case IntegrationStatusType.Error:
                return 'badge badge-light-danger';
            default:
                return 'badge badge-light';
        }
    }

    /**
     * Either the field form, or the integration's own screen when a flat field list cannot express its
     * configuration -- webhooks are a list of endpoints, not a form.
     */
    configure(tile: IntegrationTileDto): void {
        if (!tile.hasFields && tile.externalManageRoute) {
            this._router.navigate([tile.externalManageRoute]);
            return;
        }

        this.configureModal().show(tile.key);
    }

    openExternal(tile: IntegrationTileDto): void {
        this._router.navigate([tile.externalManageRoute]);
    }

    toggle(tile: IntegrationTileDto): void {
        if (tile.isEnabled) {
            this.confirmDisable(tile);
            return;
        }

        this.confirmEnable(tile);
    }

    /**
     * Switching something off is confirmed rather than done on one click, because the consequences are
     * invisible until somebody needs the thing that stopped -- most sharply for e-mail, where turning it
     * off also stops password-reset messages.
     */
    private confirmDisable(tile: IntegrationTileDto): void {
        const name = this.l(tile.nameKey);

        const question =
            tile.key === 'Smtp'
                ? this.l('IntegrationConfirmDisableSmtp')
                : this.l('IntegrationConfirmDisable', name);

        this.message.confirm(question, this.l('AreYouSure'), (confirmed) => {
            if (confirmed) {
                this.setEnabled(tile, false);
            }
        });
    }

    /**
     * Enabling a member of an exclusive group switches its siblings off. The server does that on its own;
     * saying so first means nobody discovers it by finding their other provider mysteriously disabled.
     */
    private confirmEnable(tile: IntegrationTileDto): void {
        const sibling = tile.exclusiveGroup
            ? this.tiles().find((t) => t.exclusiveGroup === tile.exclusiveGroup && t.key !== tile.key && t.isEnabled)
            : undefined;

        if (!sibling) {
            this.setEnabled(tile, true);
            return;
        }

        this.message.confirm(
            this.l('IntegrationConfirmSwitchProvider', this.l(sibling.nameKey), this.l(tile.nameKey)),
            this.l('AreYouSure'),
            (confirmed) => {
                if (confirmed) {
                    this.setEnabled(tile, true);
                }
            }
        );
    }

    private setEnabled(tile: IntegrationTileDto, enabled: boolean): void {
        const input = new SetIntegrationEnabledInput();
        input.key = tile.key;
        input.enabled = enabled;

        this.loading.set(true);

        this._service
            .setEnabled(input)
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe(() => {
                this.notify.success(this.l('SavedSuccessfully'));
                this.load();
            });
    }

    test(tile: IntegrationTileDto): void {
        this.testingKey.set(tile.key);

        this._service
            .testConnection(tile.key)
            .pipe(finalize(() => this.testingKey.set('')))
            .subscribe((result) => {
                // The message comes back already localized, and carries the detail that makes a failure
                // actionable -- the status code, or what the far end said.
                if (result.succeeded) {
                    this.notify.success(result.message, this.l('IntegrationTestSucceeded'));
                } else {
                    this.notify.error(result.message, this.l('IntegrationTestFailed'));
                }

                this.load();
            });
    }
}
