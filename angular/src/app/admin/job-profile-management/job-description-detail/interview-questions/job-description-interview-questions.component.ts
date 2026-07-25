import { Component, effect, inject, input, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    JobDescriptionInterviewQuestionDto,
    JobDescriptionInterviewQuestionsServiceProxy,
    GetAllJobDescriptionInterviewQuestionsInput,
} from '@shared/service-proxies/service-proxies';
import { LazyLoadEvent, MenuItem } from 'primeng/api';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { Table, TableModule } from 'primeng/table';
import { TieredMenu } from 'primeng/tieredmenu';
import { finalize } from 'rxjs/operators';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { PermissionAnyPipe } from '@shared/common/pipes/permission-any.pipe';
import { CreateOrEditJobDescriptionInterviewQuestionModalComponent } from './create-or-edit-job-description-interview-question-modal.component';

@Component({
    selector: 'job-description-interview-questions',
    templateUrl: './job-description-interview-questions.component.html',
    imports: [
        BusyIfDirective,
        TableModule,
        PaginatorModule,
        TieredMenu,
        LocalizePipe,
        PermissionPipe,
        PermissionAnyPipe,
        CreateOrEditJobDescriptionInterviewQuestionModalComponent,
    ],
})
export class JobDescriptionInterviewQuestionsComponent extends AppComponentBase {
    private _service = inject(JobDescriptionInterviewQuestionsServiceProxy);

    readonly jobDescriptionId = input<number>();

    readonly createOrEditModal = viewChild<CreateOrEditJobDescriptionInterviewQuestionModalComponent>('createOrEditModal');
    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly activeMenuItems = signal<MenuItem[]>([]);

    constructor() {
        super();
        effect(() => {
            if (this.jobDescriptionId()) {
                this.reloadPage();
            }
        });
    }

    getRecords(event?: LazyLoadEvent): void {
        if (!this.jobDescriptionId()) {
            return;
        }

        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._service
            .getAll(
                new GetAllJobDescriptionInterviewQuestionsInput({
                    jobDescriptionId: this.jobDescriptionId(),
                    sorting: this.primengTableHelper.getSorting(this.dataTable()),
                    maxResultCount: this.primengTableHelper.getMaxResultCount(this.paginator(), event),
                    skipCount: this.primengTableHelper.getSkipCount(this.paginator(), event),
                })
            )
            .pipe(finalize(() => this.primengTableHelper.hideLoadingIndicator()))
            .subscribe((result) => {
                this.primengTableHelper.totalRecordsCount.set(result.totalCount);
                this.primengTableHelper.records.set(result.items);
                this.primengTableHelper.hideLoadingIndicator();
            });
    }

    reloadPage(): void {
        const rows = this.paginator() ? this.paginator().rows || this.primengTableHelper.defaultRecordsCountPerPage : this.primengTableHelper.defaultRecordsCountPerPage;
        this.getRecords({ first: (this.paginator() ? this.paginator().getPage() : 0) * rows, rows } as LazyLoadEvent);
    }

    createRecord(): void {
        this.createOrEditModal().show(this.jobDescriptionId());
    }

    showMenu(event: Event, record: JobDescriptionInterviewQuestionDto, menu: any): void {
        this.activeMenuItems.set(this.getMenuItems(record));
        menu.toggle(event);
    }

    getMenuItems(record: JobDescriptionInterviewQuestionDto): MenuItem[] {
        const items: MenuItem[] = [];

        if (this.permission.isGranted('Pages.JobProfileManagement.JobDescriptionInterviewQuestions.Edit')) {
            items.push({
                label: this.l('Edit'),
                icon: 'pi pi-pencil',
                command: () => this.createOrEditModal().show(this.jobDescriptionId(), record.id),
            });
        }

        if (this.permission.isGranted('Pages.JobProfileManagement.JobDescriptionInterviewQuestions.Delete')) {
            items.push({
                label: this.l('Delete'),
                icon: 'pi pi-trash',
                command: () => this.deleteRecord(record),
            });
        }

        return items;
    }

    deleteRecord(record: JobDescriptionInterviewQuestionDto): void {
        this.message.confirm(this.l('AreYouSure'), this.l('AreYouSure'), (isConfirmed) => {
            if (isConfirmed) {
                this._service.delete(record.id).subscribe(() => {
                    this.reloadPage();
                    this.notify.success(this.l('SuccessfullyDeleted'));
                });
            }
        });
    }
}
