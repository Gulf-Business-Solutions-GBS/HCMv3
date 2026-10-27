import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { CreateOrEditWorkflowGroupDto, WorkflowGroupsServiceProxy } from '@shared/service-proxies/service-proxies';
import { TabsetComponent, TabDirective } from 'ngx-bootstrap/tabs';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { WorkflowGroupDetailsComponent } from './workflow-group-details/workflow-group-details.component';
import { WorkflowGroupFieldAccessesComponent } from './workflow-group-field-accesses/workflow-group-field-accesses.component';

@Component({
    templateUrl: './workflow-group-detail.component.html',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        TabsetComponent,
        TabDirective,
        LocalizePipe,
        WorkflowGroupDetailsComponent,
        WorkflowGroupFieldAccessesComponent,
    ],
})
export class WorkflowGroupDetailComponent extends AppComponentBase implements OnInit {
    private _route = inject(ActivatedRoute);
    private _service = inject(WorkflowGroupsServiceProxy);

    readonly workflowGroupId = signal<number | undefined>(undefined);
    readonly workflowGroup = signal<CreateOrEditWorkflowGroupDto | undefined>(undefined);

    ngOnInit(): void {
        this._route.paramMap.subscribe((params) => {
            const id = Number(params.get('id'));
            if (id) {
                this.workflowGroupId.set(id);
                this.loadWorkflowGroup(id);
            }
        });
    }

    private loadWorkflowGroup(id: number): void {
        this._service.getWorkflowGroupForEdit(id).subscribe((result) => {
            this.workflowGroup.set(result.workflowGroup);
        });
    }
}
