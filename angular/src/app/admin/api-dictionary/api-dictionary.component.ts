import { Component, computed, inject, signal } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import {
    ApiDictionaryServiceProxy,
    ApiDictionaryEntityDto,
    ApiDictionaryEntitySummaryDto,
    ApiDictionaryFieldDto,
    ApiDictionaryOperationDto,
} from '@shared/service-proxies/service-proxies';

/** The four operations, always in this order, so a missing one shows as "not supported" in its own place. */
const OPERATIONS = ['Query', 'Read', 'Upsert', 'Delete'] as const;

/**
 * The API dictionary: what another system can call on a record type, and which fields it can read and write.
 * Modelled on SAP SuccessFactors' OData API Data Dictionary.
 *
 * READ-ONLY and generated. Nothing here is edited; every value comes from the server, which reads it from the
 * code, so the page cannot drift from what the API does.
 *
 * ALL STATE IS SIGNALS. The app runs zoneless, and a plain field assigned from an HTTP callback schedules no
 * repaint -- the header search showed "No results found" until a key press for exactly that reason.
 */
@Component({
    templateUrl: './api-dictionary.component.html',
    styleUrl: './api-dictionary.component.css',
    animations: [appModuleAnimation()],
    imports: [SubHeaderComponent, BusyIfDirective, FormsModule, LocalizePipe, NgTemplateOutlet],
})
export class ApiDictionaryComponent extends AppComponentBase {
    private _apiDictionaryService = inject(ApiDictionaryServiceProxy);

    readonly isLoading = signal<boolean>(false);
    readonly entities = signal<ApiDictionaryEntitySummaryDto[]>([]);
    readonly selectedName = signal<string>('');
    readonly entity = signal<ApiDictionaryEntityDto | null>(null);

    // Field filters.
    readonly filterText = signal<string>('');
    readonly onlyRequired = signal<boolean>(false);
    readonly onlySensitive = signal<boolean>(false);
    readonly onlyWritable = signal<boolean>(false);

    /** Always the placeholder host: the page is for handing to people outside the company. */
    readonly signInUrl = computed(() => (this.entity()?.baseUrl ?? 'https://servername') + '/api/TokenAuth/Authenticate');

    /** One row per operation, in a fixed order; null where the record type does not support it. */
    readonly operationRows = computed(() => {
        const operations = this.entity()?.operations ?? [];
        return OPERATIONS.map((name) => ({
            name,
            operation: operations.find((o) => o.operation === name) ?? null,
        }));
    });

    readonly allFields = computed<ApiDictionaryFieldDto[]>(() => this.entity()?.fields ?? []);

    readonly visibleFields = computed<ApiDictionaryFieldDto[]>(() => {
        const text = this.filterText().trim().toLowerCase();

        return this.allFields().filter((field) => {
            if (this.onlyRequired() && !field.isRequired) {
                return false;
            }

            if (this.onlySensitive() && !field.sensitiveCategory) {
                return false;
            }

            if (this.onlyWritable() && !field.isCreatable && !field.isUpdatable) {
                return false;
            }

            if (!text) {
                return true;
            }

            return [field.propertyName, field.label, field.type, field.linksTo]
                .filter((value) => !!value)
                .some((value) => value.toLowerCase().includes(text));
        });
    });

    constructor() {
        super();
        this.loadEntities();
    }

    loadEntities(): void {
        this.isLoading.set(true);

        this._apiDictionaryService
            .getEntities()
            .pipe(finalize(() => this.isLoading.set(false)))
            .subscribe((result) => {
                const items = result.items ?? [];
                this.entities.set(items);

                if (items.length) {
                    this.selectEntity(items[0].name);
                }
            });
    }

    selectEntity(name: string): void {
        this.selectedName.set(name);

        if (!name) {
            this.entity.set(null);
            return;
        }

        this.isLoading.set(true);

        this._apiDictionaryService
            .getEntity(name)
            .pipe(finalize(() => this.isLoading.set(false)))
            .subscribe((result) => this.entity.set(result));
    }

    clearFilters(): void {
        this.filterText.set('');
        this.onlyRequired.set(false);
        this.onlySensitive.set(false);
        this.onlyWritable.set(false);
    }

    copy(text: string): void {
        navigator.clipboard?.writeText(text).then(() => this.notify.info(this.l('Copied')));
    }

    operationLabel(name: string): string {
        return this.l('ApiOperation' + name);
    }

    /** The colour convention most API tools use, so a reader recognises a DELETE at a glance. */
    methodClass(operation: ApiDictionaryOperationDto): string {
        switch (operation.httpMethod) {
            case 'GET':
                return 'badge-light-success';
            case 'POST':
                return 'badge-light-primary';
            case 'PUT':
            case 'PATCH':
                return 'badge-light-warning';
            case 'DELETE':
                return 'badge-light-danger';
            default:
                return 'badge-light';
        }
    }
}
