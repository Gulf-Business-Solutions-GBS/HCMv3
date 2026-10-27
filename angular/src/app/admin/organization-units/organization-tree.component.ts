import { OnInit, Component, inject, output, viewChild, signal } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { Router } from '@angular/router';
import {
    ListResultDtoOfOrganizationUnitDto,
    MoveOrganizationUnitInput,
    OrganizationUnitDto,
    OrganizationUnitServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { filter as _filter } from 'lodash-es';
import { throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { IBasicOrganizationUnitInfo } from './basic-organization-unit-info';
import { CreateOrEditUnitModalComponent } from './create-or-edit-unit-modal.component';
import { IUserWithOrganizationUnit } from './user-with-organization-unit';
import { IUsersWithOrganizationUnit } from './users-with-organization-unit';
import { IRoleWithOrganizationUnit } from './role-with-organization-unit';
import { IRolesWithOrganizationUnit } from './roles-with-organization-unit';

import { TreeNode, MenuItem } from 'primeng/api';

import { ArrayToTreeConverterService } from '@shared/utils/array-to-tree-converter.service';
import { TreeDataHelperService } from '@shared/utils/tree-data-helper.service';

import { TreeModule } from 'primeng/tree';
import { ContextMenuModule } from 'primeng/contextmenu';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { TreeDragDropService } from 'primeng/api';

export interface IOrganizationUnitOnTree extends IBasicOrganizationUnitInfo {
    id: number;
    parent: string | number;
    code: string;
    displayName: string;
    memberCount: number;
    roleCount: number;
    text: string;
    state: any;
}

@Component({
    selector: 'organization-tree',
    templateUrl: './organization-tree.component.html',
    imports: [TreeModule, ContextMenuModule, CreateOrEditUnitModalComponent, LocalizePipe, PermissionPipe],
    providers: [TreeDragDropService],
})
export class OrganizationTreeComponent extends AppComponentBase implements OnInit {
    private _organizationUnitService = inject(OrganizationUnitServiceProxy);
    private _arrayToTreeConverterService = inject(ArrayToTreeConverterService);
    private _treeDataHelperService = inject(TreeDataHelperService);
    private _router = inject(Router);

    readonly ouSelected = output<IBasicOrganizationUnitInfo>();

    readonly createOrEditOrganizationUnitModal = viewChild<CreateOrEditUnitModalComponent>(
        'createOrEditOrganizationUnitModal'
    );

    readonly treeData = signal<TreeNode[]>([]);
    readonly selectedOu = signal<TreeNode | null>(null);
    readonly ouContextMenuItems = signal<MenuItem[]>([]);
    readonly canManageOrganizationUnits = signal<boolean>(false);
    readonly totalUnitCount = signal<number>(0);

    _entityTypeFullName = 'Abp.Organizations.OrganizationUnit';

    ngOnInit(): void {
        this.canManageOrganizationUnits.set(
            this.isGranted('Pages.Administration.OrganizationUnits.ManageOrganizationTree')
        );
        this.ouContextMenuItems.set(this.getContextMenuItems());
        this.getTreeDataFromServer();
    }

    nodeSelect(event) {
        this.selectedOu.set(event.node);
        this.ouSelected.emit(<IBasicOrganizationUnitInfo>{
            id: event.node.data.id,
            displayName: event.node.data.displayName,
        });
    }

    onContextMenuShow(event: any): void {
        this.selectedOu.set(event.node);
    }

    isDroppingBetweenTwoNodes(event: any): boolean {
        return event.originalEvent.target.nodeName === 'LI';
    }

    nodeDrop(event) {
        const input = new MoveOrganizationUnitInput();
        input.id = event.dragNode.data.id;
        let dropNodeDisplayName: string;

        if (this.isDroppingBetweenTwoNodes(event)) {
            input.newParentId = event.dropNode.parent ? event.dropNode.parent.data.id : null;
            dropNodeDisplayName = event.dropNode.parent ? event.dropNode.parent.data.displayName : this.l('Root');
        } else {
            input.newParentId = event.dropNode.data.id;
            dropNodeDisplayName = event.dropNode.data.displayName;
        }

        this.message.confirm(
            this.l('OrganizationUnitMoveConfirmMessage', event.dragNode.data.displayName, dropNodeDisplayName),
            this.l('AreYouSure'),
            (isConfirmed) => {
                if (isConfirmed) {
                    this._organizationUnitService
                        .moveOrganizationUnit(input)
                        .pipe(
                            catchError((error) => {
                                this.revertDragDrop();
                                return throwError(error);
                            })
                        )
                        .subscribe(() => {
                            this.notify.success(this.l('SuccessfullyMoved'));
                            this.reload();
                        });
                } else {
                    this.revertDragDrop();
                }
            }
        );
    }

    revertDragDrop() {
        this.reload();
    }

    reload(): void {
        this.getTreeDataFromServer();
    }

    addUnit(parentId?: number): void {
        this.createOrEditOrganizationUnitModal().show({
            parentId: parentId,
        });
    }

    unitCreated(ou: OrganizationUnitDto): void {
        const newNode: any = {
            label: ou.displayName,
            expandedIcon: 'fa fa-folder-open text-warning',
            collapsedIcon: 'fa fa-folder text-warning',
            children: [],
            data: ou,
            memberCount: ou.memberCount,
            roleCount: ou.roleCount,
        };

        this.treeData.update((currentTree) => {
            if (ou.parentId) {
                const unit = this._treeDataHelperService.findNode(currentTree, { data: { id: ou.parentId } });
                if (!unit) {
                    return currentTree;
                }

                const updatedTree = this.deepCloneTree(currentTree);
                const parentUnit = this._treeDataHelperService.findNode(updatedTree, { data: { id: ou.parentId } });
                if (parentUnit) {
                    parentUnit.children = [...(parentUnit.children || []), newNode];
                    parentUnit.expanded = true;
                }
                this.selectedOu.set(newNode);
                return updatedTree;
            } else {
                this.selectedOu.set(newNode);
                return [...currentTree, newNode];
            }
        });

        this.totalUnitCount.update((count) => count + 1);
    }

    deleteUnit(id) {
        this.treeData.update((currentTree) => {
            const node = this._treeDataHelperService.findNode(currentTree, { data: { id: id } });
            if (!node) {
                return currentTree;
            }

            const updatedTree = this.deepCloneTree(currentTree);

            if (!node.data.parentId) {
                const filtered = updatedTree.filter((item) => item.data.id !== id);
                return filtered;
            }

            const parentNode = this._treeDataHelperService.findNode(updatedTree, { data: { id: node.data.parentId } });
            if (!parentNode) {
                return updatedTree;
            }

            if (parentNode.children) {
                parentNode.children = parentNode.children.filter((child) => child.data.id !== id);
            }

            return updatedTree;
        });
    }

    unitUpdated(ou: OrganizationUnitDto): void {
        this.treeData.update((currentTree) => {
            const item = this._treeDataHelperService.findNode(currentTree, { data: { id: ou.id } });
            if (!item) {
                return currentTree;
            }

            const updatedTree = this.deepCloneTree(currentTree);
            const updatedItem = this._treeDataHelperService.findNode(updatedTree, { data: { id: ou.id } });
            if (updatedItem) {
                updatedItem.data.displayName = ou.displayName;
                updatedItem.label = ou.displayName;
                updatedItem.memberCount = ou.memberCount;
                updatedItem.roleCount = ou.roleCount;
            }
            return updatedTree;
        });
    }

    membersAdded(data: IUsersWithOrganizationUnit): void {
        this.incrementMemberCount(data.ouId, data.userIds.length);
    }

    memberRemoved(data: IUserWithOrganizationUnit): void {
        this.incrementMemberCount(data.ouId, -1);
    }

    incrementMemberCount(ouId: number, incrementAmount: number): void {
        this.treeData.update((currentTree) => {
            const item = this._treeDataHelperService.findNode(currentTree, { data: { id: ouId } });
            if (!item) {
                return currentTree;
            }

            const updatedTree = this.deepCloneTree(currentTree);
            const updatedItem = this._treeDataHelperService.findNode(updatedTree, { data: { id: ouId } });
            if (updatedItem) {
                updatedItem.data.memberCount += incrementAmount;
                updatedItem.memberCount = updatedItem.data.memberCount;
            }
            return updatedTree;
        });
    }

    rolesAdded(data: IRolesWithOrganizationUnit): void {
        this.incrementRoleCount(data.ouId, data.roleIds.length);
    }

    roleRemoved(data: IRoleWithOrganizationUnit): void {
        this.incrementRoleCount(data.ouId, -1);
    }

    incrementRoleCount(ouId: number, incrementAmount: number): void {
        this.treeData.update((currentTree) => {
            const item = this._treeDataHelperService.findNode(currentTree, { data: { id: ouId } });
            if (!item) {
                return currentTree;
            }

            const updatedTree = this.deepCloneTree(currentTree);
            const updatedItem = this._treeDataHelperService.findNode(updatedTree, { data: { id: ouId } });
            if (updatedItem) {
                updatedItem.data.roleCount += incrementAmount;
                updatedItem.roleCount = updatedItem.data.roleCount;
            }
            return updatedTree;
        });
    }

    private getTreeDataFromServer(): void {
        this._organizationUnitService.getOrganizationUnits().subscribe((result: ListResultDtoOfOrganizationUnitDto) => {
            this.totalUnitCount.set(result.items.length);
            const tree = this._arrayToTreeConverterService.createTree(
                result.items,
                'parentId',
                'id',
                null,
                'children',
                [
                    {
                        target: 'key',
                        source: 'name',
                    },
                    {
                        target: 'label',
                        targetFunction(item) {
                            return item.displayName;
                        },
                    },
                    {
                        target: 'expandedIcon',
                        value: 'fa fa-folder-open text-warning',
                    },
                    {
                        target: 'collapsedIcon',
                        value: 'fa fa-folder text-warning',
                    },
                    {
                        target: 'selectable',
                        value: true,
                    },
                    {
                        target: 'memberCount',
                        targetFunction(item) {
                            return item.memberCount;
                        },
                    },
                    {
                        target: 'roleCount',
                        targetFunction(item) {
                            return item.roleCount;
                        },
                    },
                ]
            );
            this.treeData.set(tree);
        });
    }

    private deepCloneTree(tree: TreeNode[]): TreeNode[] {
        return JSON.parse(
            JSON.stringify(tree, (key, value) => {
                if (key === 'parent') {
                    return undefined;
                }
                return value;
            })
        );
    }

    private isEntityHistoryEnabled(): boolean {
        let customSettings = (abp as any).custom;
        return (
            customSettings.EntityHistory &&
            customSettings.EntityHistory.isEnabled &&
            _filter(
                customSettings.EntityHistory.enabledEntities,
                (entityType) => entityType === this._entityTypeFullName
            ).length === 1
        );
    }

    private getContextMenuItems(): any[] {
        const canManageOrganizationTree = this.isGranted(
            'Pages.Administration.OrganizationUnits.ManageOrganizationTree'
        );

        let items = [
            {
                label: this.l('Edit'),
                disabled: !canManageOrganizationTree,
                command: (event) => {
                    const selected = this.selectedOu();
                    this.createOrEditOrganizationUnitModal().show({
                        id: selected.data.id,
                        displayName: selected.data.displayName,
                    });
                },
            },
            {
                label: this.l('AddSubUnit'),
                disabled: !canManageOrganizationTree,
                command: () => {
                    const selected = this.selectedOu();
                    this.addUnit(selected?.data?.id);
                },
            },
            {
                label: this.l('Delete'),
                disabled: !canManageOrganizationTree,
                command: () => {
                    const selected = this.selectedOu();
                    if (!selected) {
                        return;
                    }
                    this.message.confirm(
                        this.l('OrganizationUnitDeleteWarningMessage', selected.data.displayName),
                        this.l('AreYouSure'),
                        (isConfirmed) => {
                            if (isConfirmed) {
                                this._organizationUnitService.deleteOrganizationUnit(selected.data.id).subscribe(() => {
                                    this.deleteUnit(selected.data.id);
                                    this.notify.success(this.l('SuccessfullyDeleted'));
                                    this.selectedOu.set(null);
                                    this.reload();
                                    this.ouSelected.emit(null);
                                });
                            }
                        }
                    );
                },
            },
        ];

        if (this.isEntityHistoryEnabled()) {
            items.push({
                label: this.l('History'),
                disabled: false,
                command: (event) => {
                    const selected = this.selectedOu();
                    if (selected) {
                        this._router.navigate([
                            abp.appPath + `/app/admin/entity-changes/${selected.data.id}/${this._entityTypeFullName}`,
                        ]);
                    }
                },
            });
        }

        return items;
    }
}
