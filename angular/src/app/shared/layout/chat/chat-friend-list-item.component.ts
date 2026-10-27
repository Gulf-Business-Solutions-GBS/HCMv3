import { AbpMultiTenancyService } from 'abp-ng2-module';
import { Component, inject, input, output } from '@angular/core';
import { AppConsts } from '@shared/AppConsts';
import { ChatFriendDto } from './ChatFriendDto';
import { FriendProfilePictureComponent } from '../../../../shared/utils/friend-profile-picture.component';

@Component({
    selector: 'chat-friend-list-item',
    templateUrl: './chat-friend-list-item.component.html',
    imports: [FriendProfilePictureComponent],
})
export class ChatFriendListItemComponent {
    readonly friend = input<ChatFriendDto>(undefined);
    readonly selectChatFriend = output<string>();

    remoteServiceUrl: string = AppConsts.remoteServiceBaseUrl;
    appPath: string = AppConsts.appBaseUrl;

    multiTenancy = inject(AbpMultiTenancyService);

    getShownUserName(tenantName: string, userName: string): string {
        if (!this.multiTenancy.isEnabled) {
            return userName;
        }
        return (tenantName ? tenantName : '.') + '\\' + userName;
    }

    getRemoteImageUrl(profilePictureId: string, userId: number, tenantId?: number): string {
        return (
            this.remoteServiceUrl +
            '/Profile/GetFriendProfilePictureById?id=' +
            profilePictureId +
            '&userId=' +
            userId +
            '&tenantId=' +
            tenantId
        );
    }
}
