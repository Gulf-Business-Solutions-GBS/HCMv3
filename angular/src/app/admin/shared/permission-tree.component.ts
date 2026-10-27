import { Component, inject, input, signal, effect, untracked } from '@angular/core';
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
    private filterTimer: ReturnType<typeof setTimeout> | undefined;

    /** The tree before any search: every permission, or only the granted ones when that switch is on. */
    private baseTree: TreeNode[] = [];

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
            if (!val) {
                return;
            }

            // Only new edit data may run this. Everything below reads the tree it has just set; left tracked, each
            // search or tick would re-run it, rebuild the whole tree and throw the search away -- which is exactly
            // how the tree used to flicker and never stay filtered.
            untracked(() => {
                this.onlyShowGrantedPermissions.set(false);
                this.filter.set('');
                this.setTreeData(val.permissions);
                this.setSelectedNodes(val.grantedPermissionNames);
            });
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
        this.baseTree = builtTree;
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

        // Once per permission: a node ticked while searching is a copy of the one ticked before the search.
        return Array.from(new Set(currentSelected.map((node) => node.data.name)));
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
        let parentNode = this._treeDataHelperService.findParent(this.fullTreeData(), {
            data: { name: event.node.data.name },
        });

        while (parentNode != null) {
            currentSelected.push(parentNode);
            parentNode = this._treeDataHelperService.findParent(this.fullTreeData(), {
                data: { name: parentNode.data.name },
            });
        }
        this.selectedPermissions.set(currentSelected);
    }

    onNodeUnselect(event) {
        if (this.disableCascade()) {
            return;
        }

        let childrenNodes = this._treeDataHelperService.findChildren(this.fullTreeData(), {
            data: { name: event.node.data.name },
        });
        childrenNodes.push(event.node.data.name);
        const currentSelected = [...this.selectedPermissions()];
        _remove(currentSelected, (x) => childrenNodes.indexOf(x.data.name) !== -1);
        this.selectedPermissions.set(currentSelected);
    }

    /**
     * Typing in the search box. Waits until typing pauses, so the tree is filtered once per word rather than once
     * per letter -- redrawing a couple of thousand nodes on every keystroke is what made the tree flicker.
     */
    onFilterChange(value: string): void {
        this.filter.set(value);
        clearTimeout(this.filterTimer);
        this.filterTimer = setTimeout(() => this.filterPermissions(), 250);
    }

    /**
     * Shows only the permissions matching the search, with the folders that lead to them opened; an empty search
     * shows the whole tree again.
     *
     * The filtered tree is made of NEW node objects. PrimeNG draws each node OnPush, so it only notices a node that
     * was replaced: hiding or opening nodes by changing them in place never reaches the screen. Ticks carry over
     * because PrimeNG matches selected nodes by key.
     */
    filterPermissions(): void {
        const text = (this.filter() ?? '').trim().toLowerCase();
        this.treeData.set(text ? this.filteredCopy(this.baseTree, text, false).nodes : this.baseTree);
    }

    /**
     * A copy of the nodes that match, lead to a match, or sit under a folder that matches (so a matching folder
     * shows its contents). A folder is opened only when the match is somewhere below it.
     */
    private filteredCopy(
        nodes: TreeNode[] | undefined,
        text: string,
        ancestorMatches: boolean
    ): { nodes: TreeNode[]; anyMatch: boolean } {
        const kept: TreeNode[] = [];
        let anyMatch = false;

        for (const node of nodes ?? []) {
            const selfMatches =
                (node.data?.displayName ?? '').toLowerCase().includes(text) ||
                (node.data?.name ?? '').toLowerCase().includes(text);
            const below = this.filteredCopy(node.children, text, ancestorMatches || selfMatches);

            if (selfMatches || below.anyMatch || ancestorMatches) {
                kept.push({ ...node, children: below.nodes, expanded: below.anyMatch });
            }

            anyMatch = anyMatch || selfMatches || below.anyMatch;
        }

        return { nodes: kept, anyMatch };
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

            this.baseTree = newTreeData;
            this.filterPermissions();
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
