import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TreeNode } from 'primeng/api';
import { TreeModule } from 'primeng/tree';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { OrgUnitTreeNodeDto, OrgUnitTreeReportServiceProxy } from '@shared/service-proxies/service-proxies';

/**
 * Organization Unit tree browse.
 *
 * Ported legacy screen: HCM10.Web/UserControls/Reports/Analysis/Organization/uscOrgUnitTree.ascx(.cs).
 *
 * UNLIKE the rest of the "Analysis" report family this is NOT a filtered/paged grid: the backend
 * (`OrgUnitTreeReportAppService.GetOrgUnitTree`) returns the WHOLE org unit hierarchy already nested as
 * `OrgUnitTreeNodeDto.Children`, mirroring the legacy DevExpress `TreeViewNode` tree it replaces, so this
 * screen is a single no-input fetch rendered through a PrimeNG `<p-tree>`.
 *
 * Legacy's `txtSearch`/`btnSearch` "find and expand to node" was pure client-side DOM manipulation over
 * the already-rendered tree (`FindAllRecursive` + `ExpandToNode`, no server involvement) -- ported here as
 * a client-side text filter using PrimeNG's built-in tree filtering instead of reproducing the same
 * find/expand mechanics by hand.
 */
@Component({
    templateUrl: './org-unit-tree-report.component.html',
    animations: [appModuleAnimation()],
    imports: [SubHeaderComponent, BusyIfDirective, FormsModule, TreeModule, LocalizePipe],
})
export class OrgUnitTreeReportComponent extends AppComponentBase implements OnInit {
    private readonly _reportService = inject(OrgUnitTreeReportServiceProxy);

    readonly loading = signal<boolean>(true);
    readonly treeData = signal<TreeNode[]>([]);
    readonly filterText = signal<string>('');

    ngOnInit(): void {
        this.loading.set(true);
        this._reportService.getOrgUnitTree().subscribe((result) => {
            this.treeData.set((result ?? []).map((node) => this.toTreeNode(node)));
            this.loading.set(false);
        });
    }

    private toTreeNode(node: OrgUnitTreeNodeDto): TreeNode {
        return {
            key: String(node.id),
            label: node.name,
            data: node,
            expandedIcon: 'fa fa-folder-open text-warning',
            collapsedIcon: 'fa fa-folder text-warning',
            children: (node.children ?? []).map((child) => this.toTreeNode(child)),
        };
    }
}
