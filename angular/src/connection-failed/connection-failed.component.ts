import { Component, OnInit, ViewEncapsulation } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { AppPreBootstrap } from 'AppPreBootstrap';

@Component({
    templateUrl: './connection-failed.component.html',
    styleUrl: './connection-failed.component.css',
    encapsulation: ViewEncapsulation.None,
})
export class ConnectionFailedComponent extends AppComponentBase {
    checkServer(): void {
        AppPreBootstrap.getServerStatus((status) => {
            if (status) {
                location.href = '/';
            } else {
                this.message.error('Server is not up yet! Please try again later.');
            }
        });
    }
}
