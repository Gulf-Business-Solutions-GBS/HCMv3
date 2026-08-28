import { Component, signal } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { SubHeaderComponent } from '../../shared/common/sub-header/sub-header.component';
import { PasteGridComponent } from '../go-live-setup/shared/paste-grid/paste-grid.component';
import {
    CellIssue,
    CellValue,
    PasteGridColumn,
    PasteGridIssue,
    PasteGridRow,
} from '../go-live-setup/shared/paste-grid/paste-grid.types';

/**
 * Harness for the Go-Live Setup paste grid, using the real S3.1 Employees column set.
 *
 * Lives in the demo gallery rather than in the Go-Live Setup folder so it can be exercised before that
 * feature's shell, routes and permissions exist. The component under test lives in its final home.
 *
 * The dropdown sources below stand in for what step S2 will have created by the time a customer
 * reaches this sheet (departments, job titles, grades).
 */
@Component({
    templateUrl: './demo-ui-paste-grid.component.html',
    animations: [appModuleAnimation()],
    imports: [SubHeaderComponent, PasteGridComponent, LocalizePipe],
})
export class DemoUiPasteGridComponent extends AppComponentBase {
    private static readonly DEPARTMENTS = [
        'Head Office',
        'Finance',
        'Human Resources',
        'Operations',
        'Spa & Wellness',
        'Front Desk',
        'Maintenance',
    ];

    private static readonly JOB_TITLES = [
        'General Manager',
        'Accountant',
        'HR Officer',
        'Spa Therapist',
        'Receptionist',
        'Technician',
        'Driver',
    ];

    readonly columns: PasteGridColumn[] = [
        {
            key: 'code',
            header: 'Employee code',
            width: 120,
            required: true,
            hint: 'Their payroll number. Must be unique.',
            validate: (value, row) => this.uniqueCode(value, row),
        },
        { key: 'nameEn', header: 'Full name (English)', width: 210, required: true },
        { key: 'nameAr', header: 'Full name (Arabic)', width: 190, required: true },
        { key: 'gender', header: 'Gender', type: 'choice', width: 100, required: true, options: ['Male', 'Female'] },
        {
            key: 'birthDate',
            header: 'Date of birth',
            type: 'date',
            width: 125,
            required: true,
            hint: 'YYYY-MM-DD',
        },
        {
            key: 'nationality',
            header: 'Nationality',
            type: 'choice',
            width: 130,
            required: true,
            options: [
                'Kuwaiti',
                'Egyptian',
                'Indian',
                'Jordanian',
                'Lebanese',
                'Syrian',
                'Philippine',
                'Sri Lankan',
                'Tunisian',
                'Indonesian',
                'Moroccan',
                'Iranian',
            ],
        },
        {
            key: 'civilId',
            header: 'Civil ID',
            width: 140,
            required: true,
            hint: 'Twelve digits, no spaces.',
            validate: (value) => this.civilId(value),
        },
        {
            key: 'mobile',
            header: 'Mobile',
            width: 115,
            required: true,
            hint: 'Digits only, no plus sign.',
            validate: (value) => (/^\d{7,12}$/.test(String(value)) ? null : { key: 'PasteGridDemoBadMobile' }),
        },
        {
            key: 'email',
            header: 'Email',
            width: 200,
            hint: 'Only needed if they will sign in.',
            validate: (value) => (/^\S+@\S+\.\S+$/.test(String(value)) ? null : { key: 'PasteGridDemoBadEmail' }),
        },
        {
            key: 'joiningDate',
            header: 'Joining date',
            type: 'date',
            width: 125,
            required: true,
            hint: 'Drives leave and indemnity, so it must be exact.',
            validate: (value, row) => this.joinAfterBirth(value, row),
        },
        {
            key: 'department',
            header: 'Department',
            type: 'choice',
            width: 155,
            required: true,
            options: DemoUiPasteGridComponent.DEPARTMENTS,
        },
        {
            key: 'jobTitle',
            header: 'Job title',
            type: 'choice',
            width: 155,
            required: true,
            options: DemoUiPasteGridComponent.JOB_TITLES,
        },
        {
            key: 'employmentType',
            header: 'Permanent or temporary',
            type: 'choice',
            width: 150,
            options: ['Permanent', 'Temporary'],
        },
        {
            key: 'active',
            header: 'Still working with you?',
            type: 'choice',
            width: 150,
            required: true,
            options: ['Yes', 'No'],
        },
    ];

    readonly rows = signal<PasteGridRow[]>([]);
    readonly issues = signal<PasteGridIssue[]>([]);

    onIssuesChange(issues: PasteGridIssue[]): void {
        this.issues.set(issues);
    }

    /** Sample deliberately contains four faults, to show what a real paste looks like. */
    loadSample(): void {
        this.rows.set([
            this.row(
                '1001',
                'Ahmed Kulaib Mohamed Ali',
                'أحمد كليب محمد علي',
                'Male',
                '1984-01-27',
                'Egyptian',
                '284012703579',
                '99709947',
                'ahmed.ali@example.com',
                '2019-03-01',
                'Finance',
                'Accountant',
                'Permanent',
                'Yes'
            ),
            this.row(
                '1002',
                'Bader Naser Hamad Almutairi',
                'بدر ناصر حمد المطيري',
                'Male',
                '1992-11-24',
                'Kuwaiti',
                '292112401105',
                '50331155',
                'bader.n@example.com',
                '2015-06-15',
                'Head Office',
                'General Manager',
                'Permanent',
                'Yes'
            ),
            // Nationality misspelt -- the grid should suggest "Egyptian".
            this.row(
                '1003',
                'Eslam Mohamed Yhya Elsayed',
                'اسلام محمد يحيى السيد',
                'Male',
                '1990-02-11',
                'Egyptain',
                '290021101234',
                '66554433',
                'eslam@example.com',
                '2021-01-10',
                'Operations',
                'Technician',
                'Permanent',
                'Yes'
            ),
            // Civil ID too short, and a duplicate employee code below.
            this.row(
                '1004',
                'Anwar Humoud Munawer',
                'انوار حمود مناور',
                'Female',
                '1995-09-19',
                'Kuwaiti',
                '2890919',
                '90000377',
                'anwar@example.com',
                '2022-04-01',
                'Human Resources',
                'HR Officer',
                'Permanent',
                'Yes'
            ),
            // Joining date before date of birth.
            this.row(
                '1004',
                'Harold Nacorda Bicada',
                'هارولد ناكوردا بيكادا',
                'Male',
                '1998-07-03',
                'Philippine',
                '298070301111',
                '55667788',
                'harold@example.com',
                '1995-01-01',
                'Spa & Wellness',
                'Spa Therapist',
                'Temporary',
                'Yes'
            ),
        ]);
    }

    private row(...values: string[]): PasteGridRow {
        const keys = [
            'code',
            'nameEn',
            'nameAr',
            'gender',
            'birthDate',
            'nationality',
            'civilId',
            'mobile',
            'email',
            'joiningDate',
            'department',
            'jobTitle',
            'employmentType',
            'active',
        ];
        const row: PasteGridRow = {};
        keys.forEach((k, i) => (row[k] = values[i] ?? null));
        return row;
    }

    // ---- column rules -------------------------------------------------------

    private uniqueCode(value: CellValue, row: PasteGridRow): CellIssue | null {
        const code = String(value);
        let seen = 0;
        for (const other of this.rows()) {
            if (String(other['code'] ?? '') === code) {
                seen++;
            }
        }
        return seen > 1 && row !== undefined ? { key: 'PasteGridDemoDuplicateCode' } : null;
    }

    private civilId(value: CellValue): CellIssue | null {
        return /^\d{12}$/.test(String(value)) ? null : { key: 'PasteGridDemoBadCivilId' };
    }

    private joinAfterBirth(value: CellValue, row: PasteGridRow): CellIssue | null {
        const birth = row['birthDate'];
        if (typeof birth !== 'string' || typeof value !== 'string') {
            return null;
        }
        return value <= birth ? { key: 'PasteGridDemoJoinBeforeBirth' } : null;
    }
}
