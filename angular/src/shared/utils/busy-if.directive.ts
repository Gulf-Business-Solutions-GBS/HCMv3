import {
    Directive,
    ComponentFactoryResolver,
    ViewContainerRef,
    OnChanges,
    SimpleChanges,
    inject,
    input,
    signal,
} from '@angular/core';
import { NgxSpinnerService, NgxSpinnerComponent } from 'ngx-spinner';

@Directive({ selector: '[busyIf]' })
export class BusyIfDirective implements OnChanges {
    private _viewContainer = inject(ViewContainerRef);
    private _componentFactoryResolver = inject(ComponentFactoryResolver);

    private ngxSpinnerService = inject(NgxSpinnerService);

    private static index = 0;

    private spinnerName = '';

    readonly busyIf = input<boolean>(undefined);

    readonly isBusy = signal<boolean>(false);

    constructor() {
        this.loadComponent();
    }

    refreshState(): void {
        if (this.isBusy() === undefined || this.spinnerName === '') {
            return;
        }

        if (this.isBusy()) {
            this.ngxSpinnerService.show(this.spinnerName);
        } else {
            this.ngxSpinnerService.hide(this.spinnerName);
        }
    }

    loadComponent() {
        const componentFactory = this._componentFactoryResolver.resolveComponentFactory(NgxSpinnerComponent);
        const componentRef = this._viewContainer.createComponent(componentFactory);

        this.spinnerName = 'busyIfSpinner-' + BusyIfDirective.index++ + '-' + Math.floor(Math.random() * 1000000);

        const component = componentRef.instance as NgxSpinnerComponent;

        component.name = this.spinnerName;
        component.fullScreen = false;

        component.type = 'ball-clip-rotate';
        component.size = 'medium';
        component.color = '#5ba7ea';
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes.busyIf) {
            this.isBusy.set(changes.busyIf.currentValue);
            this.refreshState();
        }
    }
}
