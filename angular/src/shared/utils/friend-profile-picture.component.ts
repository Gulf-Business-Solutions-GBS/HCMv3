import { AfterViewInit, Component, inject, input, signal } from '@angular/core';
import { AppConsts } from '@shared/AppConsts';
import { ProfileServiceProxy } from '@shared/service-proxies/service-proxies';

@Component({
    selector: 'friend-profile-picture',
    template: `
        <img [src]="profilePicture()" alt="..." />
    `,
})
export class FriendProfilePictureComponent implements AfterViewInit {
    private _profileService = inject(ProfileServiceProxy);

    readonly userId = input<number>(undefined);
    readonly tenantId = input<number>(undefined);

    readonly profilePicture = signal<string>(
        AppConsts.appBaseUrl + '/assets/common/images/default-profile-picture.png'
    );

    ngAfterViewInit(): void {
        this.setProfileImage();
    }

    private setProfileImage(): void {
        const tenantId = this.tenantId();

        this._profileService.getFriendProfilePicture(this.userId(), tenantId).subscribe((result) => {
            if (result && result.profilePicture) {
                this.profilePicture.set('data:image/jpeg;base64,' + result.profilePicture);
            }
        });
    }
}
