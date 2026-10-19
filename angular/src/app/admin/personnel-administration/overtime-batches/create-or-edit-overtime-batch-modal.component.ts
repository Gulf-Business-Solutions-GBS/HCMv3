import { Component, computed, inject, output, signal, viewChild } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditOvertimeBatchDto,
    CreateOrEditOvertimeBatchLineDto,
    EntityDto,
    GetOvertimeBatchCandidatesInput,
    OvertimeBatchLineDto,
    OvertimeBatchOrganizationUnitDto,
    OvertimeBatchOvertimeTypeDto,
    OvertimeBatchesServiceProxy,
    RequestStatus,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { Observable } from 'rxjs';
import { finalize, switchMap } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { Tooltip } from 'primeng/tooltip';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';

/**
 * Create, view or correct one department overtime batch (legacy Overtime Request form): pick the department and
 * dates, Get Data loads each employee's time-register days, enter Approved Hours per line, Submit puts the
 * batch forward for approval. Only lines with approved hours or a comment are kept, as in legacy.
 */
@Component({
    selector: 'createOrEditOvertimeBatchModal',
    templateUrl: './create-or-edit-overtime-batch-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, Tooltip, ButtonBusyDirective, BusyIfDirective, DecimalPipe, LocalizePipe],
})
export class CreateOrEditOvertimeBatchModalComponent extends AppComponentBase {
    private _service = inject(OvertimeBatchesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly loading = signal<boolean>(false);

    readonly batchId = signal<number | undefined>(undefined);
    readonly organizationUnitId = signal<number | undefined>(undefined);
    readonly startDate = signal<DateTime | undefined>(undefined);
    readonly endDate = signal<DateTime | undefined>(undefined);
    readonly comments = signal<string | undefined>(undefined);
    readonly onlyOvertimeDays = signal<boolean>(true);

    readonly lines = signal<OvertimeBatchLineDto[]>([]);
    readonly organizationUnits = signal<OvertimeBatchOrganizationUnitDto[]>([]);
    readonly overtimeTypes = signal<OvertimeBatchOvertimeTypeDto[]>([]);

    readonly isEditable = signal<boolean>(true);
    readonly workflowStatus = signal<RequestStatus | undefined>(undefined);
    readonly requestNumber = signal<string | undefined>(undefined);
    readonly canResubmit = signal<boolean>(false);

    /** Lines that will be kept on save: approved hours above zero or a comment (legacy rule). */
    readonly keptLineCount = computed(
        () => this.lines().filter((l) => (l.approvedHours ?? 0) > 0 || !!(l.comments ?? '').trim()).length
    );

    readonly totalApprovedHours = computed(() => this.lines().reduce((sum, l) => sum + (l.approvedHours ?? 0), 0));

    readonly canLoad = computed(
        () => this.isEditable() && !!this.organizationUnitId() && !!this.startDate() && !!this.endDate()
    );

    show(id?: number): void {
        this.active.set(true);
        this.batchId.set(id);
        this.lines.set([]);

        this._service.getOrganizationUnits().subscribe((result) => this.organizationUnits.set(result));
        this._service.getOvertimeTypes().subscribe((result) => this.overtimeTypes.set(result));

        if (!id) {
            const today = DateTime.now().startOf('day');
            this.organizationUnitId.set(undefined);
            this.startDate.set(today);
            this.endDate.set(today);
            this.comments.set(undefined);
            this.isEditable.set(true);
            this.workflowStatus.set(undefined);
            this.requestNumber.set(undefined);
            this.canResubmit.set(false);
            this.modal().show();
            return;
        }

        this._service.getOvertimeBatchForEdit(id).subscribe((result) => {
            this.organizationUnitId.set(result.overtimeBatch.organizationUnitId);
            this.startDate.set(result.overtimeBatch.startDate);
            this.endDate.set(result.overtimeBatch.endDate);
            this.comments.set(result.overtimeBatch.comments);
            this.lines.set(result.details ?? []);
            this.isEditable.set(result.isEditable && this.permission.isGranted('Pages.PersonnelAdministration.OvertimeBatches.Edit'));
            this.workflowStatus.set(result.workflowStatus);
            this.requestNumber.set(result.requestNumber);
            this.canResubmit.set(result.canResubmit);
            this.modal().show();
        });
    }

    /** Legacy GetTimeEventGrid. What was already entered on a line survives a reload of the same day. */
    loadCandidates(): void {
        if (!this.canLoad()) {
            return;
        }

        const previous = new Map<number, OvertimeBatchLineDto>();
        for (const line of this.lines()) {
            if (line.timeRegisterId) {
                previous.set(line.timeRegisterId, line);
            }
        }

        this.loading.set(true);
        this._service
            .getOvertimeBatchCandidates(
                new GetOvertimeBatchCandidatesInput({
                    organizationUnitId: this.organizationUnitId(),
                    startDate: this.startDate(),
                    endDate: this.endDate(),
                    onlyOvertimeDays: this.onlyOvertimeDays(),
                    excludeOvertimeBatchId: this.batchId(),
                })
            )
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe((candidates) => {
                for (const candidate of candidates) {
                    const kept = candidate.timeRegisterId ? previous.get(candidate.timeRegisterId) : undefined;
                    if (kept) {
                        candidate.approvedHours = kept.approvedHours;
                        candidate.overtimeTypeId = kept.overtimeTypeId;
                        candidate.comments = kept.comments;
                    }
                }

                this.lines.set(candidates);
            });
    }

    save(): void {
        const input = new CreateOrEditOvertimeBatchDto({
            id: this.batchId(),
            organizationUnitId: this.organizationUnitId(),
            startDate: this.startDate(),
            endDate: this.endDate(),
            comments: this.comments(),
            details: this.lines()
                .filter((l) => !!l.timeRegisterId && !l.hasExistingOvertime)
                .map(
                    (l) =>
                        new CreateOrEditOvertimeBatchLineDto({
                            timeRegisterId: l.timeRegisterId,
                            approvedHours: l.approvedHours ?? 0,
                            overtimeTypeId: l.overtimeTypeId,
                            comments: l.comments,
                        })
                ),
        });

        this.saving.set(true);

        // A sent-back batch is corrected and then put forward again in the same click.
        const call: Observable<unknown> = !this.batchId()
            ? this._service.createOvertimeBatch(input)
            : this.canResubmit()
              ? this._service
                    .updateOvertimeBatch(input)
                    .pipe(switchMap(() => this._service.resubmitOvertimeBatch(new EntityDto({ id: this.batchId() }))))
              : this._service.updateOvertimeBatch(input);

        call.pipe(finalize(() => this.saving.set(false))).subscribe(() => {
            this.notify.info(this.l('SavedSuccessfully'));
            this.close();
            this.modalSave.emit();
        });
    }

    close(): void {
        this.active.set(false);
        this.modal().hide();
    }

    updateLine(index: number, field: 'approvedHours' | 'overtimeTypeId' | 'comments', value: any): void {
        const next = [...this.lines()];
        const line = new OvertimeBatchLineDto();
        Object.assign(line, next[index]);
        (line as any)[field] = field === 'approvedHours' ? Number(value) || 0 : value;
        next[index] = line;
        this.lines.set(next);
    }

    updateStartDate(value: string): void {
        this.startDate.set(value ? DateTime.fromISO(value) : undefined);
    }

    updateEndDate(value: string): void {
        this.endDate.set(value ? DateTime.fromISO(value) : undefined);
    }

    statusLabel(): string {
        const status = this.workflowStatus();
        if (status === undefined || status === null) {
            return this.batchId() ? this.l('Recorded') : '';
        }

        return this.l('WorkflowRequestStatus_' + RequestStatus[status]);
    }

    employeeName(line: OvertimeBatchLineDto): string {
        const rtl = document?.documentElement?.getAttribute('dir') === 'rtl';
        return rtl ? line.employeeNameAr || line.employeeNameEn || '' : line.employeeNameEn || '';
    }

    typeName(line: OvertimeBatchLineDto): string {
        const rtl = document?.documentElement?.getAttribute('dir') === 'rtl';
        return rtl ? line.overtimeTypeNameAr || line.overtimeTypeNameEn || '' : line.overtimeTypeNameEn || '';
    }

    overtimeTypeLabel(type: OvertimeBatchOvertimeTypeDto): string {
        const rtl = document?.documentElement?.getAttribute('dir') === 'rtl';
        return rtl ? type.nameAr || type.nameEn || '' : type.nameEn || '';
    }

    /** Indents by tree depth: ABP unit codes are dot-separated, five digits per level. */
    unitLabel(unit: OvertimeBatchOrganizationUnitDto): string {
        const depth = Math.max(0, (unit.code ?? '').split('.').length - 1);
        return '  '.repeat(depth) + (unit.displayName ?? '');
    }

    date(value: DateTime | undefined): string {
        return !value || value.year <= 1 ? '' : value.toFormat('yyyy-MM-dd');
    }

    time(value: DateTime | undefined): string {
        return !value || value.year <= 1900 ? '' : value.toFormat('HH:mm');
    }
}
