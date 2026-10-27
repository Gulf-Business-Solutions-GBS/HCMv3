import { ChangeDetectionStrategy, Component, OnInit, computed, effect, inject, signal, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { EssContextDto, EssSessionServiceProxy, MyDocumentRequestDto } from '@shared/service-proxies/service-proxies';
import { HelpPageDirective } from '@app/shared/help/help-page.directive';
import { SubHeaderComponent } from '../../../shared/common/sub-header/sub-header.component';
import { MyDocumentComponent } from '../my-document/my-document.component';
import { MyLetterComponent } from '../my-letter/my-letter.component';
import { MyPassportWithdrawalComponent } from '../my-passport-withdrawal/my-passport-withdrawal.component';
import { EssRequestDetailModalComponent } from '../shared/ess-request-detail-modal.component';

/** What New Request was asked for. The document type is chosen on the form that opens. */
type NewRequestChoice = 'document' | 'withdrawal' | 'letter';

/**
 * The one self-service screen for everything an employee does with documents: recording a new or renewed
 * document, taking one out of company custody and handing it back, and asking for a letter.
 *
 * <p><b>One screen, three records, no code of its own for any of them.</b> Each request is still its own
 * transaction with its own workflow request -- a document (<c>EmployeeDocument</c>), a movement
 * (<c>EmployeeDocumentMvmt</c>), a letter (<c>EmployeeLetterRequest</c>) -- so the approval chain, the log,
 * the comments and the notifications are the engine's, exactly as for any other type. What this screen adds
 * is only the front door: one New Request button, where the request type chosen decides which form opens,
 * and the three lists side by side so a document and what has been asked of it are read together.</p>
 *
 * <p>Nothing here knows about passports or salary certificates. Which types can be recorded, withdrawn or
 * asked for as a letter comes from the switches on each document type.</p>
 *
 * <p>A section is shown only when its transaction type is switched on for self-service and the employee
 * holds its permission -- the ESS context already answers both.</p>
 */
@Component({
    templateUrl: './my-document-center.component.html',
    animations: [appModuleAnimation()],
    imports: [
        CommonModule,
        FormsModule,
        SubHeaderComponent,
        LocalizePipe,
        HelpPageDirective,
        MyDocumentComponent,
        MyPassportWithdrawalComponent,
        MyLetterComponent,
        EssRequestDetailModalComponent,
    ],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyDocumentCenterComponent extends AppComponentBase implements OnInit {
    private readonly _essSessionService = inject(EssSessionServiceProxy);

    /** A request named in the address, waiting for the lists to arrive so it can be opened. Used once. */
    private requestToOpen: number | undefined =
        Number(inject(ActivatedRoute).snapshot.queryParamMap.get('requestId')) || undefined;

    readonly context = signal<EssContextDto | undefined>(undefined);
    readonly contextLoaded = signal(false);

    /** An account with no employee record: a setup gap for HR to close, explained rather than shown empty. */
    readonly notLinked = computed(() => this.contextLoaded() && !this.context()?.isLinkedToEmployee);

    readonly showDocuments = computed(() => this.serves('EmployeeDocument'));
    readonly showWithdrawals = computed(() => this.serves('EmployeeDocumentMvmt'));
    readonly showLetters = computed(() => this.serves('EmployeeLetterRequest'));

    readonly documents = viewChild(MyDocumentComponent);
    readonly withdrawals = viewChild(MyPassportWithdrawalComponent);
    readonly letters = viewChild(MyLetterComponent);
    readonly detail = viewChild<EssRequestDetailModalComponent>('detail');

    /** The New Request chooser is open. */
    readonly choosing = signal(false);

    /** The chooser's current pick; reset each time it opens. */
    choice: NewRequestChoice | null = null;

    /**
     * The section whose form is open, if any. While one is, the other sections step aside, so the form is
     * the only thing on the screen -- as it is on every other request screen.
     */
    readonly formOpenIn = computed<'documents' | 'withdrawals' | 'letters' | null>(() => {
        if (this.documents()?.showForm()) {
            return 'documents';
        }

        if (this.withdrawals()?.showForm()) {
            return 'withdrawals';
        }

        return this.letters()?.showForm() ? 'letters' : null;
    });

    /**
     * The help page for what is in front: each request form keeps the page written for it, and the lists
     * are covered by the Documents page.
     */
    readonly helpId = computed(() => {
        switch (this.formOpenIn()) {
            case 'withdrawals':
                return 'ess.passport-withdrawal';
            case 'letters':
                return 'ess.letter';
            default:
                return 'ess.document';
        }
    });

    constructor() {
        super();

        // Opens the request the address names, once every list on the screen has arrived: on its row in
        // whichever list holds it, or straight from the request when none does -- a past record no list
        // shows any more is still a request somebody followed a link to.
        effect(() => {
            const requestId = this.requestToOpen;

            if (!requestId || !this.contextLoaded()) {
                return;
            }

            const sections = [
                this.showDocuments() ? this.documents() : undefined,
                this.showWithdrawals() ? this.withdrawals() : undefined,
                this.showLetters() ? this.letters() : undefined,
            ];

            const expected = [this.showDocuments(), this.showWithdrawals(), this.showLetters()].filter(
                (shown) => shown
            ).length;
            const present = sections.filter((section) => !!section);

            if (present.length < expected || present.some((section) => !section!.loaded() && !section!.notLinked())) {
                return;
            }

            this.requestToOpen = undefined;

            if (!present.some((section) => section!.openRequest(requestId))) {
                this.detail()?.show({
                    title: this.l('EssMyDocuments'),
                    workflowRequestId: requestId,
                    statusLabel: '',
                    statusClass: '',
                });
            }
        });
    }

    ngOnInit(): void {
        this._essSessionService.getEssContext().subscribe({
            next: (result) => {
                this.context.set(result);
                this.contextLoaded.set(true);
            },
            error: () => this.contextLoaded.set(true),
        });
    }

    /** Whether this employee may use the given transaction: switched on for self-service, and permitted. */
    private serves(code: string): boolean {
        return !!this.context()?.availableTransactionTypes?.some((type) => type.code === code);
    }

    /** New Request: asks which kind of request it is, then opens the form that request is made on. */
    openChooser(): void {
        this.choice = null;
        this.choosing.set(true);
    }

    closeChooser(): void {
        this.choosing.set(false);
    }

    /** Opens the form the chosen request is made on, in the section that owns it. */
    start(): void {
        const choice = this.choice;

        if (!choice) {
            this.message.warn(this.l('NewRequestTypeIsRequired'));
            return;
        }

        this.choosing.set(false);

        switch (choice) {
            case 'document':
                this.documents()?.openForm();
                break;
            case 'letter':
                this.letters()?.openForm();
                break;
            default:
                this.withdrawals()?.openForm();
        }
    }

    /** Withdraw on a document: the withdrawal form opens with that document already chosen. */
    withdraw(row: MyDocumentRequestDto): void {
        const type = this.isRtl ? row.documentTypeNameAr || row.documentTypeNameEn || '' : row.documentTypeNameEn || '';

        this.withdrawals()?.openFormForDocument(row.id, row.documentNum ? type + ' - ' + row.documentNum : type);
    }

    private get isRtl(): boolean {
        return document?.documentElement?.getAttribute('dir') === 'rtl';
    }
}
