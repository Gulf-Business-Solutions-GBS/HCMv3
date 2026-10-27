import { Component, OnInit, inject, viewChild, signal } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { AbpLoginResultType, UserLoginServiceProxy } from '@shared/service-proxies/service-proxies';
import { Table, TableModule } from 'primeng/table';
import { filter as _filter } from 'lodash-es';
import { finalize } from 'rxjs/operators';
import { DateTimeService } from '@app/shared/common/timing/date-time.service';
import { DateTime } from 'luxon';
import { LazyLoadEvent } from 'primeng/api';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { PrimengTableHelper } from 'shared/helpers/PrimengTableHelper';
import { SubHeaderComponent } from '../../shared/common/sub-header/sub-header.component';
import { FormsModule } from '@angular/forms';
import { DatePickerModule } from 'primeng/datepicker';
import { PrimengDateRangePickerLuxonModifierDirective } from '../../../shared/utils/date-time/primeng-date-range-picker-luxon-modifier.directive';
import { LoginResultTypeComboComponent } from './login-result-type.combo';
import { BusyIfDirective } from '../../../shared/utils/busy-if.directive';

import { LuxonFormatPipe } from '../../../shared/utils/luxon-format.pipe';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    templateUrl: './login-attempts.component.html',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        FormsModule,
        DatePickerModule,
        PrimengDateRangePickerLuxonModifierDirective,
        LoginResultTypeComboComponent,
        BusyIfDirective,
        TableModule,
        PaginatorModule,
        LuxonFormatPipe,
        LocalizePipe,
    ],
})
export class LoginAttemptsComponent extends AppComponentBase {
    private _dateTimeService = inject(DateTimeService);
    private _userLoginService = inject(UserLoginServiceProxy);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    filter = signal('');
    dateRange = signal<DateTime[]>([
        this._dateTimeService.getStartOfWeek().toDateTime(),
        this._dateTimeService.getEndOfDay().toDateTime(),
    ]);

    loginResultFilter = signal<AbpLoginResultType>('' as any);

    primengTableHelper = new PrimengTableHelper();

    onDateRangeChange(dateRange: DateTime[]): void {
        this.dateRange.set(dateRange);
        if (dateRange && dateRange.length === 2 && dateRange[0] && dateRange[1]) {
            this.getLoginAttempts();
        }
    }

    getLoginAttempts(event?: LazyLoadEvent): void {
        this.primengTableHelper.showLoadingIndicator();

        const range = this.dateRange();
        this._userLoginService
            .getUserLoginAttempts(
                this.filter(),
                this._dateTimeService.getStartOfDayForDate(range[0]).toDateTime(),
                this._dateTimeService.getEndOfDayForDate(range[1]).toDateTime(),
                this.loginResultFilter(),
                this.primengTableHelper.getSorting(this.dataTable()),
                this.primengTableHelper.getSkipCount(this.paginator(), event),
                this.primengTableHelper.getMaxResultCount(this.paginator(), event)
            )
            .pipe(finalize(() => this.primengTableHelper.hideLoadingIndicator()))
            .subscribe((result) => {
                this.primengTableHelper.records.set(result.items);
                this.primengTableHelper.totalRecordsCount.set(result.totalCount);
                this.primengTableHelper.hideLoadingIndicator();
            });
    }
}
