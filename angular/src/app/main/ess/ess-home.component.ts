import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { RouterModule } from '@angular/router';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { EssContextDto, EssSessionServiceProxy, EssTransactionTypeDto } from '@shared/service-proxies/service-proxies';
import { finalize } from 'rxjs/operators';
import { SubHeaderComponent } from '../../shared/common/sub-header/sub-header.component';

/**
 * The Employee Self Service landing page.
 *
 * The tile list is NOT hardcoded: it comes from the WorkflowTransactionType catalog via GetEssContext, so
 * enabling a seventh self-service transaction is a catalog row plus a form, not a change here. That is also
 * why this lives in the admin application at all -- the admin menu is permission-filtered, so an employee
 * holding only the Employee role never sees it, and legacy HCM10's separate ESS project is not needed.
 */
@Component({
    templateUrl: './ess-home.component.html',
    animations: [appModuleAnimation()],
    imports: [SubHeaderComponent, LocalizePipe, RouterModule],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EssHomeComponent extends AppComponentBase implements OnInit {
    private readonly _essSessionService = inject(EssSessionServiceProxy);

    readonly loading = signal(true);
    readonly context = signal<EssContextDto | undefined>(undefined);

    ngOnInit(): void {
        this.loadContext();
    }

    loadContext(): void {
        this.loading.set(true);

        this._essSessionService
            .getEssContext()
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe((result) => this.context.set(result));
    }

    /**
     * Employee names follow the UI language rather than being transliterated -- the English and Arabic name
     * fields are maintained separately on Employee.
     */
    employeeName(): string {
        const ctx = this.context();
        if (!ctx) {
            return '';
        }

        return this.isRtl ? ctx.employeeFullNameAr || ctx.employeeFullNameEn : ctx.employeeFullNameEn;
    }

    typeName(type: EssTransactionTypeDto): string {
        return this.isRtl ? type.nameAr || type.nameEn : type.nameEn;
    }

    private get isRtl(): boolean {
        return document?.documentElement?.getAttribute('dir') === 'rtl';
    }
}
