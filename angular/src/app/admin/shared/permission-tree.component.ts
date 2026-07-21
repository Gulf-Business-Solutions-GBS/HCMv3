import { Component, inject, input, signal, effect } from '@angular/core';
import { PermissionTreeEditModel } from '@app/admin/shared/permission-tree-edit.model';
import { AppComponentBase } from '@shared/common/app-component-base';
import { ArrayToTreeConverterService } from '@shared/utils/array-to-tree-converter.service';
import { TreeDataHelperService } from '@shared/utils/tree-data-helper.service';
import { FlatPermissionDto } from '@shared/service-proxies/service-proxies';
import { TreeNode } from 'primeng/api';
import { forEach as _forEach, remove as _remove } from 'lodash-es';
import { FormsModule } from '@angular/forms';
import { TreeModule } from 'primeng/tree';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { Tooltip } from 'primeng/tooltip';
import { NgClass } from '@angular/common';

@Component({
    selector: 'permission-tree',
    templateUrl: './permission-tree.component.html',
    styleUrls: ['./permission-tree.component.less'],
    imports: [FormsModule, TreeModule, Tooltip, LocalizePipe, NgClass],
})
export class PermissionTreeComponent extends AppComponentBase {
    private _arrayToTreeConverterService = inject(ArrayToTreeConverterService);
    private _treeDataHelperService = inject(TreeDataHelperService);
    private originalPermissions: FlatPermissionDto[] = [];

    readonly singleSelect = input<boolean>(undefined);
    readonly disableCascade = input<boolean>(undefined);
    readonly editData = input<PermissionTreeEditModel>(undefined);

    readonly treeData = signal<TreeNode[]>([]);
    readonly selectedPermissions = signal<TreeNode[]>([]);
    readonly filter = signal<string>('');
    readonly onlyShowGrantedPermissions = signal<boolean>(false);
    readonly isTreeExpanded = signal<boolean>(true);
    readonly fullTreeData = signal<TreeNode[]>([]);
    readonly isProcessing = signal<boolean>(false);

    constructor() {
        super();
        effect(() => {
            const val = this.editData();
            if (val) {
                this.onlyShowGrantedPermissions.set(false);
                this.setTreeData(val.permissions);

                let newTreeData: TreeNode[];

                if (this.onlyShowGrantedPermissions()) {
                    const grantedPermissionNames = val.grantedPermissionNames;
                    const grantedPermissions = this.originalPermissions.filter((p) =>
                        grantedPermissionNames.includes(p.name)
                    );
                    newTreeData = this.buildTree(grantedPermissions);
                } else {
                    newTreeData = this.fullTreeData();
                }

                this.treeData.set(newTreeData);
                this.setSelectedNodes(val.grantedPermissionNames);
            }
        });
    }

    get onlyShowEnabledPermissionsLabel(): string {
        return this.onlyShowGrantedPermissions() ? 'ShowAllPermissions' : 'OnlyShowEnabledPermissions';
    }

    get tooltipText(): string {
        return this.onlyShowGrantedPermissions() ? 'ShowAllPermissionsTooltip' : 'OnlyShowEnabledPermissionsTooltip';
    }

    setTreeData(permissions: FlatPermissionDto[]) {
        this.originalPermissions = permissions;
        const builtTree = this.buildTree(permissions);
        this.fullTreeData.set(builtTree);
        this.treeData.set(builtTree);
    }

    setSelectedNodes(grantedPermissionNames: string[]) {
        const selected: TreeNode[] = [];
        _forEach(grantedPermissionNames, (permission) => {
            let item = this._treeDataHelperService.findNode(this.treeData(), { data: { name: permission } });
            if (item) {
                selected.push(item);
            }
        });
        this.selectedPermissions.set(selected);
    }

    getGrantedPermissionNames(): string[] {
        const currentSelected = this.selectedPermissions();
        if (!currentSelected || !currentSelected.length) {
            return [];
        }

        let permissionNames = [];

        for (let i = 0; i < currentSelected.length; i++) {
            permissionNames.push(currentSelected[i].data.name);
        }

        return permissionNames;
    }

    nodeSelect(event) {
        if (this.singleSelect()) {
            this.selectedPermissions.set([event.node]);
            return;
        }

        if (this.disableCascade()) {
            return;
        }

        const currentSelected = [...this.selectedPermissions()];
        let parentNode = this._treeDataHelperService.findParent(this.treeData(), {
            data: { name: event.node.data.name },
        });

        while (parentNode != null) {
            currentSelected.push(parentNode);
            parentNode = this._treeDataHelperService.findParent(this.treeData(), {
                data: { name: parentNode.data.name },
            });
        }
        this.selectedPermissions.set(currentSelected);
    }

    onNodeUnselect(event) {
        if (this.disableCascade()) {
            return;
        }

        let childrenNodes = this._treeDataHelperService.findChildren(this.treeData(), {
            data: { name: event.node.data.name },
        });
        childrenNodes.push(event.node.data.name);
        const currentSelected = [...this.selectedPermissions()];
        _remove(currentSelected, (x) => childrenNodes.indexOf(x.data.name) !== -1);
        this.selectedPermissions.set(currentSelected);
    }

    filterPermissions(): void {
        if (!this.filter() || !this.filter().trim()) {
            if (this.onlyShowGrantedPermissions()) {
                const grantedPermissionNames = this.getGrantedPermissionNames();
                const grantedPermissions = this.originalPermissions.filter((p) =>
                    grantedPermissionNames.includes(p.name)
                );
                this.treeData.set(this.buildTree(grantedPermissions));
            } else {
                this.treeData.set(this.buildTree(this.originalPermissions));
            }
        } else {
            const currentTreeData = this.treeData();
            this.filterPermission(currentTreeData, this.filter());
            this.treeData.set([...currentTreeData]);
        }
    }

    filterPermission(nodes, filterText): any {
        _forEach(nodes, (node) => {
            const matchesFilter = node.data.displayName.toLowerCase().includes(filterText.toLowerCase());

            if (matchesFilter) {
                node.styleClass = '';
                this.showParentNodes(node);

                if (node.children) {
                    _forEach(node.children, (child) => {
                        child.styleClass = '';
                    });
                }
            } else {
                node.styleClass = 'hidden-tree-node';

                if (node.children) {
                    const childMatches = this.filterPermission(node.children, filterText);

                    if (childMatches) {
                        node.styleClass = '';
                    }
                }
            }
        });

        return nodes.some((n) => !n.styleClass || n.styleClass === '');
    }

    showParentNodes(node): void {
        if (!node.parent) {
            return;
        }

        node.parent.styleClass = '';
        this.showParentNodes(node.parent);
    }

    onOnlyShowGrantedPermissionsChange(value: boolean): void {
        this.isTreeExpanded.set(true);
        this.isProcessing.set(true);

        requestAnimationFrame(() => {
            let newTreeData: TreeNode[];

            if (value) {
                const grantedPermissionNames = this.getGrantedPermissionNames();
                const grantedPermissions = this.originalPermissions.filter((p) =>
                    grantedPermissionNames.includes(p.name)
                );
                newTreeData = this.buildTree(grantedPermissions);
            } else {
                newTreeData = this.buildTree(this.originalPermissions);
            }

            if (this.filter() && this.filter().trim()) {
                this.filterPermission(newTreeData, this.filter());
            }

            this.treeData.set(newTreeData);
            this.isProcessing.set(false);
        });
    }

    toggleAllNodes(): void {
        this.isTreeExpanded.update((expanded) => !expanded);
        const currentTreeData = this.treeData();
        _forEach(currentTreeData, (node) => {
            this.expandRecursive(node, this.isTreeExpanded());
        });
        this.treeData.set([...currentTreeData]);
    }

    private expandRecursive(node: TreeNode, isExpand: boolean): void {
        node.expanded = isExpand;
        if (node.children) {
            node.children.forEach((childNode) => {
                this.expandRecursive(childNode, isExpand);
            });
        }
    }

    private buildTree(permissions: FlatPermissionDto[]): TreeNode[] {
        const tree = this._arrayToTreeConverterService.createTree(permissions, 'parentName', 'name', null, 'children', [
            { target: 'key', source: 'name' },
            { target: 'label', source: 'displayName' },
            { target: 'expandedIcon', value: 'fa fa-folder-open text-warning' },
            { target: 'collapsedIcon', value: 'fa fa-folder text-warning' },
            { target: 'expanded', value: false },
        ]);
        // Expand only first level nodes
        for (const node of tree) {
            node.expanded = true;
        }
        return tree;
    }
}
