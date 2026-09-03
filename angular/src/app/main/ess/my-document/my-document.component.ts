import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import {
    EntityDto,
    EssLookupServiceProxy,
    GetMyRequestsInput,
    MyDocumentRequestDto,
    MyDocumentServiceProxy,
    RequestDocumentInput,
    SimpleLookupDto,
} from '@shared/service-proxies/service-proxies';
import { DateTime } from 'luxon';
import { Observable } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { SubHeaderComponent } from '../../../shared/common/sub-header/sub-header.component';
import { WorkflowHistoryModalComponent } from '../../../shared/common/workflow-activity/workflow-history-modal.component';
import { MyRequestComponentBase } from '../my-request-component-base';

/** Self-service document requests. */
@Component({
    templateUrl: './my-document.component.html',
    animations: [appModuleAnimation()],
    imports: [CommonModule, FormsModule, SubHeaderComponent, LocalizePipe, LuxonFormatPipe, WorkflowHistoryModalComponent],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyDocumentComponent extends MyRequestComponentBase<MyDocumentRequestDto> implements OnInit {
    private readonly _service = inject(MyDocumentServiceProxy);
    private readonly _lookups = inject(EssLookupServiceProxy);

    readonly documentTypes = signal<SimpleLookupDto[]>([]);

    form = { documentTypeId: null as number | null, documentNum: '', issueDate: '', expiryDate: '', comments: '' };

    ngOnInit(): void {
        // Every document type, not only the letter ones -- this screen is "I hold document X", which the
        // letter form's narrower list would wrongly exclude a passport from.
        this._lookups.getDocumentTypes().subscribe((result) => this.documentTypes.set(result ?? []));

        this.reload();
    }

    protected fetch(input: GetMyRequestsInput): Observable<{ items: MyDocumentRequestDto[] }> {
        return this._service.getMyDocuments(input);
    }

    protected cancelRequest(input: EntityDto): Observable<void> {
        return this._service.cancelMyDocument(input);
    }

    protected resetForm(): void {
        this.form = { documentTypeId: null, documentNum: '', issueDate: '', expiryDate: '', comments: '' };
    }

    submit(): void {
        if (!this.form.documentTypeId || !this.form.issueDate || !this.form.expiryDate || this.saving()) {
            return;
        }

        // Refused here as well as by the server, so the employee is told before the round trip.
        if (!this.form.documentNum?.trim()) {
            this.message.warn(this.l('DocumentNumberIsRequired'));
            return;
        }

        const input = new RequestDocumentInput();
        input.documentTypeId = this.form.documentTypeId;
        input.documentNum = this.form.documentNum.trim();
        input.issueDate = DateTime.fromISO(this.form.issueDate);
        input.expiryDate = DateTime.fromISO(this.form.expiryDate);
        input.comments = this.form.comments;

        this.saving.set(true);

        this._service
            .requestDocument(input)
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe((result) => this.notifySubmitted(result.submittedForApproval));
    }

    typeName(row: MyDocumentRequestDto): string {
        return this.pick(row.documentTypeNameEn, row.documentTypeNameAr);
    }
}
