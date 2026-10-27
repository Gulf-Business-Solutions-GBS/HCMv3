import {
    AfterViewInit,
    Component,
    OnInit,
    ViewEncapsulation,
    ElementRef,
    inject,
    input,
    output,
    viewChild,
    signal,
} from '@angular/core';
import { AppConsts } from '@shared/AppConsts';
import { AppComponentBase } from '@shared/common/app-component-base';
import { DomHelper } from '@shared/helpers/DomHelper';
import { HttpClient } from '@angular/common/http';
import { FileUpload, FileUploadModule } from 'primeng/fileupload';
import { finalize } from 'rxjs/operators';
import {
    BlockUserInput,
    ChatSide,
    ChatServiceProxy,
    FriendDto,
    FriendshipState,
    FriendshipServiceProxy,
    MarkAllUnreadMessagesOfUserAsReadInput,
    ProfileServiceProxy,
    UnblockUserInput,
    UserLoginInfoDto,
    ChatMessageReadState,
    ChatMessageDto,
} from '@shared/service-proxies/service-proxies';
import { LocalStorageService } from '@shared/utils/local-storage.service';
import { filter as _filter, map as _map, forEach as _forEach, min as _min, reduce as _reduce } from 'lodash-es';
import { DateTime } from 'luxon';
import { ChatFriendDto } from './ChatFriendDto';
import { ChatSignalrService } from './chat-signalr.service';
import { DateTimeService } from '@app/shared/common/timing/date-time.service';
import { DrawerComponent } from '@metronic/app/kt/components';
import { AddFriendModalComponent } from './add-friend-modal.component';
import { FormsModule } from '@angular/forms';
import { Tooltip } from 'primeng/tooltip';
import { NgClass } from '@angular/common';
import { ChatFriendListItemComponent } from './chat-friend-list-item.component';
import { ChatMessageComponent } from './chat-message.component';
import { FriendProfilePictureComponent } from '../../../../shared/utils/friend-profile-picture.component';
import { LuxonFromNowPipe } from '../../../../shared/utils/luxon-from-now.pipe';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { ButtonModule } from 'primeng/button';

@Component({
    templateUrl: './chat-bar.component.html',
    selector: 'chat-bar',
    styleUrls: ['./chat-bar.component.less'],
    encapsulation: ViewEncapsulation.None,
    imports: [
        FormsModule,
        Tooltip,
        NgClass,
        ChatFriendListItemComponent,
        ChatMessageComponent,
        FriendProfilePictureComponent,
        FileUploadModule,
        LuxonFromNowPipe,
        LocalizePipe,
        ButtonModule,
    ],
})
export class ChatBarComponent extends AppComponentBase implements OnInit, AfterViewInit {
    private _friendshipService = inject(FriendshipServiceProxy);
    private _chatService = inject(ChatServiceProxy);
    private _localStorageService = inject(LocalStorageService);
    private _chatSignalrService = inject(ChatSignalrService);
    private _profileService = inject(ProfileServiceProxy);
    private _httpClient = inject(HttpClient);
    private _dateTimeService = inject(DateTimeService);

    readonly onProgress = output<any>();
    readonly addFriendModal = input<AddFriendModalComponent>(undefined);
    readonly chatMessageInput = viewChild<ElementRef>('ChatMessage');
    readonly chatScrollBar = viewChild('chatScrollBar');
    readonly chatImageUpload = viewChild<FileUpload>('chatImageUpload');
    readonly chatFileUpload = viewChild<FileUpload>('chatFileUpload');

    readonly progress = signal<number>(0);
    maxMessageCount = 10;
    styleOverflow: any = 'hidden';
    friendDtoState: typeof FriendshipState = FriendshipState;
    uploadUrl: string;
    readonly isFileSelected = signal<boolean>(false);
    readonly friends = signal<ChatFriendDto[]>([]);
    currentUser: UserLoginInfoDto = this.appSession.user;
    readonly profilePicture = signal<string>(
        AppConsts.appBaseUrl + '/assets/common/images/default-profile-picture.png'
    );
    readonly chatMessage = signal<string>('');
    chatDrawer: DrawerComponent;
    readonly sendingMessage = signal<boolean>(false);
    readonly userNameFilter = signal<string>('');
    serverClientTimeDifference = 0;
    isMultiTenancyEnabled: boolean = this.multiTenancy.isEnabled;
    appChatSide: typeof ChatSide = ChatSide;
    appChatMessageReadState: typeof ChatMessageReadState = ChatMessageReadState;

    _isOpen: boolean;
    _pinned = false;
    _selectedUser: ChatFriendDto = new ChatFriendDto();

    constructor() {
        super();
        this.uploadUrl = AppConsts.remoteServiceBaseUrl + '/Chat/UploadFile';
    }

    get chatUserSearchHint(): string {
        return this.l('ChatUserSearch_Hint');
    }

    get selectedUser(): ChatFriendDto {
        return this._selectedUser;
    }

    get isOpen(): boolean {
        return this._isOpen;
    }

    get pinned(): boolean {
        return this._pinned;
    }

    set isOpen(newValue: boolean) {
        if (newValue === this._isOpen) {
            return;
        }

        this._localStorageService.setItem('app.chat.isOpen', newValue);
        this._isOpen = newValue;

        if (newValue) {
            this.markAllUnreadMessagesOfUserAsRead(this.selectedUser);
        }
    }

    set pinned(newValue: boolean) {
        if (newValue === this._pinned) {
            return;
        }

        this._pinned = newValue;
        this._localStorageService.setItem('app.chat.pinned', newValue);
    }

    set selectedUser(newValue: ChatFriendDto) {
        if (newValue === this._selectedUser) {
            return;
        }

        this._selectedUser = newValue;

        //NOTE: this is a fix for localForage is not able to store user with messages array filled
        if (newValue.messages) {
            newValue.messages = [];
            newValue.messagesLoaded = false;
        }
        this._localStorageService.setItem('app.chat.selectedUser', newValue);
    }

    onScrollChatPanel(e) {
        if (e.target.scrollTop === 0 && !this.selectedUser.allPreviousMessagesLoaded) {
            this.loadMessages(this.selectedUser, () => {
                this.setScrollPosition(1);
            });
        }
    }

    ngOnInit(): void {
        this.registerEvents();
        this.getProfilePicture();
    }

    shareCurrentLink() {
        this.chatMessage.set('[link]{"message":"' + window.location.href + '"}');
        this.sendMessage();
    }

    uploadImage(data: { files: File }): void {
        const formData: FormData = new FormData();
        const file = data.files[0];
        formData.append('file', file, file.name);

        this._httpClient
            .post<any>(this.uploadUrl, formData)
            .pipe(finalize(() => this.chatImageUpload().clear()))
            .subscribe({
                next: (response) => {
                    this.chatMessage.set(
                        '[image]{"id":"' +
                            response.result.id +
                            '", "name":"' +
                            response.result.name +
                            '", "contentType":"' +
                            response.result.contentType +
                            '"}'
                    );
                    this.sendMessage();

                    this.isFileSelected.set(false);
                    this.progress.set(0);
                },
                error: (error) => {
                    abp.message.error(error?.error?.message);
                },
                complete: () => {
                    this.scrollToBottom();
                },
            });
    }

    uploadFile(data: { files: File }): void {
        const formData: FormData = new FormData();
        const file = data.files[0];
        formData.append('file', file, file.name);
        formData.append('isImage', 'false');

        this._httpClient
            .post<any>(this.uploadUrl, formData)
            .pipe(finalize(() => this.chatFileUpload().clear()))
            .subscribe({
                next: (response) => {
                    this.chatMessage.set(
                        '[file]{"id":"' +
                            response.result.id +
                            '", "name":"' +
                            response.result.name +
                            '", "contentType":"' +
                            response.result.contentType +
                            '"}'
                    );
                    this.sendMessage();

                    this.isFileSelected.set(false);
                    this.progress.set(0);
                },
                error: (error) => {
                    abp.message.error(error?.error?.message);
                },
                complete: () => {
                    this.scrollToBottom();
                },
            });
    }

    onBeforeSend(event): void {
        this.isFileSelected.set(true);
        event.xhr.upload.addEventListener(
            'progress',
            (e: ProgressEvent) => {
                if (e.lengthComputable) {
                    const progressValue = Math.round((e.loaded * 100) / e.total);
                    this.progress.set(progressValue);
                }

                this.onProgress.emit({ originalEvent: e, progress: this.progress() });
            },
            false
        );
    }

    getShownUserName(tenanycName: string, userName: string): string {
        if (!this.isMultiTenancyEnabled) {
            return userName;
        }

        return (tenanycName ? tenanycName : '.') + '\\' + userName;
    }

    getProfilePicture(): void {
        this._profileService.getProfilePicture().subscribe((result) => {
            if (result && result.profilePicture) {
                this.profilePicture.set('data:image/jpeg;base64,' + result.profilePicture);
            }
        });
    }

    block(user: FriendDto): void {
        const blockUserInput = new BlockUserInput();
        blockUserInput.tenantId = user.friendTenantId;
        blockUserInput.userId = user.friendUserId;

        this._friendshipService.blockUser(blockUserInput).subscribe(() => {
            this.notify.info(this.l('UserBlocked'));
        });
    }

    unblock(user: FriendDto): void {
        const unblockUserInput = new UnblockUserInput();
        unblockUserInput.tenantId = user.friendTenantId;
        unblockUserInput.userId = user.friendUserId;

        this._friendshipService.unblockUser(unblockUserInput).subscribe(() => {
            this.notify.info(this.l('UserUnblocked'));
        });
    }

    removeFriendUI(user: FriendDto): void {
        const currentFriends = this.friends();
        this.friends.set(
            currentFriends.filter((item) => {
                if (item.friendUserId === user.friendUserId && item.friendTenantId === user.friendTenantId) {
                    return false;
                }
                return true;
            })
        );

        this.quickSideBarBackClick();
    }

    remove(user: FriendDto): void {
        this.message.confirm(this.l('AreYouSureToRemoveFromFriends'), null, (isConfirmed) => {
            if (isConfirmed) {
                this._friendshipService
                    .removeFriend(user.friendUserId, user.friendTenantId ? user.friendTenantId : undefined)
                    .subscribe(() => {
                        this.removeFriendUI(user);
                        this.notify.info(this.l('FriendRemoved'));
                    });
            }
        });
    }

    markAllUnreadMessagesOfUserAsRead(user: ChatFriendDto): void {
        if (!user || !this.isOpen) {
            return;
        }

        const unreadMessages = _filter(user.messages, (m) => m.readState === ChatMessageReadState.Unread);
        const unreadMessageIds = _map(unreadMessages, 'id');

        if (!unreadMessageIds.length) {
            return;
        }

        const input = new MarkAllUnreadMessagesOfUserAsReadInput();
        input.tenantId = user.friendTenantId;
        input.userId = user.friendUserId;

        this._chatService.markAllUnreadMessagesOfUserAsRead(input).subscribe(() => {
            _forEach(user.messages, (message) => {
                if (unreadMessageIds.indexOf(message.id) >= 0) {
                    message.readState = ChatMessageReadState.Read;
                }
            });
        });
    }

    loadMessages(user: ChatFriendDto, callback: any): void {
        let minMessageId;
        if (user.messages && user.messages.length) {
            minMessageId = _min(_map(user.messages, (m) => m.id));
        }

        this._chatService
            .getUserChatMessages(
                user.friendTenantId ? user.friendTenantId : undefined,
                user.friendUserId,
                minMessageId,
                this.maxMessageCount
            )
            .subscribe((result) => {
                if (!user.messages) {
                    user.messages = [];
                }

                user.messages = result.items.concat(user.messages);

                this.markAllUnreadMessagesOfUserAsRead(user);

                if (!result.items.length) {
                    user.allPreviousMessagesLoaded = true;
                }

                if (callback) {
                    callback();
                }
            });
    }

    openSearchModal(): void {
        this.addFriendModal().show();
    }

    getFriendOrNull(userId: number, tenantId?: number): ChatFriendDto {
        const friends = _filter(
            this.friends(),
            (friend) => friend.friendUserId === userId && friend.friendTenantId === tenantId
        );
        if (friends.length) {
            return friends[0];
        }

        return null;
    }

    getFilteredFriends(state: FriendshipState, userNameFilter: string): FriendDto[] {
        const foundFriends = _filter(
            this.friends(),
            (friend) =>
                friend.state === state &&
                this.getShownUserName(friend.friendTenancyName, friend.friendUserName)
                    .toLocaleLowerCase()
                    .indexOf(userNameFilter.toLocaleLowerCase()) >= 0
        );

        return foundFriends;
    }

    getFilteredFriendsCount(state: FriendshipState): number {
        return _filter(this.friends(), (friend) => friend.state === state).length;
    }

    getUserNameByChatSide(chatSide: ChatSide): string {
        return chatSide === ChatSide.Sender ? this.currentUser.userName : this.selectedUser.friendUserName;
    }

    getFixedMessageTime(messageTime: DateTime): DateTime {
        if (typeof messageTime === 'string') {
            messageTime = this._dateTimeService.fromISODateString(messageTime);
        }

        return this._dateTimeService.plusSeconds(messageTime, -1 * this.serverClientTimeDifference).toDateTime();
    }

    getFriendsAndSettings(callback: any): void {
        this._chatService.getUserChatFriendsWithSettings().subscribe((result) => {
            this.friends.set(result.friends as ChatFriendDto[]);
            this.serverClientTimeDifference = this._dateTimeService.getDiffInSeconds(
                this._dateTimeService.getDate().toDateTime(),
                result.serverTime
            );

            this.triggerUnreadMessageCountChangeEvent();
            callback();
        });
    }

    scrollToBottom(): void {
        setTimeout(() => {
            this.setScrollPosition('bottom');
        }, 500);
    }

    loadLastState(): void {
        const self = this;
        self._localStorageService.getItem('app.chat.isOpen', (err, isOpen) => {
            self.isOpen = isOpen;

            self._localStorageService.getItem('app.chat.pinned', (err, pinned) => {
                self.pinned = pinned;
            });

            if (isOpen) {
                this.chatDrawer.show();
                self._localStorageService.getItem('app.chat.selectedUser', (err, selectedUser) => {
                    if (selectedUser && selectedUser.friendUserId) {
                        self.selectFriend(selectedUser);
                    }
                });
            }
        });
    }

    selectFriend(friend: FriendDto): void {
        const chatUser = this.getFriendOrNull(friend.friendUserId, friend.friendTenantId);
        this.selectedUser = chatUser;

        if (!chatUser) {
            return;
        }

        this.chatMessage.set('');

        if (!chatUser.messagesLoaded) {
            this.loadMessages(chatUser, () => {
                chatUser.messagesLoaded = true;
                this.scrollToBottom();
            });
        } else {
            this.markAllUnreadMessagesOfUserAsRead(this.selectedUser);
        }

        window.dispatchEvent(new Event('resize'));
    }

    sendMessage(event?: any): void {
        if (event) {
            event.preventDefault();
            event.stopPropagation();
        }

        if (!this.chatMessage()) {
            return;
        }

        this.sendingMessage.set(true);
        const tenancyName = this.appSession.tenant ? this.appSession.tenant.tenancyName : null;
        this._chatSignalrService.sendMessage(
            {
                tenantId: this.selectedUser.friendTenantId,
                userId: this.selectedUser.friendUserId,
                message: this.chatMessage(),
                tenancyName: tenancyName,
                userName: this.appSession.user.userName,
                profilePictureId: this.appSession.user.profilePictureId,
            },
            () => {
                this.chatMessage.set('');
                this.sendingMessage.set(false);
            }
        );
    }

    reversePinned(): void {
        this.pinned = !this.pinned;
    }

    quickSideBarBackClick(): void {
        this.selectedUser = new ChatFriendDto();
    }

    ngAfterViewInit(): void {
        this.chatDrawer = new DrawerComponent(document.getElementById('kt_drawer_chat'), {
            overlay: true,
            baseClass: 'drawer',
            overlayClass: 'drawer-overlay',
            direction: 'end',
        });

        setTimeout(() => {
            this.chatDrawer.on('kt.drawer.after.hidden', (e: Event) => {
                this.isOpen = false;
            });
        }, 100);

        this.chatDrawer.on('kt.drawer.shown', (e: Event) => {
            this.isOpen = true;
        });
    }

    triggerUnreadMessageCountChangeEvent(): void {
        let totalUnreadMessageCount = 0;

        const currentFriends = this.friends();
        if (currentFriends) {
            totalUnreadMessageCount = _reduce(currentFriends, (memo, friend) => memo + friend.unreadMessageCount, 0);
        }

        abp.event.trigger('app.chat.unreadMessageCountChanged', totalUnreadMessageCount);
    }

    registerEvents(): void {
        const self = this;

        function onMessageReceived(message) {
            const user = self.getFriendOrNull(message.targetUserId, message.targetTenantId);
            if (!user) {
                return;
            }

            user.messages = user.messages || [];
            user.messages.push(message);

            if (message.side === ChatSide.Receiver) {
                user.unreadMessageCount += 1;
                message.readState = ChatMessageReadState.Unread;
                self.triggerUnreadMessageCountChangeEvent();

                if (
                    self.isOpen &&
                    self.selectedUser !== null &&
                    user.friendTenantId === self.selectedUser.friendTenantId &&
                    user.friendUserId === self.selectedUser.friendUserId
                ) {
                    self.markAllUnreadMessagesOfUserAsRead(user);
                } else {
                    self.notify.info(
                        abp.utils.formatString(
                            '{0}: {1}',
                            user.friendUserName,
                            abp.utils.truncateString(message.message, 100)
                        ),
                        null,
                        {
                            onclick() {
                                if (document.body.className.indexOf('offcanvas-on') < 0) {
                                    self.showChatPanel();
                                    self.isOpen = true;
                                }

                                self.selectFriend(user);
                                self.pinned = true;
                            },
                        }
                    );
                }
            }

            self.scrollToBottom();
        }

        this.subscribeToEvent('app.chat.messageReceived', (message) => {
            onMessageReceived(message);
        });

        function onFriendshipRequestReceived(data, isOwnRequest) {
            if (!isOwnRequest) {
                abp.notify.info(self.l('UserSendYouAFriendshipRequest', data.friendUserName));
            }

            const currentFriends = self.friends();
            if (
                !_filter(
                    currentFriends,
                    (f) => f.friendUserId === data.friendUserId && f.friendTenantId === data.friendTenantId
                ).length
            ) {
                self.friends.set([...currentFriends, data]);
            }
        }

        this.subscribeToEvent('app.chat.friendshipRequestReceived', (data, isOwnRequest) => {
            onFriendshipRequestReceived(data, isOwnRequest);
        });

        function onUserConnectionStateChanged(data) {
            const user = self.getFriendOrNull(data.friend.userId, data.friend.tenantId);
            if (!user) {
                return;
            }

            user.isOnline = data.isConnected;
        }

        this.subscribeToEvent('app.chat.userConnectionStateChanged', (data) => {
            onUserConnectionStateChanged(data);
        });

        function onUserStateChanged(data) {
            const user = self.getFriendOrNull(data.friend.userId, data.friend.tenantId);
            if (!user) {
                return;
            }

            user.state = data.state;
        }

        this.subscribeToEvent('app.chat.userStateChanged', (data) => {
            onUserStateChanged(data);
        });

        function onAllUnreadMessagesOfUserRead(data) {
            const user = self.getFriendOrNull(data.friend.userId, data.friend.tenantId);
            if (!user) {
                return;
            }

            user.unreadMessageCount = 0;
            self.triggerUnreadMessageCountChangeEvent();
        }

        this.subscribeToEvent('app.chat.allUnreadMessagesOfUserRead', (data) => {
            onAllUnreadMessagesOfUserRead(data);
        });

        function onReadStateChange(data) {
            const user = self.getFriendOrNull(data.friend.userId, data.friend.tenantId);
            if (!user) {
                return;
            }

            _forEach(user.messages, (message) => {
                message.receiverReadState = ChatMessageReadState.Read;
            });
        }

        this.subscribeToEvent('app.chat.readStateChange', (data) => {
            onReadStateChange(data);
        });

        function onConnected() {
            self.getFriendsAndSettings(() => {
                DomHelper.waitUntilElementIsReady('#kt_drawer_chat', () => {
                    self.loadLastState();
                });
            });
        }

        this.subscribeToEvent('app.chat.connected', () => {
            onConnected();
        });

        this.subscribeToEvent('app.chat.userDeleted', (data) => {
            let chatFriendDto = this.getFriendOrNull(data.userId, data.tenantId);
            this.removeFriendUI(chatFriendDto);
        });
    }

    getReadStateHtml(message: ChatMessageDto): string {
        let readStateClass = message.receiverReadState === ChatMessageReadState.Read ? ' text-primary' : ' text-muted';
        return message.side === ChatSide.Sender
            ? '<i class="read-state-check fa fa-check' + readStateClass + '" aria-hidden="true"></i>'
            : '';
    }

    showChatPanel(): void {
        document.body.setAttribute('data-kt-drawer-chat', 'on');
        document.body.setAttribute('data-kt-drawer', 'on');
        document.getElementById('kt_drawer_chat').className += ' drawer-on';
    }

    private setScrollPosition(position: 'top' | 'bottom' | number): void {
        const messagePanel = document.querySelector('#kt_drawer_chat_messenger_body');
        if (!messagePanel) {
            return;
        }

        const images = messagePanel.querySelectorAll('img');

        if (images.length > 0) {
            const loadImagePromises = Array.from(images).map((img) => {
                return new Promise<void>((resolve) => {
                    if (img.complete) {
                        resolve();
                    } else {
                        img.onload = () => resolve();
                        img.onerror = () => resolve();
                    }
                });
            });

            Promise.all(loadImagePromises).then(() => {
                this.scrollToPosition(position, messagePanel);
            });
        } else {
            this.scrollToPosition(position, messagePanel);
        }
    }

    private scrollToPosition(position: 'top' | 'bottom' | number, messagePanel: Element): void {
        if (position === 'bottom') {
            messagePanel.scrollTop = messagePanel.scrollHeight + 20;
        } else if (position === 'top') {
            messagePanel.scrollTop = 0;
        } else {
            messagePanel.scrollTop = position;
        }
    }
}
