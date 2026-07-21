import { Component, inject, signal } from '@angular/core';
import { AppConsts } from '@shared/AppConsts';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { DemoUiComponentsServiceProxy } from '@shared/service-proxies/service-proxies';
import { FileUploadModule } from 'primeng/fileupload';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'demo-ui-file-upload',
    templateUrl: './demo-ui-file-upload.component.html',
    animations: [appModuleAnimation()],
    imports: [FileUploadModule, LocalizePipe],
})
export class DemoUiFileUploadComponent extends AppComponentBase {
    private demoUiComponentsService = inject(DemoUiComponentsServiceProxy);

    uploadUrl = signal<string>('');
    uploadedFiles = signal<any[]>([]);

    constructor() {
        super();
        this.uploadUrl.set(AppConsts.remoteServiceBaseUrl + '/DemoUiComponents/UploadFiles');
    }

    // upload completed event
    onUpload(event): void {
        const files = [...this.uploadedFiles()];
        for (const file of event.files) {
            files.push(file);
        }
        this.uploadedFiles.set(files);
    }

    onBeforeSend(event): void {
        event.xhr.setRequestHeader('Authorization', 'Bearer ' + abp.auth.getToken());
    }
}
