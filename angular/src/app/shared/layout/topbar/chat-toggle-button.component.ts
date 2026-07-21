import { Component, OnInit, inject, input, signal } from '@angular/core';
import { ThemesLayoutBaseComponent } from '../themes/themes-layout-base.component';
import { AbpSessionService } from 'abp-ng2-module';
import { Tooltip } from 'primeng/tooltip';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { FeatureCheckerPipe } from '@shared/common/pipes/feature-checker.pipe';

@Component({
    selector: 'chat-toggle-button',
    templateUrl: './chat-toggle-button.component.html',
    imports: [Tooltip, LocalizePipe, FeatureCheckerPipe],
})
export class ChatToggleButtonComponent extends ThemesLayoutBaseComponent implements OnInit {
    private _abpSessionService = inject(AbpSessionService);

    readonly customStyle = input(
        'btn btn-active-color-primary btn-active-light btn-custom btn-icon btn-icon-muted h-35px h-md-40px position-relative w-35px w-md-40px'
    );
    readonly iconStyle = input('flaticon-chat-2 fs-4');

    readonly unreadChatMessageCount = signal<number>(0);
    readonly chatConnected = signal<boolean>(false);
    readonly isHost = signal<boolean>(false);

    ngOnInit(): void {
        this.registerToEvents();
        this.isHost.set(!this._abpSessionService.tenantId);
    }

    registerToEvents() {
        this.subscribeToEvent('app.chat.unreadMessageCountChanged', (messageCount) => {
            this.unreadChatMessageCount.set(messageCount);
        });

        this.subscribeToEvent('app.chat.connected', () => {
            this.chatConnected.set(true);
        });
    }
}
