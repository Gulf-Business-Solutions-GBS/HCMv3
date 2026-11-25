import { Component, OnInit, computed, inject, signal, viewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { SalaryRegisterDto, SalaryRegistersServiceProxy } from '@shared/service-proxies/service-proxies';
import { DateTime } from 'luxon';
import { finalize } from 'rxjs/operators';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import { CreateOrEditSalaryRegisterModalComponent } from '../create-or-edit-salary-register-modal.component';
import { SalaryRegisterLinesComponent } from './salary-register-lines.component';

/**
 * One salary register, as the legacy SalaryRegister detail view showed it: the header read-only (Edit opens
 * the usual dialog) and the register's wage type lines below it, read-only (they are the payroll engine's output).
 */
@Component({
    templateUrl: './salary-register-detail.component.html',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        BusyIfDirective,
        CreateOrEditSalaryRegisterModalComponent,
        SalaryRegisterLinesComponent,
        LocalizePipe,
        LuxonFormatPipe,
    ],
})
export class SalaryRegisterDetailComponent extends AppComponentBase implements OnInit {
    private _route = inject(ActivatedRoute);
    private _router = inject(Router);
    private _registersService = inject(SalaryRegistersServiceProxy);

    readonly editModal = viewChild<CreateOrEditSalaryRegisterModalComponent>('editModal');

    readonly registerId = signal<number | undefined>(undefined);
    readonly register = signal<SalaryRegisterDto | undefined>(undefined);
    readonly loading = signal<boolean>(false);

    /** Legacy caption, e.g. "5363 Reg(Jun-2024)", with "Retro(May-2024)" when for and in period differ. */
    readonly title = computed(() => {
        const register = this.register();
        if (!register?.employeeCode) {
            return this.l('SalaryRegisters');
        }
        const run = register.payrollRunType === 0 ? this.l('Regular') : this.l('OffCycle');
        const month = (d: DateTime) => d?.toFormat('MMM-yyyy');
        const retro =
            register.forPeriod && register.inPeriod && !register.forPeriod.hasSame(register.inPeriod, 'day')
                ? ` ${this.l('Retro')}(${month(register.forPeriod)})`
                : '';
        return `${this.l('SalaryRegisters')} - ${register.employeeCode} ${run} (${month(register.inPeriod)})${retro}`;
    });

    readonly canEdit = this.permission.isGranted('Pages.PayrollManagement.SalaryRegisters.Edit');
    readonly canSeeLines = this.permission.isGranted('Pages.PayrollManagement.SalaryRegisterDetails');

    ngOnInit(): void {
        this._route.paramMap.subscribe((params) => {
            const id = Number(params.get('id'));
            if (id) {
                this.registerId.set(id);
                this.load(id);
            }
        });
    }

    load(id: number): void {
        this.loading.set(true);
        this._registersService
            .getSalaryRegister(id)
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe((result) => this.register.set(result));
    }

    edit(): void {
        if (this.canEdit && this.registerId()) {
            this.editModal().show(this.registerId());
        }
    }

    reload(): void {
        if (this.registerId()) {
            this.load(this.registerId());
        }
    }

    goBack(): void {
        this._router.navigate(['/app/admin/salary-registers']);
    }

    /** DateTime.MinValue (01/01/0001) is the backend's "not set"; show it blank. */
    isSet(date: DateTime | undefined): boolean {
        return !!date && date.year > 1;
    }

    payrollResultTypeKey(value: number | undefined): string {
        return value === 0 ? 'Current' : value === 1 ? 'Last' : 'Archive';
    }
}
