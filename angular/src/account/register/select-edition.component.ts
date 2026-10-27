import { AfterViewInit, Component, OnInit, ViewEncapsulation, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { accountModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    EditionSelectDto,
    EditionWithFeaturesDto,
    EditionsSelectOutput,
    FlatFeatureSelectDto,
    TenantRegistrationServiceProxy,
    SubscriptionStartType,
} from '@shared/service-proxies/service-proxies';
import { filter as _filter } from 'lodash-es';
import { DecimalPipe } from '@angular/common';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    templateUrl: './select-edition.component.html',
    styleUrls: ['./select-edition.component.less'],
    encapsulation: ViewEncapsulation.None,
    animations: [accountModuleAnimation()],
    imports: [RouterLink, DecimalPipe, LocalizePipe],
})
export class SelectEditionComponent extends AppComponentBase implements OnInit, AfterViewInit {
    private _tenantRegistrationService = inject(TenantRegistrationServiceProxy);
    private _router = inject(Router);

    readonly editionsSelectOutput = signal<EditionsSelectOutput>(new EditionsSelectOutput());
    isUserLoggedIn = false;
    subscriptionStartType: typeof SubscriptionStartType = SubscriptionStartType;
    readonly paymentPeriodType = signal('monthly');

    ngOnInit() {
        this.isUserLoggedIn = abp.session.userId > 0;

        this._tenantRegistrationService.getEditionsForSelect().subscribe((result) => {
            this.editionsSelectOutput.set(result);

            if (
                !this.editionsSelectOutput().editionsWithFeatures ||
                this.editionsSelectOutput().editionsWithFeatures.length <= 0
            ) {
                this._router.navigate(['/account/register-tenant']);
            }
        });
    }

    ngAfterViewInit(): void {
        KTApp.init();
    }

    isFree(edition: EditionSelectDto): boolean {
        return edition.isFree;
    }

    isTrueFalseFeature(feature: FlatFeatureSelectDto): boolean {
        return feature.inputType.name === 'CHECKBOX';
    }

    featureEnabledForEdition(feature: FlatFeatureSelectDto, edition: EditionWithFeaturesDto): boolean {
        const featureValues = _filter(edition.featureValues, { name: feature.name });
        if (!featureValues || featureValues.length <= 0) {
            return false;
        }

        const featureValue = featureValues[0];
        return featureValue.value.toLowerCase() === 'true';
    }

    getFeatureValueForEdition(feature: FlatFeatureSelectDto, edition: EditionWithFeaturesDto): string {
        const featureValues = _filter(edition.featureValues, { name: feature.name });
        if (!featureValues || featureValues.length <= 0) {
            return '';
        }

        const featureValue = featureValues[0];
        return featureValue.value;
    }

    changePaymentPeriodType(paymentPeriodType: string): void {
        this.paymentPeriodType.set(paymentPeriodType);
    }
}
