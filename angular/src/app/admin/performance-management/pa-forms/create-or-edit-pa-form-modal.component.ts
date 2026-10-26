import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditPAFormDto,
    EmployeeDto,
    EmployeesServiceProxy,
    GetAllEmployeesInput,
    GetAllPAPeriodsInput,
    PAFormsServiceProxy,
    PAFormTickRulesDto,
    EntityDto,
    PAPeriodDto,
    PAPeriodsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditPAFormModal',
    templateUrl: './create-or-edit-pa-form-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditPAFormModalComponent extends AppComponentBase {
    private _service = inject(PAFormsServiceProxy);
    private _paPeriodsService = inject(PAPeriodsServiceProxy);
    private _employeesService = inject(EmployeesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly paForm = signal<CreateOrEditPAFormDto>(new CreateOrEditPAFormDto());
    readonly paPeriods = signal<PAPeriodDto[]>([]);
    readonly employees = signal<EmployeeDto[]>([]);
    /** Calculated by the appraisal process and not on the edit DTO, so the list row passes it in. Display only. */
    readonly overallScore = signal<number | undefined>(undefined);

    /**
     * Which of the twelve boxes the signed-in employee may see and tick, from the server. Until it arrives
     * every box is shown and none can be ticked -- offering a box and then having the save refuse it would
     * be worse than a moment of grey.
     */
    readonly tickRules = signal<PAFormTickRulesDto | undefined>(undefined);

    /** The twelve boxes in legacy order: the rule's name for each, and the DTO field it writes. */
    readonly boxes: { name: string; field: keyof CreateOrEditPAFormDto }[] = [
        { name: 'PlanSubmitByAppraiser', field: 'planSubmitByAppraiser' },
        { name: 'PlanSubmitByAppraisee', field: 'planSubmitByAppraisee' },
        { name: 'Quarter1SubmitByAppraiser', field: 'quarter1SubmitByAppraiser' },
        { name: 'Quarter1SubmitByAppraisee', field: 'quarter1SubmitByAppraisee' },
        { name: 'MRSubmitByAppraiser', field: 'mrSubmitByAppraiser' },
        { name: 'MRSubmitByAppraisee', field: 'mrSubmitByAppraisee' },
        { name: 'Quarter3SubmitByAppraiser', field: 'quarter3SubmitByAppraiser' },
        { name: 'Quarter3SubmitByAppraisee', field: 'quarter3SubmitByAppraisee' },
        { name: 'ERSubmitByAppraiser', field: 'erSubmitByAppraiser' },
        { name: 'ERSubmitByAppraisee', field: 'erSubmitByAppraisee' },
        { name: 'AppraiserSignedOff', field: 'appraiserSignedOff' },
        { name: 'AppraiseeSignedOff', field: 'appraiseeSignedOff' },
    ];

    boxVisible(name: string): boolean {
        const rule = this.tickRules()?.boxes?.find((b) => b.box === name);
        return rule ? !!rule.visible : true;
    }

    boxEnabled(name: string): boolean {
        const rule = this.tickRules()?.boxes?.find((b) => b.box === name);
        return !!rule?.enabled;
    }

    /** A nominated peer marks their own peer review submitted. Saved at once; there is nothing else for them to save. */
    submitPeerReview(ticked: boolean): void {
        const id = this.paForm().id;
        if (!ticked || !id) {
            return;
        }

        this.saving.set(true);
        this._service
            .submitPeerReview(new EntityDto({ id }))
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe(() => {
                this.notify.info(this.l('SavedSuccessfully'));
                this.loadTickRules(id);
            });
    }

    private loadTickRules(id?: number): void {
        this.tickRules.set(undefined);
        this._service.getPAFormTickRules(id).subscribe((rules) => this.tickRules.set(rules));
    }

    show(id?: number, overallScore?: number): void {
        this.active.set(true);
        this.overallScore.set(id ? overallScore : undefined);
        this.loadTickRules(id);

        this._paPeriodsService
            .getAll(
                new GetAllPAPeriodsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined })
            )
            .subscribe((result) => this.paPeriods.set(result.items));

        this._employeesService
            .getAll(
                new GetAllEmployeesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined })
            )
            .subscribe((result) => this.employees.set(result.items));

        if (!id) {
            this.paForm.set(new CreateOrEditPAFormDto());
            this.modal().show();
        } else {
            this._service.getPAFormForEdit(id).subscribe((result) => {
                this.paForm.set(result.paForm);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('PAForm_PAPeriodId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._service
            .createOrEdit(this.paForm())
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe(() => {
                this.notify.info(this.l('SavedSuccessfully'));
                this.close();
                this.modalSave.emit();
            });
    }

    close(): void {
        this.active.set(false);
        this.modal().hide();
    }

    employeeLabel(employee: EmployeeDto): string {
        return `${employee.code} - ${employee.fullNameEn}`;
    }

    updateField(field: keyof CreateOrEditPAFormDto, value: any): void {
        const current = this.paForm();
        (current as any)[field] = value;
        const next = new CreateOrEditPAFormDto();
        Object.assign(next, current);
        this.paForm.set(next);
    }
}
