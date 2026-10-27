import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { EntityAndPropertyChangeListDto, EntityChangeServiceProxy } from '@shared/service-proxies/service-proxies';
import { ActivatedRoute } from '@angular/router';
import { NgClass, DatePipe } from '@angular/common';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    templateUrl: './entity-changes.component.html',
    styleUrls: ['./entity-changes.component.less'],
    animations: [appModuleAnimation()],
    imports: [NgClass, DatePipe, LocalizePipe],
})
export class EntityChangesComponent extends AppComponentBase implements OnInit {
    private _entityChangeService = inject(EntityChangeServiceProxy);
    private _activatedRoute = inject(ActivatedRoute);

    readonly entityAndPropertyChanges = signal<EntityAndPropertyChangeListDto[]>([]);
    readonly entityTypeFullName = signal<string>('');
    readonly entityTypeShortName = computed(() => this.entityTypeFullName().split('.').pop() || '');
    readonly entityId = signal<string>('');

    ngOnInit(): void {
        this.loadEntityChanges();
    }

    loadEntityChanges(): void {
        this.entityId.set(this._activatedRoute.snapshot.params['entityId']);
        this.entityTypeFullName.set(this._activatedRoute.snapshot.params['entityTypeFullName']);

        this._entityChangeService
            .getEntityChangesByEntity(this.entityTypeFullName(), this.entityId())
            .subscribe((data) => {
                this.entityAndPropertyChanges.set(data.items);
            });
    }
}
