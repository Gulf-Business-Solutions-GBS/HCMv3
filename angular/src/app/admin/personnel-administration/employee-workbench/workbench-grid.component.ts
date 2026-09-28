import {
    Component,
    ComponentRef,
    DestroyRef,
    Injector,
    Type,
    ViewContainerRef,
    computed,
    effect,
    inject,
    input,
    signal,
    untracked,
} from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import {
    EmployeeWorkbenchPayslipsServiceProxy,
    GetWorkbenchPayslipPdfInput,
    SalaryRegisterDto,
} from '@shared/service-proxies/service-proxies';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import { DateTime } from 'luxon';
import { Subscription } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { WorkbenchCellContext, WorkbenchColumn, WorkbenchForm, WorkbenchSubView } from './employee-workbench.tiles';

/** Older than this is a "no date" sentinel (legacy 1800-01-01, or .NET's default), shown as blank. */
const BLANK_DATE_YEAR_CEILING = 1900;

/**
 * One workbench sub-link: the last few records, a New button, and an edit button per row.
 *
 * The create and edit forms are the Employee Detail forms themselves, created on first use into the
 * page's `formHost` and kept for the life of the sub-link, so reopening one does not rebuild it.
 */
@Component({
    selector: 'workbench-grid',
    templateUrl: './workbench-grid.component.html',
    imports: [DecimalPipe, RouterLink, LocalizePipe, LuxonFormatPipe],
})
export class WorkbenchGridComponent extends AppComponentBase {
    private readonly _injector = inject(Injector);
    private readonly _payslips = inject(EmployeeWorkbenchPayslipsServiceProxy);

    readonly subView = input.required<WorkbenchSubView>();
    readonly employeeId = input.required<number>();
    readonly isExEmployee = input(false);

    /**
     * Where the forms are created: a container the page keeps OUTSIDE the drawer and popup. Those two are
     * fixed-position layers with their own z-index, and a Bootstrap modal rendered inside one would sit
     * under the modal backdrop Bootstrap appends to the body -- visible, greyed out, and unclickable.
     */
    readonly formHost = input.required<ViewContainerRef>();

    readonly rows = signal<any[]>([]);
    readonly loading = signal(false);
    readonly openingPayslipId = signal<number | null>(null);

    readonly context: WorkbenchCellContext = {
        l: (key) => this.l(key),
        isArabic: abp.localization.currentLanguage.name?.startsWith('ar') ?? false,
    };

    readonly canCreate = computed(() => {
        const view = this.subView();
        return (
            !!view.form &&
            !!view.createPermission &&
            this.permission.isGranted(view.createPermission) &&
            (!this.isExEmployee() || !!view.allowCreateForExEmployee)
        );
    });

    readonly canEdit = computed(() => {
        const view = this.subView();
        return !!view.form && !!view.editPermission && this.permission.isGranted(view.editPermission);
    });

    readonly hasRowButtons = computed(() => this.canEdit() || this.subView().rowAction === 'payslipPdf');

    private _load: Subscription | undefined;
    private _form: ComponentRef<WorkbenchForm> | undefined;
    private _formType: Type<WorkbenchForm> | undefined;

    constructor() {
        super();

        effect(() => {
            this.subView();
            this.employeeId();
            untracked(() => this.reload());
        });

        inject(DestroyRef).onDestroy(() => {
            this._load?.unsubscribe();
            this._form?.destroy();
        });
    }

    reload(): void {
        this._load?.unsubscribe();
        this.loading.set(true);
        this._load = this.subView()
            .load(this._injector, this.employeeId())
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe((rows) => this.rows.set(rows ?? []));
    }

    async openForm(id?: number): Promise<void> {
        const type = await this.subView().form();

        if (this._formType !== type) {
            this._form?.destroy();
            this._form = this.formHost().createComponent(type);
            this._formType = type;
            this._form.instance.modalSave.subscribe(() => this.reload());

            // The form reaches its bsModal through a view query, which exists only once it has rendered.
            this._form.changeDetectorRef.detectChanges();
        }

        this._form.instance.show(this.employeeId(), id);
    }

    cell(row: any, column: WorkbenchColumn): unknown {
        return column.value ? column.value(row, this.context) : row[column.field];
    }

    isBlankDate(value: unknown): boolean {
        return !(value instanceof DateTime) || value.year <= BLANK_DATE_YEAR_CEILING;
    }

    tone(row: any): string {
        const tone = this.subView().tone?.(row);
        return tone ? 'table-' + tone : '';
    }

    /**
     * The window is opened on the click itself and pointed at the PDF once it arrives: a window opened
     * after an await is a popup as far as the browser is concerned, and would be blocked.
     */
    openPayslip(row: SalaryRegisterDto): void {
        if (this.openingPayslipId() !== null) {
            return;
        }

        const target = window.open('', '_blank');
        const input = new GetWorkbenchPayslipPdfInput({
            employeeId: this.employeeId(),
            month: row.inPeriod.month,
            year: row.inPeriod.year,
            payrollRunType: row.payrollRunType,
        });

        this.openingPayslipId.set(row.id);
        this._payslips
            .getPayslipPdf(input)
            .pipe(finalize(() => this.openingPayslipId.set(null)))
            .subscribe({
                next: (pdf) => {
                    const url = URL.createObjectURL(this.pdfBlob(pdf.base64Content ?? ''));
                    if (target) {
                        target.location.href = url;
                    } else {
                        this.download(url, pdf.fileName ?? 'Payslip.pdf');
                    }
                },
                error: () => target?.close(),
            });
    }

    private pdfBlob(base64: string): Blob {
        const bytes = Uint8Array.from(atob(base64), (c) => c.charCodeAt(0));
        return new Blob([bytes], { type: 'application/pdf' });
    }

    private download(url: string, fileName: string): void {
        const link = document.createElement('a');
        link.href = url;
        link.download = fileName;
        link.click();
    }
}
