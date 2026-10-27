import { Component, inject, output, viewChild, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { AppConsts } from '@shared/AppConsts';
import { AppComponentBase } from '@shared/common/app-component-base';
import { ProfileServiceProxy, UpdateProfilePictureInput } from '@shared/service-proxies/service-proxies';
import { FileUpload, FileUploadModule } from 'primeng/fileupload';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { ImageCroppedEvent, ImageCropperComponent } from 'ngx-image-cropper';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';

import { FormsModule } from '@angular/forms';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'changeProfilePictureModal',
    templateUrl: './change-profile-picture-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ImageCropperComponent, FileUploadModule, LocalizePipe],
})
export class ChangeProfilePictureModalComponent extends AppComponentBase {
    private _profileService = inject(ProfileServiceProxy);
    private _httpClient = inject(HttpClient);

    readonly modal = viewChild<ModalDirective>('changeProfilePictureModal');
    readonly profilePictureUpload = viewChild<FileUpload>('profilePictureUpload');

    readonly modalSave = output<number>();

    readonly active = signal(false);
    readonly temporaryPictureUrl = signal<string>('');
    readonly saving = signal(false);
    readonly useGravatarProfilePicture = signal(false);
    readonly uploadUrl: string;
    readonly croppedImageBlob = signal<Blob | null>(null);

    readonly imageChangedEvent = signal<any>('');
    readonly userId = signal<number | null>(null);
    readonly maxProfilePictureSizeInMB = signal<number>(0);
    readonly maxProfilePictureWidth = signal<number>(0);
    readonly maxProfilePictureHeight = signal<number>(0);

    constructor() {
        super();
        this.uploadUrl = AppConsts.remoteServiceBaseUrl + '/Profile/UploadProfilePictureFile';
    }

    initializeModal(): void {
        this.active.set(true);
        this.temporaryPictureUrl.set('');
        const useGravatar = this.setting.getBoolean('App.UserManagement.UseGravatarProfilePicture');
        this.useGravatarProfilePicture.set(this.canUseGravatar() ? useGravatar : false);

        this.maxProfilePictureSizeInMB.set(
            parseFloat(this.setting.get('App.UserManagement.MaxProfilePictureSizeInMB'))
        );
        this.maxProfilePictureWidth.set(this.setting.getInt('App.UserManagement.MaxProfilePictureWidth'));
        this.maxProfilePictureHeight.set(this.setting.getInt('App.UserManagement.MaxProfilePictureHeight'));
    }

    show(userId?: number): void {
        this.initializeModal();
        this.modal().show();
        this.userId.set(userId ?? null);
    }

    close(): void {
        this.active.set(false);
        this.imageChangedEvent.set('');
        this.croppedImageBlob.set(null);
        if (this.profilePictureUpload()) {
            this.profilePictureUpload().clear();
        }
        this.modal().hide();
    }

    fileChangeEvent(event: any): void {
        if (event.target.files[0].size > this.maxProfilePictureSizeInByte()) {
            this.message.warn(this.l('ProfilePicture_Warn_SizeLimit', this.maxProfilePictureSizeInMB()));
            return;
        }

        this.imageChangedEvent.set(event);
    }

    imageCroppedFile(event: ImageCroppedEvent) {
        if (!event.blob) {
            return;
        }

        this.croppedImageBlob.set(event.blob);
    }

    uploadProfilePicture(event: { files: File[] }): void {
        const formData: FormData = new FormData();
        const token = this.guid();

        const croppedBlob = this.croppedImageBlob();
        const fileToUpload = croppedBlob
            ? new File([croppedBlob], 'ProfilePicture.png', { type: 'image/png' })
            : event.files[0];

        formData.append('file', fileToUpload, fileToUpload.name);
        formData.append('FileType', fileToUpload.type);
        formData.append('FileName', 'ProfilePicture');
        formData.append('FileToken', token);

        this.saving.set(true);
        this._httpClient
            .post<any>(this.uploadUrl, formData)
            .pipe(
                finalize(() => {
                    this.saving.set(false);
                    if (this.profilePictureUpload()) {
                        this.profilePictureUpload().clear();
                    }
                })
            )
            .subscribe({
                next: (response) => {
                    if (response.success) {
                        this.updateProfilePicture(token);
                    } else {
                        this.message.error(response?.error?.message);
                    }
                },
                error: (error) => {
                    this.message.error(error?.error?.message);
                },
            });
    }

    updateProfilePicture(fileToken: string): void {
        const input = new UpdateProfilePictureInput();
        input.fileToken = fileToken;

        const userId = this.userId();
        if (userId) {
            input.userId = userId;
        }

        this.saving.set(true);
        this._profileService
            .updateProfilePicture(input)
            .pipe(
                finalize(() => {
                    this.saving.set(false);
                })
            )
            .subscribe(() => {
                abp.setting.values['App.UserManagement.UseGravatarProfilePicture'] =
                    this.useGravatarProfilePicture().toString();
                abp.event.trigger('profilePictureChanged');
                const userId = this.userId();
                this.modalSave.emit(userId);
                this.close();
            });
    }

    updateProfilePictureToUseGravatar(): void {
        const input = new UpdateProfilePictureInput();
        input.useGravatarProfilePicture = this.useGravatarProfilePicture();

        this.saving.set(true);
        this._profileService
            .updateProfilePicture(input)
            .pipe(
                finalize(() => {
                    this.saving.set(false);
                })
            )
            .subscribe(() => {
                abp.setting.values['App.UserManagement.UseGravatarProfilePicture'] =
                    this.useGravatarProfilePicture().toString();
                abp.event.trigger('profilePictureChanged');
                const userId = this.userId();
                this.modalSave.emit(userId);
                this.close();
            });
    }

    guid(): string {
        function s4() {
            return Math.floor((1 + Math.random()) * 0x10000)
                .toString(16)
                .substring(1);
        }

        return s4() + s4() + '-' + s4() + '-' + s4() + '-' + s4() + '-' + s4() + s4() + s4();
    }

    save(): void {
        if (this.useGravatarProfilePicture()) {
            this.updateProfilePictureToUseGravatar();
        } else {
            const croppedBlob = this.croppedImageBlob();
            if (croppedBlob) {
                this.uploadProfilePicture({ files: [new File([croppedBlob], 'ProfilePicture.png')] });
            }
        }
    }

    onBeforeSend(event): void {
        event.xhr.setRequestHeader('Authorization', 'Bearer ' + abp.auth.getToken());
    }

    canUseGravatar(): boolean {
        return this.setting.getBoolean('App.UserManagement.AllowUsingGravatarProfilePicture');
    }

    maxProfilePictureSizeInByte(): number {
        return this.maxProfilePictureSizeInMB() * 1024 * 1024;
    }
}
