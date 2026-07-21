import { Component, inject, viewChild, signal } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { EditionListDto, EditionServiceProxy } from '@shared/service-proxies/service-proxies';
import { Paginator } from 'primeng/paginator';
import { Table, TableModule } from 'primeng/table';
import { CreateEditionModalComponent } from './create-edition-modal.component';
import { EditEditionModalComponent } from './edit-edition-modal.component';
import { MoveTenantsToAnotherEditionModalComponent } from './move-tenants-to-another-edition-modal.component';
import { finalize } from 'rxjs/operators';
import { SubHeaderComponent } from '../../shared/common/sub-header/sub-header.component';

import { BusyIfDirective } from '../../../shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { PermissionAnyPipe } from '@shared/common/pipes/permission-any.pipe';
import { TieredMenu } from 'primeng/tieredmenu';
import { MenuItem } from 'primeng/api';

@Component({
    templateUrl: './editions.component.html',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        BusyIfDirective,
        TableModule,
        TieredMenu,
        CreateEditionModalComponent,
        EditEditionModalComponent,
        MoveTenantsToAnotherEditionModalComponent,
        LocalizePipe,
        PermissionPipe,
        PermissionAnyPipe,
    ],
})
export class EditionsComponent extends AppComponentBase {
    private _editionService = inject(EditionServiceProxy);

    readonly createEditionModal = viewChild<CreateEditionModalComponent>('createEditionModal');
    readonly editEditionModal = viewChild<EditEditionModalComponent>('editEditionModal');
    readonly moveTenantsToAnotherEditionModal = viewChild<MoveTenantsToAnotherEditionModalComponent>(
        'moveTenantsToAnotherEditionModal'
    );
    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');
    readonly activeEditionMenuItems = signal<MenuItem[]>([]);

    showEditionMenu(event: Event, edition: EditionListDto, menu: any): void {
        this.activeEditionMenuItems.set(this.getMenuItemsForEdition(edition));
        menu.toggle(event);
    }

    getMenuItemsForEdition(edition: EditionListDto): MenuItem[] {
        const items: MenuItem[] = [];

        if (this.permission.isGranted('Pages.Editions.Edit')) {
            items.push({
                label: this.l('Edit'),
                icon: 'pi pi-pencil',
                command: () => this.editEditionModal().show(edition.id),
            });
        }

        if (this.permission.isGranted('Pages.Editions.Delete')) {
            items.push({
                label: this.l('Delete'),
                icon: 'pi pi-trash',
                command: () => this.deleteEdition(edition),
            });
        }

        if (this.permission.isGranted('Pages.Editions.MoveTenantsToAnotherEdition')) {
            items.push({
                label: this.l('MoveTenantsToAnotherEdition'),
                icon: 'pi pi-arrow-right-arrow-left',
                command: () => this.moveTenantsToAnotherEditionModal().show(edition.id),
            });
        }

        return items;
    }

    getEditions(): void {
        this.primengTableHelper.showLoadingIndicator();
        this._editionService
            .getEditions()
            .pipe(finalize(() => this.primengTableHelper.hideLoadingIndicator()))
            .subscribe((result) => {
                this.primengTableHelper.totalRecordsCount.set(result.items.length);
                this.primengTableHelper.records.set(result.items);
                this.primengTableHelper.hideLoadingIndicator();
            });
    }

    createEdition(): void {
        this.createEditionModal().show();
    }

    deleteEdition(edition: EditionListDto): void {
        this.message.confirm(
            this.l('EditionDeleteWarningMessage', edition.displayName),
            this.l('AreYouSure'),
            (isConfirmed) => {
                if (isConfirmed) {
                    this._editionService.deleteEdition(edition.id).subscribe(() => {
                        this.getEditions();
                        this.notify.success(this.l('SuccessfullyDeleted'));
                    });
                }
            }
        );
    }
}
