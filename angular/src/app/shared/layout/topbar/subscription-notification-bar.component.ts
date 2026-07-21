import { Component, input } from '@angular/core';
import { ThemesLayoutBaseComponent } from '../themes/themes-layout-base.component';
import { NgClass } from '@angular/common';
import { RouterLink } from '@angular/router';

@Component({
    selector: 'subscription-notification-bar',
    templateUrl: './subscription-notification-bar.component.html',
    imports: [NgClass, RouterLink],
})
export class SubscriptionNotificationBarComponent extends ThemesLayoutBaseComponent {
    readonly customStyle = input(
        'btn btn-icon btn-custom btn-icon-muted btn-active-light btn-active-color-primary w-35px h-35px w-md-40px h-md-40px position-relative'
    );
}
