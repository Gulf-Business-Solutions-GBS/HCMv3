import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CountriesServiceProxy,
    CreateOrEditIndemnityPayRuleDto,
    GetAllSimpleLookupsInput,
    IndemnityPayRulesServiceProxy,
    SeparationType,
    SimpleLookupDto,
    Unit,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';

@Component({
    selector: 'createOrEditIndemnityPayRuleModal',
    templateUrl: './create-or-edit-indemnity-pay-rule-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditIndemnityPayRuleModalComponent extends AppComponentBase {
    private _indemnityPayRulesService = inject(IndemnityPayRulesServiceProxy);
    private _countriesService = inject(CountriesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly indemnityPayRule = signal<CreateOrEditIndemnityPayRuleDto>(new CreateOrEditIndemnityPayRuleDto());
    readonly countries = signal<SimpleLookupDto[]>([]);

    readonly separationTypes = [
        { value: SeparationType.Resignation, name: 'Resignation' },
        { value: SeparationType.Termination, name: 'Termination' },
    ];

    readonly units = [
        { value: Unit.Day, name: 'Day' },
        { value: Unit.Month, name: 'Month' },
    ];

    show(indemnityPayRuleId?: number): void {
        this.active.set(true);

        this._countriesService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.countries.set(result.items));

        if (!indemnityPayRuleId) {
            this.indemnityPayRule.set(new CreateOrEditIndemnityPayRuleDto());
            this.modal().show();
        } else {
            this._indemnityPayRulesService.getIndemnityPayRuleForEdit(indemnityPayRuleId).subscribe((result) => {
                this.indemnityPayRule.set(result.indemnityPayRule);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('IndemnityPayRule_SeparationType')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._indemnityPayRulesService
            .createOrEdit(this.indemnityPayRule())
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

    updateField(field: keyof CreateOrEditIndemnityPayRuleDto, value: any): void {
        const current = this.indemnityPayRule();
        (current as any)[field] = value;
        const next = new CreateOrEditIndemnityPayRuleDto();
        Object.assign(next, current);
        this.indemnityPayRule.set(next);
    }

    updateDateField(field: keyof CreateOrEditIndemnityPayRuleDto, value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }
}
