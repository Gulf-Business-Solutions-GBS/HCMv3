import { Component, inject } from '@angular/core';
import { NgxSpinnerTextService } from './app/shared/ngx-spinner-text.service';
import { RouterOutlet } from '@angular/router';
import { NgxSpinnerComponent } from 'ngx-spinner';

@Component({
    selector: 'app-root',
    template: `
        <router-outlet />
        <ngx-spinner type="ball-clip-rotate" size="medium" color="#5ba7ea">
            @if (ngxSpinnerText) {
                <p>{{ getSpinnerText() }}</p>
            }
        </ngx-spinner>
    `,
    imports: [RouterOutlet, NgxSpinnerComponent],
})
export class RootComponent {
    ngxSpinnerText;
    constructor() {
        this.ngxSpinnerText = inject(NgxSpinnerTextService);
    }

    getSpinnerText(): string {
        return this.ngxSpinnerText.getText();
    }
}
