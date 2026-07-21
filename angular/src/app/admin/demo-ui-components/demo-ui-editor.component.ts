import { Component, inject, signal } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { DemoUiComponentsServiceProxy } from '@shared/service-proxies/service-proxies';
import { EditorModule } from 'primeng/editor';
import { FormsModule } from '@angular/forms';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'demo-ui-editor',
    templateUrl: './demo-ui-editor.component.html',
    animations: [appModuleAnimation()],
    imports: [EditorModule, FormsModule, LocalizePipe],
})
export class DemoUiEditorComponent extends AppComponentBase {
    private demoUiComponentsService = inject(DemoUiComponentsServiceProxy);

    htmlEditorInput = signal<string>('');

    // input mask - post
    submitValue(): void {
        this.demoUiComponentsService.sendAndGetValue(this.htmlEditorInput()).subscribe((data) => {
            this.message.info(data.output, this.l('PostedValue'), { isHtml: true });
        });
    }
}
