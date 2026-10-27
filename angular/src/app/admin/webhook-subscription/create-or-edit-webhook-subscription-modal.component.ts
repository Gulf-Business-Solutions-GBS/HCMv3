import { Component, inject, output, viewChild, signal, computed } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { NameValuePair } from '@shared/utils/name-value-pair';
import { WebhookSubscriptionServiceProxy, WebhookSubscription } from '@shared/service-proxies/service-proxies';
import { Observable } from 'rxjs';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { KeyValueListManagerComponent } from '@app/shared/common/key-value-list-manager/key-value-list-manager.component';

import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../shared/utils/validation-messages.component';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { KeyValueListManagerComponent as KeyValueListManagerComponent_1 } from '../../shared/common/key-value-list-manager/key-value-list-manager.component';
import { ButtonBusyDirective } from '../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'create-or-edit-webhook-subscription',
    templateUrl: './create-or-edit-webhook-subscription-modal.component.html',
    styleUrls: ['./create-or-edit-webhook-subscription-modal.component.css'],
    imports: [
        ModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        AutoCompleteModule,
        KeyValueListManagerComponent_1,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditWebhookSubscriptionModalComponent extends AppComponentBase {
    private _webhookSubscriptionService = inject(WebhookSubscriptionServiceProxy);

    readonly headerKeyValueManager = viewChild<KeyValueListManagerComponent>('headerKeyValueManager');
    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<any>();

    objectKeys = Object.keys;

    webhookSubscription = signal<WebhookSubscription>(new WebhookSubscription());
    webhookSubscriptionId = signal<string | undefined>(undefined);

    allWebhooks = signal<NameValuePair[]>([]);

    filteredWebhooks = signal<NameValuePair[]>([]);

    active = signal(false);
    saving = signal(false);

    webhooks = signal<NameValuePair[]>([]);
    headers = signal<{ key: string; value: string }[]>([]);

    constructor() {
        super();
        this.getAllWebhooks();
    }

    show(subscriptionId?: string): void {
        this.resetValues();
        this.webhookSubscriptionId.set(subscriptionId);
        if (!subscriptionId) {
            this.active.set(true);
            this.modal().show();
            return;
        }

        this.showMainSpinner();
        this._webhookSubscriptionService.getSubscription(subscriptionId).subscribe(
            (result) => {
                this.webhookSubscription.set(result);
                this.webhooks.set(
                    result.webhooks.map(
                        (wh) =>
                            new NameValuePair({
                                name: wh,
                                value: wh,
                            })
                    )
                );

                let keys = Object.keys(result.headers);
                if (result.headers && keys.length > 0) {
                    this.headers.set(
                        keys.map((x) => ({
                            key: x,
                            value: result.headers[x],
                        }))
                    );
                }

                this.hideMainSpinner();
                this.active.set(true);
                this.modal().show();
            },
            (e) => {
                this.hideMainSpinner();
            }
        );
    }

    resetValues(): void {
        this.webhookSubscription.set(new WebhookSubscription());
        this.webhooks.set([]);
        this.headers.set([]);
    }

    save(): void {
        this.webhookSubscription.update((subscription) => {
            const updated = new WebhookSubscription();
            const webhooks = this.webhooks().map((wh) => wh.value);
            const headers = {};
            this.headerKeyValueManager()
                .getItems()
                .forEach((item) => {
                    headers[item.key] = item.value;
                });
            updated.init({ ...subscription.toJSON(), webhooks, headers });
            return updated;
        });

        let observable: Observable<void>;
        if (!this.webhookSubscriptionId()) {
            observable = this._webhookSubscriptionService.addSubscription(this.webhookSubscription());
        } else {
            observable = this._webhookSubscriptionService.updateSubscription(this.webhookSubscription());
        }

        observable.subscribe(
            () => {
                this.notify.info(this.l('SavedSuccessfully'));
                this.hideMainSpinner();
                this.modalSave.emit(null);
                this.close();
            },
            (e) => {
                this.hideMainSpinner();
            }
        );
    }

    getAllWebhooks(): void {
        this._webhookSubscriptionService.getAllAvailableWebhooks().subscribe((webhooks) => {
            this.allWebhooks.set(
                webhooks.items.map(
                    (wh) =>
                        new NameValuePair({
                            name: wh.displayName ?? wh.name,
                            value: wh.name,
                        })
                )
            );
        });
    }

    filterWebhooks(event) {
        this.filteredWebhooks.set(
            this.allWebhooks().filter(
                (item) =>
                    item.name.toLowerCase().includes(event.query.toLowerCase()) ||
                    item.value.toLowerCase().includes(event.query.toLowerCase())
            )
        );
    }

    close(): void {
        this.active.set(false);
        this.modal().hide();
    }

    removeHeader(headerKey: string) {
        let item = document.getElementById('additional-header-' + headerKey);
        item.remove();
        this.webhookSubscription.update((subscription) => {
            const updated = new WebhookSubscription();
            const newHeaders = { ...subscription.headers };
            delete newHeaders[headerKey];
            updated.init({ ...subscription.toJSON(), headers: newHeaders });
            return updated;
        });
    }
}
