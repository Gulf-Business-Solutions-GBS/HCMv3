import { Component, inject, signal } from '@angular/core';
import { FeatureTreeEditModel } from '@app/admin/shared/feature-tree-edit.model';
import { AppComponentBase } from '@shared/common/app-component-base';
import { FlatFeatureDto, NameValueDto } from '@shared/service-proxies/service-proxies';
import { ArrayToTreeConverterService } from '@shared/utils/array-to-tree-converter.service';
import { TreeDataHelperService } from '@shared/utils/tree-data-helper.service';
import { TreeNode } from 'primeng/api';
import { find as _find, forEach as _forEach, filter as _filter, remove as _remove } from 'lodash-es';
import { TreeModule } from 'primeng/tree';

import { FormsModule } from '@angular/forms';

@Component({
    selector: 'feature-tree',
    templateUrl: './feature-tree.component.html',
    styleUrls: ['./feature-tree.component.less'],
    imports: [TreeModule, FormsModule],
})
export class FeatureTreeComponent extends AppComponentBase {
    private _arrayToTreeConverterService = inject(ArrayToTreeConverterService);
    private _treeDataHelperService = inject(TreeDataHelperService);

    _editData: FeatureTreeEditModel;
    treeData = signal<any>(null);
    selectedFeatures = signal<TreeNode[]>([]);

    setEditData(val: FeatureTreeEditModel): void {
        this._editData = val;
        this.setTreeData(val.features);
        this.setSelectedNodes(val);
    }

    setTreeData(permissions: FlatFeatureDto[]) {
        this.treeData.set(
            this._arrayToTreeConverterService.createTree(permissions, 'parentName', 'name', null, 'children', [
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
                {
                    target: 'selectable',
                    targetFunction(item) {
                        return item.inputType.name === 'CHECKBOX';
                    },
                },
            ])
        );
    }

    setSelectedNodes(val: FeatureTreeEditModel) {
        const selected: TreeNode[] = [];
        _forEach(val.features, (feature) => {
            let items = _filter(val.featureValues, { name: feature.name });
            if (items && items.length === 1) {
                let item = items[0];
                this.setSelectedNode(item.name, item.value, selected);
            } else {
                this.setSelectedNode(feature.name, feature.defaultValue, selected);
            }
        });
        this.selectedFeatures.set(selected);
    }

    setSelectedNode(featureName, value, selectedFeaturesArray: TreeNode[]) {
        let node;

        const normalizedValue = value.trim().toLowerCase();

        if (normalizedValue === 'true') {
            node = this._treeDataHelperService.findNode(this.treeData(), { data: { name: featureName } });
            if (node) {
                selectedFeaturesArray.push(node);
            }
        } else if (value && normalizedValue !== 'false') {
            node = this._treeDataHelperService.findNode(this.treeData(), { data: { name: featureName } });
            if (node) {
                node.value = value;
                selectedFeaturesArray.push(node);
            }
        }
    }

    getGrantedFeatures(): NameValueDto[] {
        if (!this._editData.features) {
            return [];
        }

        let features: NameValueDto[] = [];

        for (let i = 0; i < this._editData.features.length; i++) {
            let feature = new NameValueDto();
            feature.name = this._editData.features[i].name;
            feature.value = this.getFeatureValueByName(feature.name);

            features.push(feature);
        }

        return features;
    }

    onDropdownChange(node) {
        if (node.value) {
            node.selected = true;
        }
    }

    onInputChange(node) {
        if (node.value) {
            node.selected = true;
        }
    }

    findFeatureByName(featureName: string): FlatFeatureDto {
        const self = this;

        const feature = _find(self._editData.features, (f) => f.name === featureName);

        if (!feature) {
            abp.log.warn('Could not find a feature by name: ' + featureName);
        }

        return feature;
    }

    findFeatureValueByName(featureName: string) {
        const self = this;
        const feature = self.findFeatureByName(featureName);
        if (!feature) {
            return '';
        }

        const featureValue = _find(self._editData.featureValues, (f) => f.name === featureName);
        if (!featureValue) {
            return feature.defaultValue;
        }

        return featureValue.value;
    }

    isFeatureValueValid(featureName: string, value: string): boolean {
        const self = this;
        const feature = self.findFeatureByName(featureName);
        if (!feature || !feature.inputType || !feature.inputType.validator) {
            return true;
        }

        const validator = feature.inputType.validator as any;
        if (validator.name === 'STRING') {
            if (value === undefined || value === null) {
                return validator.attributes.AllowNull;
            }

            if (typeof value !== 'string') {
                return false;
            }

            if (validator.attributes.MinLength > 0 && value.length < validator.attributes.MinLength) {
                return false;
            }

            if (validator.attributes.MaxLength > 0 && value.length > validator.attributes.MaxLength) {
                return false;
            }

            if (validator.attributes.RegularExpression) {
                return new RegExp(validator.attributes.RegularExpression).test(value);
            }
        } else if (validator.name === 'NUMERIC') {
            const numValue = parseInt(value);

            if (isNaN(numValue)) {
                return false;
            }

            const minValue = validator.attributes.MinValue;
            if (minValue > numValue) {
                return false;
            }

            const maxValue = validator.attributes.MaxValue;
            if (maxValue > 0 && numValue > maxValue) {
                return false;
            }
        }

        return true;
    }

    areAllValuesValid(): boolean {
        let result = true;

        _forEach(this._editData.features, (feature) => {
            let value = this.getFeatureValueByName(feature.name);
            if (!this.isFeatureValueValid(feature.name, value)) {
                result = false;
            }
        });

        return result;
    }

    setFeatureValueByName(featureName: string, value: string): void {
        const featureValue = _find(this._editData.featureValues, (f) => f.name === featureName);
        if (!featureValue) {
            return;
        }

        featureValue.value = value;
    }

    isFeatureSelected(name: string): boolean {
        let nodes = _filter(this.selectedFeatures(), { data: { name: name } });
        return nodes && nodes.length === 1;
    }

    getFeatureValueByName(featureName: string): string {
        let feature = this._treeDataHelperService.findNode(this.treeData(), { data: { name: featureName } });
        if (!feature) {
            return null;
        }

        if (feature.value) {
            return feature.value;
        }

        if (!this.isFeatureSelected(featureName)) {
            return 'false';
        }

        return 'true';
    }

    isFeatureEnabled(featureName: string): boolean {
        const self = this;
        const value = self.findFeatureValueByName(featureName);
        return value.toLowerCase() === 'true';
    }

    nodeSelect(event) {
        let parentNode = this._treeDataHelperService.findParent(this.treeData(), {
            data: { name: event.node.data.name },
        });

        const currentSelected = [...this.selectedFeatures()];
        while (parentNode != null) {
            const isParentNodeAdded = _find(currentSelected, (f) => f.data.name === parentNode.data.name);
            if (!isParentNodeAdded) {
                currentSelected.push(parentNode);
            }

            parentNode = this._treeDataHelperService.findParent(this.treeData(), {
                data: { name: parentNode.data.name },
            });
        }
        this.selectedFeatures.set(currentSelected);
    }

    onNodeUnselect(event) {
        let childrenNodes = this._treeDataHelperService.findChildren(this.treeData(), {
            data: { name: event.node.data.name },
        });
        childrenNodes.push(event.node.data.name);
        const currentSelected = [...this.selectedFeatures()];
        _remove(currentSelected, (x) => childrenNodes.indexOf(x.data.name) !== -1);
        this.selectedFeatures.set(currentSelected);
    }
}
