import { Component, computed, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    AppraisalStepSettingLookupsDto,
    AppraisalStepSettingRelationshipTypeDto,
    AppraisalStepSettingRequestMasterDto,
    AppraisalStepSettingsServiceProxy,
    CreateOrEditAppraisalStepSettingDto,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

/** One switch on the dialog: its DTO field and the legacy caption key. */
interface StepFlag {
    field: keyof CreateOrEditAppraisalStepSettingDto;
    label: string;
}

/**
 * Create or edit one "Appraisal WF Setup" row. The step list follows the chosen request master, so only a
 * step the chain defines can be picked -- the server refuses any other, as legacy did.
 */
@Component({
    selector: 'createOrEditAppraisalStepSettingModal',
    templateUrl: './create-or-edit-appraisal-step-setting-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditAppraisalStepSettingModalComponent extends AppComponentBase {
    private _service = inject(AppraisalStepSettingsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal(false);
    readonly saving = signal(false);
    readonly stepSetting = signal<CreateOrEditAppraisalStepSettingDto>(new CreateOrEditAppraisalStepSettingDto());
    readonly lookups = signal<AppraisalStepSettingLookupsDto | undefined>(undefined);

    /** The steps of the chosen request master. */
    readonly steps = computed(() => {
        const masterId = this.stepSetting().requestMasterId;
        return this.lookups()?.requestMasters?.find((m) => m.id === masterId)?.steps ?? [];
    });

    /** Grouped as legacy's detail view showed them: who is added, then what they may do, then what they may see. */
    readonly addFlags: StepFlag[] = [
        { field: 'canEmployeeSelfRate', label: 'CanEmployeeSelfRate' },
        { field: 'canAddRatings', label: 'CanAddRatings' },
        { field: 'autoAddManager', label: 'AutoAddManager' },
        { field: 'autoAddRelationShip', label: 'AutoAddRelationShip' },
    ];

    readonly doFlags: StepFlag[] = [
        { field: 'canAddKpi', label: 'CanAddKpi' },
        { field: 'canAddPeer', label: 'CanAddPeer' },
        { field: 'canPeerAddRatings', label: 'CanPeerAddRatings' },
        { field: 'autoSubmitOnLastFill', label: 'AutoSubmitOnLastFill' },
    ];

    readonly viewFlags: StepFlag[] = [
        { field: 'canViewRatings', label: 'CanViewRatings' },
        { field: 'canViewPeerRatings', label: 'CanViewPeerRatings' },
        { field: 'canViewOverallAverageRating', label: 'CanViewOverallAverageRating' },
    ];

    show(id?: number): void {
        this.active.set(true);

        this._service.getLookups().subscribe((lookups) => {
            this.lookups.set(lookups);

            if (!id) {
                const dto = new CreateOrEditAppraisalStepSettingDto();
                dto.relationshipTypeIds = [];
                dto.allowedPeerCount = 0;
                this.stepSetting.set(dto);
                this.modal().show();
                return;
            }

            this._service.getAppraisalStepSettingForEdit(id).subscribe((result) => {
                const dto = result.appraisalStepSetting;
                dto.relationshipTypeIds = dto.relationshipTypeIds ?? [];
                this.stepSetting.set(dto);
                this.modal().show();
            });
        });
    }

    onShown(): void {
        document.getElementById('AppraisalStepSetting_RequestMasterId')?.focus();
    }

    masterLabel(master: AppraisalStepSettingRequestMasterDto): string {
        const name = this.isRtl ? master.nameAr || master.nameEn : master.nameEn;
        return master.code ? `${master.code} - ${name}` : name;
    }

    relationshipLabel(type: AppraisalStepSettingRelationshipTypeDto): string {
        return this.isRtl ? type.nameAr || type.nameEn : type.nameEn;
    }

    hasRelationship(typeId: number): boolean {
        return (this.stepSetting().relationshipTypeIds ?? []).includes(typeId);
    }

    toggleRelationship(typeId: number, checked: boolean): void {
        const current = this.stepSetting().relationshipTypeIds ?? [];
        this.updateField(
            'relationshipTypeIds',
            checked ? [...current.filter((id) => id !== typeId), typeId] : current.filter((id) => id !== typeId)
        );
    }

    onMasterChange(masterId: number): void {
        this.updateField('requestMasterId', masterId);

        // A step from the previous chain is meaningless on this one.
        if (!this.steps().includes(this.stepSetting().step)) {
            this.updateField('step', undefined);
        }
    }

    save(): void {
        this.saving.set(true);
        this._service
            .createOrEdit(this.stepSetting())
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

    flagValue(field: keyof CreateOrEditAppraisalStepSettingDto): boolean {
        return !!this.stepSetting()[field];
    }

    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    updateField(field: keyof CreateOrEditAppraisalStepSettingDto, value: any): void {
        const next = new CreateOrEditAppraisalStepSettingDto();
        Object.assign(next, this.stepSetting());
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        (next as any)[field] = value;
        this.stepSetting.set(next);
    }

    private get isRtl(): boolean {
        return document?.documentElement?.getAttribute('dir') === 'rtl';
    }
}
