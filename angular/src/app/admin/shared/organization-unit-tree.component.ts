import { Component, inject, input, output, effect, signal } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { OrganizationUnitDto } from '@shared/service-proxies/service-proxies';
import { ArrayToTreeConverterService } from '@shared/utils/array-to-tree-converter.service';
import { TreeDataHelperService } from '@shared/utils/tree-data-helper.service';
import { TreeNode } from 'primeng/api';
import { forEach as _forEach, remove as _remove } from 'lodash-es';
import { FormsModule } from '@angular/forms';
import { TreeModule } from 'primeng/tree';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

export interface IOrganizationUnitsTreeComponentData {
    allOrganizationUnits: OrganizationUnitDto[];
    selectedOrganizationUnits: string[];
}

@Component({
    selector: 'organization-unit-tree',
    template: `
        <div class="form-group">
            <input
                id="OrganizationUnitsTreeFilter"
                type="text"
                (input)="filterOrganizationUnits($event)"
                [(ngModel)]="filter"
                class="form-control"
                placeholder="{{ 'SearchWithThreeDot' | localize }}" />
        </div>
        <p-tree
            [value]="treeData"
            selectionMode="checkbox"
            [(selection)]="selectedOus"
            (onNodeSelect)="nodeSelect($event)"
            (onNodeUnselect)="onNodeUnselect($event)"
            [propagateSelectionUp]="false"
            [propagateSelectionDown]="cascadeSelectEnabled()" />
    `,
    imports: [FormsModule, TreeModule, LocalizePipe],
})
export class OrganizationUnitsTreeComponent extends AppComponentBase {
    private _arrayToTreeConverterService = inject(ArrayToTreeConverterService);
    private _treeDataHelperService = inject(TreeDataHelperService);

    private _allOrganizationUnits: OrganizationUnitDto[];
    private _selectedOrganizationUnits: string[];

    readonly cascadeSelectEnabled = input(true);
    readonly data = input<IOrganizationUnitsTreeComponentData>(undefined);
    readonly onChange = output<any>();

    treeData: any;
    selectedOus: TreeNode[] = [];
    filter = '';

    constructor() {
        super();
        effect(() => {
            const inputData = this.data();
            if (inputData) {
                this.setTreeData(inputData.allOrganizationUnits);
                this.setSelectedNodes(inputData.selectedOrganizationUnits);

                this._allOrganizationUnits = inputData.allOrganizationUnits;
                this._selectedOrganizationUnits = inputData.selectedOrganizationUnits;
            }
        });
    }

    setTreeData(organizationUnits: OrganizationUnitDto[]) {
        this.treeData = this._arrayToTreeConverterService.createTree(
            organizationUnits,
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
                    source: 'displayName',
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
                    target: 'expanded',
                    value: true,
                },
            ]
        );
    }

    setSelectedNodes(selectedOrganizationUnits: string[]) {
        this.selectedOus = [];
        _forEach(selectedOrganizationUnits, (ou) => {
            let item = this._treeDataHelperService.findNode(this.treeData, { data: { code: ou } });
            if (item) {
                this.selectedOus.push(item);
            }
        });
    }

    getSelectedOrganizationIds(): number[] {
        if (!this.selectedOus) {
            return [];
        }

        let organizationIds = [];

        _forEach(this.selectedOus, (ou) => {
            organizationIds.push(ou.data.id);
        });

        return organizationIds;
    }

    getSelectedOrganizations(): any[] {
        if (!this.selectedOus) {
            return [];
        }

        let organizations = [];

        _forEach(this.selectedOus, (ou) => {
            organizations.push({ displayName: ou.data.displayName, id: ou.data.id });
        });

        return organizations;
    }

    filterOrganizationUnit(nodes, filterText): any {
        _forEach(nodes, (node) => {
            if (node.data.displayName.toLowerCase().indexOf(filterText.toLowerCase()) >= 0) {
                node.styleClass = this.showParentNodes(node);
            } else {
                node.styleClass = 'hidden-tree-node';
            }

            if (node.children) {
                this.filterOrganizationUnit(node.children, filterText);
            }
        });
    }

    showParentNodes(node): void {
        if (!node.parent) {
            return;
        }

        node.parent.styleClass = '';
        this.showParentNodes(node.parent);
    }

    filterOrganizationUnits(event): void {
        this.filterOrganizationUnit(this.treeData, this.filter);
    }

    nodeSelect(event) {
        // TODO: The 'emit' function requires a mandatory any argument
        this.onChange.emit(null);

        if (!this.cascadeSelectEnabled()) {
            return;
        }

        let parentNode = this._treeDataHelperService.findParent(this.treeData, { data: { id: event.node.data.id } });

        while (parentNode != null) {
            this.selectedOus.push(parentNode);
            parentNode = this._treeDataHelperService.findParent(this.treeData, { data: { id: parentNode.data.id } });
        }
    }

    onNodeUnselect(event) {
        _remove(this.selectedOus, (x) => x.data.id === event.node.data.id);
        // TODO: The 'emit' function requires a mandatory any argument
        this.onChange.emit(null);
    }
}
