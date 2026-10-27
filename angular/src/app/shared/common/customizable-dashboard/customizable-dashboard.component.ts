import {
    Component,
    OnInit,
    Injector,
    OnDestroy,
    Injectable,
    InjectionToken,
    inject,
    input,
    viewChild,
    signal,
    WritableSignal,
} from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { DashboardViewConfigurationService } from './dashboard-view-configuration.service';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { GridsterConfig, GridsterItemConfig, Gridster, GridsterItem } from 'angular-gridster2';
import {
    DashboardCustomizationServiceProxy,
    DashboardOutput,
    AddNewPageInput,
    AddNewPageOutput,
    AddWidgetInput,
    RenamePageInput,
    SavePageInput,
    Page,
    Widget,
    WidgetFilterOutput,
    WidgetOutput,
    Dashboard,
} from '@shared/service-proxies/service-proxies';
import { TabsetComponent, TabDirective } from 'ngx-bootstrap/tabs';
import { WidgetViewDefinition, WidgetFilterViewDefinition } from './definitions';
import { AddWidgetModalComponent } from './add-widget-modal/add-widget-modal.component';
import { DashboardCustomizationConst } from './DashboardCustomizationConsts';
import { ModalDirective } from 'ngx-bootstrap/modal';
import * as rtlDetect from 'rtl-detect';
import { Subject, forkJoin } from 'rxjs';
import { GuidGeneratorService } from '@shared/utils/guid-generator.service';
import { SubHeaderComponent } from '../sub-header/sub-header.component';
import { NgClass, NgComponentOutlet } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Overlay, OverlayModule } from 'primeng/overlay';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

export const WIDGETONRESIZEEVENTHANDLERTOKEN = new InjectionToken<WidgetOnResizeEventHandler>(
    'WidgetOnResizeEventHandlerToken'
);

@Injectable()
export class WidgetOnResizeEventHandler {
    onResize: Subject<any> = new Subject();
}

@Component({
    selector: 'customizable-dashboard',
    templateUrl: './customizable-dashboard.component.html',
    styleUrls: ['./customizable-dashboard.component.css'],
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        TabsetComponent,
        NgClass,
        TabDirective,
        Gridster,
        GridsterItem,
        NgComponentOutlet,
        FormsModule,
        OverlayModule,
        AddWidgetModalComponent,
        AppBsModalDirective,
        LocalizePipe,
    ],
})
export class CustomizableDashboardComponent extends AppComponentBase implements OnInit, OnDestroy {
    private _injector = inject(Injector);
    private _dashboardViewConfiguration = inject(DashboardViewConfigurationService);
    private _dashboardCustomizationServiceProxy = inject(DashboardCustomizationServiceProxy);
    private _guidGenerator = inject(GuidGeneratorService);

    readonly dashboardName = input<string>(undefined);
    readonly addWidgetModal = viewChild<AddWidgetModalComponent>('addWidgetModal');
    readonly dashboardTabs = viewChild<TabsetComponent>('dashboardTabs');
    readonly modal = viewChild<ModalDirective>('filterModal');
    readonly addPagePanel = viewChild<Overlay>('addPagePanel');
    readonly renamePagePanel = viewChild<Overlay>('renamePagePanel');

    readonly loading = signal<boolean>(true);
    readonly busy = signal<boolean>(true);
    readonly editModeEnabled = signal<boolean>(false);

    readonly options = signal<GridsterConfig[]>([]);

    dashboardDefinition: DashboardOutput;
    readonly userDashboard = signal<any>(null);

    readonly selectedPage: WritableSignal<{ id: string; name: string }> = signal({
        id: '',
        name: '',
    });

    readonly renamePageInput = signal<string>('');
    readonly addPageInput = signal<string>('');
    readonly addPagePanelVisible = signal<boolean>(false);
    readonly renamePagePanelVisible = signal<boolean>(false);

    readonly widgetSubjects = signal<{
        [key: string]: {
            handler: WidgetOnResizeEventHandler;
            injector: Injector;
        };
    }>({});

    ngOnInit() {
        this.loading.set(true);

        forkJoin([
            this._dashboardCustomizationServiceProxy.getUserDashboard(
                this.dashboardName(),
                DashboardCustomizationConst.Applications.Angular
            ),
            this._dashboardCustomizationServiceProxy.getDashboardDefinition(
                this.dashboardName(),
                DashboardCustomizationConst.Applications.Angular
            ),
        ]).subscribe(([userDashboardResultFromServer, dashboardDefinitionResult]) => {
            this.dashboardDefinition = dashboardDefinitionResult;
            if (!this.dashboardDefinition.widgets || this.dashboardDefinition.widgets.length === 0) {
                this.loading.set(false);
                this.busy.set(false);
                return;
            }

            if (!userDashboardResultFromServer.pages || userDashboardResultFromServer.pages.length === 0) {
                this.loading.set(false);
                this.busy.set(false);
                return;
            }

            this.initializeUserDashboardDefinition(userDashboardResultFromServer, dashboardDefinitionResult);
            this.initializeUserDashboardFilters();

            if (this.userDashboard()?.pages?.length) {
                this.selectedPage.set({
                    id: this.userDashboard().pages[0].id,
                    name: this.userDashboard().pages[0].name,
                });
            }

            this.loading.set(false);
            this.busy.set(false);
        });

        this.subscribeToEvent('app.kt_aside_toggler.onClick', this.onMenuToggle);
    }

    initializeUserDashboardDefinition(
        userDashboardResultFromServer: Dashboard,
        dashboardDefinitionResult: DashboardOutput
    ) {
        this.userDashboard.set({
            dashboardName: this.dashboardName(),
            filters: [],
            pages: userDashboardResultFromServer.pages.map((page) => {
                //gridster should has its own options
                this.options.update((opts) => [...opts, this.getGridsterConfig()]);

                if (!page.widgets) {
                    return {
                        id: page.id,
                        name: page.name,
                        widgets: [],
                    };
                }

                //only use widgets which dashboard definition contains and have view definition
                //(dashboard definition can be changed after users save their dashboard, because it depends on permissions and other stuff)
                page.widgets = page.widgets.filter(
                    (w) =>
                        dashboardDefinitionResult.widgets.find((d) => d.id === w.widgetId) &&
                        this.getWidgetViewDefinition(w.widgetId)
                );

                return {
                    id: page.id,
                    name: page.name,
                    widgets: page.widgets.map((widget) => ({
                        id: widget.widgetId,
                        guid: this._guidGenerator.guid(),
                        //View definitions are stored in the angular side(a component of widget/filter etc.) get view definition and use defined component
                        component: this.getWidgetViewDefinition(widget.widgetId).component,
                        gridInformation: {
                            id: widget.widgetId,
                            cols: widget.width,
                            rows: widget.height,
                            x: widget.positionX,
                            y: widget.positionY,
                        },
                    })),
                };
            }),
        });

        this.createWidgetSubjects();
    }

    removeItem(item: GridsterItemConfig) {
        let page = this.userDashboard().pages.find((p) => p.id === this.selectedPage().id);
        let widget = page.widgets.find((w) => w.id === item.id);
        let widgetDefinition = this.dashboardDefinition.widgets.find(
            (widgetDef: WidgetOutput) => widgetDef.id === item.id
        );

        if (!widget || !widgetDefinition) {
            return;
        }

        this.message.confirm(
            this.l('WidgetDeleteWarningMessage', this.l(widgetDefinition.name), this.selectedPage().name),
            this.l('AreYouSure'),
            (isConfirmed) => {
                if (isConfirmed) {
                    this.busy.set(true);
                    this.userDashboard.update((d) => ({
                        ...d,
                        pages: d.pages.map((p) =>
                            p.id === this.selectedPage().id
                                ? { ...p, widgets: p.widgets.filter((w) => w.id !== item.id) }
                                : p
                        ),
                    }));
                    this.savePageAfterWidgetRemoval();
                }
            }
        );
    }

    addWidget(widgetId: any): void {
        if (!widgetId) {
            return;
        }

        let widgetViewConfiguration = this._dashboardViewConfiguration.WidgetViewDefinitions.find(
            (w) => w.id === widgetId
        );
        if (!widgetViewConfiguration) {
            abp.notify.error(this.l('ThereIsNoViewConfigurationForX', widgetId));
            return;
        }

        this.busy.set(true);

        this._dashboardCustomizationServiceProxy
            .addWidget(
                new AddWidgetInput({
                    widgetId: widgetId,
                    pageId: this.selectedPage().id,
                    dashboardName: this.dashboardName(),
                    width: widgetViewConfiguration.defaultWidth,
                    height: widgetViewConfiguration.defaultHeight,
                    application: DashboardCustomizationConst.Applications.Angular,
                })
            )
            .subscribe((addedWidget) => {
                const newWidget = {
                    id: widgetId,
                    guid: this._guidGenerator.guid(),
                    component: widgetViewConfiguration.component,
                    gridInformation: {
                        id: widgetId,
                        cols: addedWidget.width,
                        rows: addedWidget.height,
                        x: addedWidget.positionX,
                        y: addedWidget.positionY,
                    },
                };

                this.createWidgetSubject(newWidget.guid);

                this.userDashboard.update((d) => ({
                    ...d,
                    pages: d.pages.map((page) =>
                        page.id === this.selectedPage().id ? { ...page, widgets: [...page.widgets, newWidget] } : page
                    ),
                }));

                this.initializeUserDashboardFilters();

                this.busy.set(false);
                this.notify.success(this.l('SavedSuccessfully'));
            });
    }

    changeEditMode(): void {
        this.editModeEnabled.set(!this.editModeEnabled());
        this.refreshAllGrids();
    }

    refreshAllGrids(): void {
        // Update options with new references to trigger change detection
        this.options.update((opts) =>
            opts.map((option) => ({
                ...option,
                draggable: {
                    ...option.draggable,
                    enabled: this.editModeEnabled(),
                },
                resizable: {
                    ...option.resizable,
                    enabled: this.editModeEnabled(),
                },
            }))
        );

        // Also trigger resize after a short delay
        setTimeout(() => {
            this.options().forEach((option) => {
                if (option.api) {
                    option.api.resize();
                }
            });
        }, 50);
    }

    openAddWidgetModal(): void {
        let page = this.userDashboard().pages.find((page) => page.id === this.selectedPage().id);
        if (page) {
            this.addWidgetModal().show(this.dashboardName(), this.selectedPage().id);
        }
    }

    addNewPage(pageName: string): void {
        if (!pageName || pageName.trim() === '') {
            this.notify.warn(this.l('PageNameCanNotBeEmpty'));
            return;
        }

        pageName = pageName.trim();

        this.busy.set(true);
        this._dashboardCustomizationServiceProxy
            .addNewPage(
                new AddNewPageInput({
                    dashboardName: this.dashboardName(),
                    name: pageName,
                    application: DashboardCustomizationConst.Applications.Angular,
                })
            )
            .subscribe((result: AddNewPageOutput) => {
                //gridster options for new page
                this.options.update((opts) => [...opts, this.getGridsterConfig()]);

                this.userDashboard.update((d) => ({
                    ...d,
                    pages: [
                        ...d.pages,
                        {
                            id: result.pageId,
                            name: pageName,
                            widgets: [],
                        },
                    ],
                }));

                this.busy.set(false);
                this.notify.success(this.l('SavedSuccessfully'));

                if (this.selectedPage().id === '') {
                    this.selectPageTab(result.pageId);
                }
            });
    }

    selectPageTab(pageId: string): void {
        if (!pageId) {
            this.selectedPage.set({
                id: '',
                name: '',
            });

            return;
        }

        this.selectedPage.set({
            id: pageId,
            name: this.userDashboard().pages.find((page) => page.id === pageId).name,
        });

        //when tab change gridster should redraw because if a tab is not active gridster think that its height is 0 and do not draw it.
        setTimeout(() => {
            this.options().forEach((option) => {
                if (option.api) {
                    option.api.resize();
                }
            });
            window.dispatchEvent(new Event('resize'));
        }, 100);
    }

    renamePage(pageName: string): void {
        if (!pageName || pageName === '') {
            this.notify.warn(this.l('PageNameCanNotBeEmpty'));
            return;
        }

        pageName = pageName.trim();

        this.busy.set(true);

        let pageId = this.selectedPage().id;
        this._dashboardCustomizationServiceProxy
            .renamePage(
                new RenamePageInput({
                    dashboardName: this.dashboardName(),
                    id: pageId,
                    name: pageName,
                    application: DashboardCustomizationConst.Applications.Angular,
                })
            )
            .subscribe(() => {
                this.userDashboard.update((d) => ({
                    ...d,
                    pages: d.pages.map((page) => (page.id === pageId ? { ...page, name: pageName } : page)),
                }));
                this.notify.success(this.l('Renamed'));
                this.busy.set(false);
            });
    }

    deletePage(): void {
        let message =
            this.userDashboard().pages.length > 1
                ? this.l('PageDeleteWarningMessage', this.selectedPage().name)
                : this.l('BackToDefaultPageWarningMessage', this.selectedPage().name);

        this.message.confirm(message, this.l('AreYouSure'), (isConfirmed) => {
            if (isConfirmed) {
                this.busy.set(true);
                this._dashboardCustomizationServiceProxy
                    .deletePage(
                        this.selectedPage().id,
                        this.dashboardName(),
                        DashboardCustomizationConst.Applications.Angular
                    )
                    .subscribe(() => {
                        const currentPageId = this.selectedPage().id;
                        this.userDashboard.update((d) => ({
                            ...d,
                            pages: d.pages.filter((page) => page.id !== currentPageId),
                        }));
                        this.options.update((opts) => opts.slice(0, -1));
                        this.activateFirstPage();

                        this.busy.set(false);
                        this.notify.success(this.l('SuccessfullyRemoved'));

                        if (this.userDashboard().pages.length === 0) {
                            window.location.reload();
                        }
                    });
            }
        });
    }

    activateFirstPage() {
        if (this.userDashboard()?.pages[0]) {
            setTimeout(() => {
                let tab = this.dashboardTabs().tabs[0];
                tab.active = true;
            }, 0);

            this.selectPageTab(this.userDashboard().pages[0].id);
            this.initializeUserDashboardFilters();
        } else {
            this.selectPageTab(null);
        }
    }

    savePage(): void {
        this.busy.set(true);
        let savePageInput = new SavePageInput({
            dashboardName: this.dashboardName(),
            pages: this.userDashboard().pages.map(
                (page) =>
                    new Page({
                        id: page.id,
                        name: page.name,
                        widgets: page.widgets.map(
                            (widget) =>
                                new Widget({
                                    widgetId: widget.gridInformation.id,
                                    height: widget.gridInformation.rows,
                                    width: widget.gridInformation.cols,
                                    positionX: widget.gridInformation.x,
                                    positionY: widget.gridInformation.y,
                                })
                        ),
                    })
            ),
            application: DashboardCustomizationConst.Applications.Angular,
        });

        this._dashboardCustomizationServiceProxy.savePage(savePageInput).subscribe(() => {
            this.changeEditMode();
            this.initializeUserDashboardFilters();

            this.busy.set(false);
            this.notify.success(this.l('SavedSuccessfully'));
            window.location.reload();
        });
    }

    moreThanOnePage(): boolean {
        return this.userDashboard() && this.userDashboard().pages && this.userDashboard().pages.length > 1;
    }

    close(): void {
        this.modal().hide();
    }

    showAddPagePanel(target: HTMLElement): void {
        if (!this.addPagePanelVisible()) {
            this.addPageInput.set('');
        }
        this.addPagePanelVisible.set(!this.addPagePanelVisible());
    }

    showRenamePagePanel(target: HTMLElement): void {
        if (!this.renamePagePanelVisible()) {
            this.renamePageInput.set(this.selectedPage().name);
        }
        this.renamePagePanelVisible.set(!this.renamePagePanelVisible());
    }

    onMenuToggle = () => {
        this.refreshAllGrids();
    };

    onGridSterItemResize(item: any): void {
        if (this.editModeEnabled()) {
            const subjects = this.widgetSubjects();
            if (subjects[item.guid]) {
                subjects[item.guid].handler.onResize.next(null);
            }
        }
    }

    createWidgetSubjects() {
        const newSubjects: { [key: string]: { handler: WidgetOnResizeEventHandler; injector: Injector } } = {};
        for (let i = 0; i < this.userDashboard().pages.length; i++) {
            let page = this.userDashboard().pages[i];
            for (let j = 0; j < page.widgets.length; j++) {
                const widget = page.widgets[j];
                let handler = new WidgetOnResizeEventHandler();
                newSubjects[widget.guid] = {
                    handler,
                    injector: Injector.create({
                        providers: [{ provide: WIDGETONRESIZEEVENTHANDLERTOKEN, useValue: handler }],
                        parent: this._injector,
                    }),
                };
            }
        }
        this.widgetSubjects.set(newSubjects);
    }

    createWidgetSubject(guid: string) {
        let handler = new WidgetOnResizeEventHandler();
        this.widgetSubjects.update((subjects) => ({
            ...subjects,
            [guid]: {
                handler,
                injector: Injector.create({
                    providers: [{ provide: WIDGETONRESIZEEVENTHANDLERTOKEN, useValue: handler }],
                    parent: this._injector,
                }),
            },
        }));
    }

    private savePageAfterWidgetRemoval(): void {
        let savePageInput = new SavePageInput({
            dashboardName: this.dashboardName(),
            pages: this.userDashboard().pages.map(
                (page) =>
                    new Page({
                        id: page.id,
                        name: page.name,
                        widgets: page.widgets.map(
                            (widget) =>
                                new Widget({
                                    widgetId: widget.gridInformation.id,
                                    height: widget.gridInformation.rows,
                                    width: widget.gridInformation.cols,
                                    positionX: widget.gridInformation.x,
                                    positionY: widget.gridInformation.y,
                                })
                        ),
                    })
            ),
            application: DashboardCustomizationConst.Applications.Angular,
        });

        this._dashboardCustomizationServiceProxy.savePage(savePageInput).subscribe(() => {
            this.busy.set(false);
            this.notify.success(this.l('SavedSuccessfully'));
        });
    }

    private getWidgetViewDefinition(id: string): WidgetViewDefinition {
        return this._dashboardViewConfiguration.WidgetViewDefinitions.find((widget) => widget.id === id);
    }

    private getWidgetFilterViewDefinition(id: string): WidgetFilterViewDefinition {
        return this._dashboardViewConfiguration.widgetFilterDefinitions.find((filter) => filter.id === id);
    }

    //after we load page or add widget initialize needed filter too.
    private initializeUserDashboardFilters(): void {
        let allFilters: WidgetFilterOutput[] = [];
        const dashboard = this.userDashboard();

        this.dashboardDefinition.widgets
            .filter((widget) => widget.filters != null && widget.filters.length > 0)
            .forEach((widget) => {
                if (dashboard?.pages) {
                    dashboard.pages.forEach((page) => {
                        //if user has this widget in any page
                        if (page.widgets.filter((userWidget) => userWidget.id === widget.id).length !== 0) {
                            widget.filters.forEach((filter) => {
                                if (!allFilters.find((f) => f.id === filter.id)) {
                                    allFilters.push(filter);
                                }
                            });
                        }
                    });
                }
            });

        this.userDashboard.update((d) => ({
            ...d,
            filters: allFilters.map((filter) => {
                let definition = this.getWidgetFilterViewDefinition(filter.id);
                definition['name'] = filter.name;
                return definition;
            }),
        }));
    }

    //all pages use gridster and its where they get their options. Changing this will change all gristers.
    private getGridsterConfig(): GridsterConfig {
        const isRtl = rtlDetect.isRtlLang(abp.localization.currentLanguage.name);
        return {
            pushItems: true,
            compactType: 'compactUp',
            draggable: {
                enabled: this.editModeEnabled(),
            },
            resizable: {
                enabled: this.editModeEnabled(),
            },
            fixedRowHeight: 30,
            fixedColWidth: 30,
            gridType: 'verticalFixed',
            dirType: isRtl ? 'rtl' : 'ltr',
            displayGrid: 'onDrag&Resize',
            initCallback: (gridster) => {
                setTimeout(() => {
                    if (gridster.api) {
                        gridster.api.resize();
                    }
                }, 100);
            },
        };
    }
}
