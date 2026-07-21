import { Component, model } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'password-input-with-show-button',
    templateUrl: './password-input-with-show-button.component.html',
    imports: [FormsModule, LocalizePipe],
})
export class PasswordInputWithShowButtonComponent {
    data = model<string>();
    isVisible = false;

    toggleVisibility(): void {
        this.isVisible = !this.isVisible;
    }
}
