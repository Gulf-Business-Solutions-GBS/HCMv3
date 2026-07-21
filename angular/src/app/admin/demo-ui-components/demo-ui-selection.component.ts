import { Component, inject, signal } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { DemoUiComponentsServiceProxy, NameValueOfString } from '@shared/service-proxies/service-proxies';
import { forEach as _forEach } from 'lodash-es';
import { FormsModule } from '@angular/forms';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'demo-ui-selection',
    templateUrl: './demo-ui-selection.component.html',
    animations: [appModuleAnimation()],
    imports: [FormsModule, AutoCompleteModule, LocalizePipe],
})
export class DemoUiSelectionComponent extends AppComponentBase {
    private demoUiComponentsService = inject(DemoUiComponentsServiceProxy);

    filteredCountries = signal<NameValueOfString[]>([]);
    country = signal<any>(null);
    countries = signal<NameValueOfString[]>(new Array<NameValueOfString>());

    optionValueFn = (option) => option as any;

    // get countries
    filterCountries(event): void {
        this.demoUiComponentsService.getCountries(event.query).subscribe((countries) => {
            this.filteredCountries.set(countries);
        });
    }

    // single select - post
    submitSelectedCountry(): void {
        let selectedCountries = new Array<NameValueOfString>();

        selectedCountries.push(this.country());

        this.demoUiComponentsService
            .sendAndGetSelectedCountries(selectedCountries)
            .subscribe((countries: NameValueOfString[]) => {
                let message = '';

                _forEach(countries, (item) => {
                    message += `<div><strong>id</strong>: ${item.value} - <strong>name</strong>: ${item.name}</div>`;
                });

                this.message.info(message, this.l('PostedValue'), { isHtml: true });
            });
    }

    // multi select - post
    submitSelectedCountries(): void {
        this.demoUiComponentsService
            .sendAndGetSelectedCountries(this.countries())
            .subscribe((countries: NameValueOfString[]) => {
                let message = '';

                _forEach(countries, (item) => {
                    message += `<div><strong>id</strong>: ${item.value} - <strong>name</strong>: ${item.name}</div>`;
                });

                this.message.info(message, this.l('PostedValue'), { isHtml: true });
            });
    }

    onCountrySelect(event: NameValueOfString): void {
        this.country.set(event.value);
    }
}
