import { Component, inject, signal } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { DemoUiComponentsServiceProxy } from '@shared/service-proxies/service-proxies';
import { FormsModule } from '@angular/forms';
import { InputMaskModule } from 'primeng/inputmask';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'demo-ui-input-mask',
    templateUrl: './demo-ui-input-mask.component.html',
    animations: [appModuleAnimation()],
    imports: [FormsModule, InputMaskModule, LocalizePipe],
})
export class DemoUiInputMaskComponent extends AppComponentBase {
    private demoUiComponentsService = inject(DemoUiComponentsServiceProxy);

    dateValue = signal('');
    phoneValue = signal('');
    serialValue = signal('');
    phoneExtValue = signal('');

    submitDateValue(): void {
        this.submitValue(this.dateValue());
    }

    submiPhoneValue(): void {
        this.submitValue(this.phoneValue());
    }

    submitSerialValue(): void {
        this.submitValue(this.serialValue());
    }

    submitPhoneExtValue(): void {
        this.submitValue(this.phoneExtValue());
    }

    // input mask - post
    submitValue(value: any): void {
        this.demoUiComponentsService.sendAndGetValue(value).subscribe((data) => {
            this.message.info(data.output, this.l('PostedValue'));
        });
    }
}
