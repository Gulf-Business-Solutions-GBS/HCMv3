import {
    ChangeDetectionStrategy,
    Component,
    ElementRef,
    OnDestroy,
    OnInit,
    computed,
    inject,
    signal,
    viewChild,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import {
    AiAgentServiceProxy,
    AiAgentStartupDto,
    AiConversationListDto,
    AiMessageDto,
    AiMessageRole,
    AiPendingActionDto,
    AiPendingActionStatus,
    EntityDto,
    SendAiMessageInput,
} from '@shared/service-proxies/service-proxies';
import { finalize } from 'rxjs/operators';
import { SubHeaderComponent } from '../../../shared/common/sub-header/sub-header.component';

/** A message on screen. `failed` marks a question the assistant never answered, so it can be asked again. */
interface ChatLine {
    message: AiMessageDto;
    failed?: boolean;
}

/**
 * The AI assistant page: a conversation list on one side, the chat on the other.
 *
 * <p>Nothing the assistant says is rendered as HTML. Replies are plain text shown with {@code white-space:
 * pre-wrap}, so a model that echoes markup -- or text planted in an employee record -- cannot inject anything
 * into the page.</p>
 *
 * <p>Actions arrive as cards under a reply. The assistant cannot submit anything; the Confirm button on the card
 * is the only thing that does, and it calls the server with the card's id, never with anything the model said.</p>
 */
@Component({
    templateUrl: './ai-assistant.component.html',
    styleUrls: ['./ai-assistant.component.less'],
    animations: [appModuleAnimation()],
    imports: [CommonModule, FormsModule, SubHeaderComponent, LocalizePipe, LuxonFormatPipe],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AiAssistantComponent extends AppComponentBase implements OnInit, OnDestroy {
    private readonly _aiAgent = inject(AiAgentServiceProxy);

    readonly Role = AiMessageRole;
    readonly Status = AiPendingActionStatus;

    readonly startup = signal<AiAgentStartupDto | undefined>(undefined);
    readonly conversations = signal<AiConversationListDto[]>([]);
    readonly activeConversationId = signal<number | undefined>(undefined);
    readonly lines = signal<ChatLine[]>([]);

    readonly loadingConversation = signal(false);
    readonly sending = signal(false);
    /** Seconds the current question has been waiting. A CPU-hosted model can take a minute; a counter shows it is working. */
    readonly waitingSeconds = signal(0);
    /** Card ids with a Confirm or Cancel in flight, so a second click does nothing. */
    readonly actionsInFlight = signal<ReadonlySet<number>>(new Set());
    /** On a narrow screen the conversation list is a panel that opens over the chat. */
    readonly showConversationsOnMobile = signal(false);

    readonly isEmpty = computed(() => this.lines().length === 0 && !this.loadingConversation());

    readonly suggestions = computed(() => {
        const info = this.startup();
        const keys: string[] = [];

        if (info?.isLinkedToEmployee && info.canUseLeave) {
            keys.push('AiAgentSuggestionBalance', 'AiAgentSuggestionApplyLeave', 'AiAgentSuggestionRequests');
        }

        if (info?.canManagePay) {
            keys.push('AiAgentSuggestionCarAllowance', 'AiAgentSuggestionBonus');
        }

        return keys.map((key) => this.l(key));
    });

    draft = '';

    private readonly messagesPane = viewChild<ElementRef<HTMLElement>>('messagesPane');
    private readonly composer = viewChild<ElementRef<HTMLTextAreaElement>>('composer');
    private waitTimer: ReturnType<typeof setInterval> | undefined;

    ngOnInit(): void {
        this._aiAgent.getStartupInfo().subscribe((info) => this.startup.set(info));
        this.loadConversations();
    }

    override ngOnDestroy(): void {
        this.stopWaitTimer();
        super.ngOnDestroy();
    }

    greeting(): string {
        const info = this.startup();
        const name = this.isRtl ? info?.firstNameAr || info?.firstNameEn : info?.firstNameEn;
        return name ? this.l('AiAgentGreeting', name) : this.l('AiAgentGreetingNoName');
    }

    // ------------------------------------------------------------------ conversations

    loadConversations(): void {
        this._aiAgent.getMyConversations().subscribe((result) => this.conversations.set(result.items ?? []));
    }

    newConversation(): void {
        if (this.sending()) {
            return;
        }

        this.activeConversationId.set(undefined);
        this.lines.set([]);
        this.draft = '';
        this.showConversationsOnMobile.set(false);
        this.focusComposer();
    }

    openConversation(conversation: AiConversationListDto): void {
        if (this.sending() || conversation.id === this.activeConversationId()) {
            this.showConversationsOnMobile.set(false);
            return;
        }

        this.activeConversationId.set(conversation.id);
        this.lines.set([]);
        this.loadingConversation.set(true);
        this.showConversationsOnMobile.set(false);

        this._aiAgent
            .getConversation(conversation.id)
            .pipe(finalize(() => this.loadingConversation.set(false)))
            .subscribe((result) => {
                // Ignore a slow response for a conversation the user has already left.
                if (this.activeConversationId() !== conversation.id) {
                    return;
                }

                this.lines.set((result.messages ?? []).map((message) => ({ message })));
                this.scrollToBottom();
            });
    }

    deleteConversation(conversation: AiConversationListDto, event: Event): void {
        event.stopPropagation();

        this.message.confirm(this.l('AiAgentDeleteConversationConfirm'), this.l('AreYouSure'), (isConfirmed) => {
            if (!isConfirmed) {
                return;
            }

            this._aiAgent.deleteConversation(conversation.id).subscribe(() => {
                this.conversations.update((list) => list.filter((c) => c.id !== conversation.id));
                if (this.activeConversationId() === conversation.id) {
                    this.newConversation();
                }
            });
        });
    }

    // ------------------------------------------------------------------ sending

    onComposerKeydown(event: KeyboardEvent): void {
        // Enter sends; Shift+Enter is a new line. Not while an IME is composing (Arabic keyboards use one).
        if (event.key === 'Enter' && !event.shiftKey && !event.isComposing) {
            event.preventDefault();
            this.send();
        }
    }

    useSuggestion(text: string): void {
        this.draft = text;
        this.send();
    }

    send(text: string = this.draft): void {
        const message = text?.trim();
        if (!message || this.sending()) {
            return;
        }

        const pending: ChatLine = {
            message: new AiMessageDto({
                id: 0,
                role: AiMessageRole.User,
                content: message,
                creationTime: undefined,
                pendingAction: undefined,
            }),
        };

        this.lines.update((lines) => [...lines, pending]);
        this.draft = '';
        this.sending.set(true);
        this.startWaitTimer();
        this.scrollToBottom();

        const input = new SendAiMessageInput();
        input.conversationId = this.activeConversationId();
        input.message = message;

        const conversationAtSend = this.activeConversationId();

        this._aiAgent
            .sendMessage(input)
            .pipe(
                finalize(() => {
                    this.sending.set(false);
                    this.stopWaitTimer();
                    this.focusComposer();
                })
            )
            .subscribe({
                next: (result) => {
                    if (this.activeConversationId() !== conversationAtSend) {
                        // The user switched away while waiting; the reply is saved and shows when they come back.
                        this.loadConversations();
                        return;
                    }

                    this.activeConversationId.set(result.conversationId);
                    this.lines.update((lines) => [
                        ...lines.filter((line) => line !== pending),
                        { message: result.userMessage },
                        { message: result.assistantMessage },
                    ]);
                    this.loadConversations();
                    this.scrollToBottom();
                },
                error: () => {
                    // The server has already shown why. Keep the question on screen, marked, so it can be retried
                    // without retyping.
                    this.lines.update((lines) => lines.map((line) => (line === pending ? { ...line, failed: true } : line)));
                    this.loadConversations();
                },
            });
    }

    retry(line: ChatLine): void {
        this.lines.update((lines) => lines.filter((l) => l !== line));
        this.send(line.message.content);
    }

    // ------------------------------------------------------------------ action cards

    isInFlight(action: AiPendingActionDto): boolean {
        return this.actionsInFlight().has(action.id);
    }

    confirmAction(action: AiPendingActionDto): void {
        this.runAction(action, (body) => this._aiAgent.confirmAction(body));
    }

    cancelAction(action: AiPendingActionDto): void {
        this.runAction(action, (body) => this._aiAgent.cancelAction(body));
    }

    statusLabel(action: AiPendingActionDto): string {
        switch (action.status) {
            case AiPendingActionStatus.Pending:
                return this.l('AiAgentStatusPending');
            case AiPendingActionStatus.Processing:
                return this.l('AiAgentStatusProcessing');
            case AiPendingActionStatus.Completed:
                return this.l('AiAgentStatusCompleted');
            case AiPendingActionStatus.Cancelled:
                return this.l('AiAgentStatusCancelled');
            case AiPendingActionStatus.Failed:
                return this.l('AiAgentStatusFailed');
            case AiPendingActionStatus.Expired:
                return this.l('AiAgentStatusExpired');
            default:
                return '';
        }
    }

    statusClass(action: AiPendingActionDto): string {
        switch (action.status) {
            case AiPendingActionStatus.Completed:
                return 'badge-light-success';
            case AiPendingActionStatus.Failed:
                return 'badge-light-danger';
            case AiPendingActionStatus.Pending:
            case AiPendingActionStatus.Processing:
                return 'badge-light-warning';
            default:
                return 'badge-light';
        }
    }

    private runAction(action: AiPendingActionDto, call: (body: EntityDto) => ReturnType<AiAgentServiceProxy['confirmAction']>): void {
        if (this.isInFlight(action)) {
            return;
        }

        this.actionsInFlight.update((set) => new Set(set).add(action.id));

        call(new EntityDto({ id: action.id }))
            .pipe(
                finalize(() =>
                    this.actionsInFlight.update((set) => {
                        const next = new Set(set);
                        next.delete(action.id);
                        return next;
                    })
                )
            )
            .subscribe({
                next: (result) => {
                    this.lines.update((lines) => [
                        ...lines.map((line) =>
                            line.message.pendingAction?.id === action.id
                                ? { ...line, message: this.withAction(line.message, result.action) }
                                : line
                        ),
                        { message: result.message },
                    ]);

                    if (result.action.status === AiPendingActionStatus.Completed) {
                        this.notify.success(result.message.content);
                    }

                    this.scrollToBottom();
                },
                // Already confirmed elsewhere, or expired: reload so the card shows its real state.
                error: () => this.reloadActiveConversation(),
            });
    }

    private withAction(message: AiMessageDto, action: AiPendingActionDto): AiMessageDto {
        const copy = AiMessageDto.fromJS(message.toJSON());
        copy.pendingAction = action;
        return copy;
    }

    private reloadActiveConversation(): void {
        const id = this.activeConversationId();
        if (!id) {
            return;
        }

        this._aiAgent.getConversation(id).subscribe((result) => {
            if (this.activeConversationId() === id) {
                this.lines.set((result.messages ?? []).map((message) => ({ message })));
            }
        });
    }

    // ------------------------------------------------------------------ view helpers

    private startWaitTimer(): void {
        this.stopWaitTimer();
        this.waitingSeconds.set(0);
        this.waitTimer = setInterval(() => this.waitingSeconds.update((s) => s + 1), 1000);
    }

    private stopWaitTimer(): void {
        if (this.waitTimer) {
            clearInterval(this.waitTimer);
            this.waitTimer = undefined;
        }
    }

    private scrollToBottom(): void {
        setTimeout(() => {
            const pane = this.messagesPane()?.nativeElement;
            if (pane) {
                pane.scrollTop = pane.scrollHeight;
            }
        });
    }

    private focusComposer(): void {
        setTimeout(() => this.composer()?.nativeElement.focus());
    }

    private get isRtl(): boolean {
        return document?.documentElement?.getAttribute('dir') === 'rtl';
    }
}
