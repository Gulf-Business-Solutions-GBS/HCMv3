import { GoLiveStage, GoLiveStep } from './go-live.types';
import { AnswerValue, GoLiveQuestion } from './question-card/go-live-question.types';
import { PasteGridColumn } from './paste-grid/paste-grid.types';

/**
 * Every stage and step of the Go-Live Setup, in dependency order.
 *
 * This is the one place a step is declared. The navigation rail, the readiness board and the step
 * router all read from here, so adding a step is a single edit.
 *
 * In production this registry comes from the server-side go-live template definition -- the same
 * definition that generates the offline workbook -- so the questions cannot drift between the two. It
 * is hard-coded here while that definition is being built.
 */

// ---------------------------------------------------------------- lookups
//
// Choice columns and questions name a tenant lookup through `optionsFrom` rather than carrying a
// list. The values come from what the tenant actually has -- nationalities and banks from the
// shipped reference packs, job titles and work locations from what the customer created in step S2
// -- so the dropdown can never offer something the rest of the system does not know about.
//
// Departments are deliberately absent: they are an OrganizationUnit tree rather than a flat lookup,
// and until S2.1 is built there is nothing to read. That column stays free text.

// ---------------------------------------------------------------- S1.2 worked example
// August 2026: 31 days, weekly offs Friday and Saturday, joiner starts on the 16th.

const SAMPLE_YEAR = 2026;
const SAMPLE_MONTH = 7;
const SAMPLE_JOIN_DAY = 16;
const SAMPLE_SALARY = 650;

/**
 * Recomputes the mid-month joiner example for the selected proration rule. Working days are counted
 * against the real calendar rather than assumed, so the figure holds up if anyone checks it.
 */
export function prorationExample(value: AnswerValue): string | null {
    if (!value) {
        return null;
    }

    const daysInMonth = new Date(Date.UTC(SAMPLE_YEAR, SAMPLE_MONTH + 1, 0)).getUTCDate();
    const daysPaid = daysInMonth - SAMPLE_JOIN_DAY + 1;

    let workingDaysInMonth = 0;
    let workingDaysPaid = 0;
    for (let d = 1; d <= daysInMonth; d++) {
        const dow = new Date(Date.UTC(SAMPLE_YEAR, SAMPLE_MONTH, d)).getUTCDay();
        if (dow === 5 || dow === 6) {
            continue;
        }
        workingDaysInMonth++;
        if (d >= SAMPLE_JOIN_DAY) {
            workingDaysPaid++;
        }
    }

    const salary = SAMPLE_SALARY.toFixed(3);
    let amount: number;
    let basis: string;

    switch (value) {
        case 'Fixed30':
            amount = (SAMPLE_SALARY / 30) * daysPaid;
            basis = `${daysPaid} of 30 days`;
            break;
        case 'ActualDays':
            amount = (SAMPLE_SALARY / daysInMonth) * daysPaid;
            basis = `${daysPaid} of ${daysInMonth} days`;
            break;
        case 'WorkingDays':
            amount = (SAMPLE_SALARY / workingDaysInMonth) * workingDaysPaid;
            basis = `${workingDaysPaid} of ${workingDaysInMonth} working days`;
            break;
        case 'NoProration':
            return `Someone joining on 16 August 2026 on a KD ${salary} salary would receive the full KD ${salary} for August.`;
        default:
            return null;
    }

    return `Someone joining on 16 August 2026 on a KD ${salary} salary would receive KD ${amount.toFixed(3)} for August (${basis}).`;
}

// ---------------------------------------------------------------- questions

const S1_1_COMPANY: GoLiveQuestion[] = [
    {
        ref: 'Q1.01',
        question: 'What is your company name, exactly as it should appear on payslips and reports?',
        control: 'text',
        example: 'Spa 180 Company W.L.L.',
        phase: 'first-payroll',
    },
    {
        ref: 'Q1.02',
        question: 'And the same name in Arabic?',
        control: 'text',
        example: 'شركة سبا ١٨٠',
        phase: 'first-payroll',
    },
    {
        ref: 'Q1.03',
        question: 'From which month should HCMv3 start running your payroll?',
        help: 'This is your go-live month. Every balance you enter later is treated as the position on the 1st of this month.',
        control: 'text',
        example: '2026-08',
        phase: 'first-payroll',
    },
    {
        ref: 'Q1.04',
        question: 'Which currency do you pay salaries in?',
        control: 'choice',
        phase: 'first-payroll',
        optionsFrom: 'Currency',
    },
    {
        ref: 'Q1.05',
        question: 'How many employees will be in that first payroll?',
        help: 'This is checked against the rows you enter on step S3.1.',
        control: 'number',
        unit: 'employees',
        example: '170',
        min: 1,
        phase: 'first-payroll',
    },
    {
        ref: 'Q1.06',
        question: 'Which days are your weekly days off?',
        control: 'choice',
        phase: 'first-payroll',
        options: [
            { value: 'FriSat', label: 'Friday and Saturday' },
            { value: 'Fri', label: 'Friday only' },
            { value: 'SatSun', label: 'Saturday and Sunday' },
            { value: 'Other', label: 'Something else - described in the notes' },
        ],
    },
];

const S1_2_PAY_RULES: GoLiveQuestion[] = [
    {
        ref: 'Q2.01',
        question: 'How often do you pay salaries?',
        control: 'choice',
        phase: 'first-payroll',
        options: [
            { value: 'Monthly', label: 'Once a month' },
            { value: 'SemiMonthly', label: 'Twice a month' },
            { value: 'Fortnightly', label: 'Every two weeks' },
            { value: 'Weekly', label: 'Weekly' },
        ],
    },
    {
        ref: 'Q2.02',
        question: 'On which day of the month do salaries reach employees?',
        control: 'number',
        unit: 'day of the month',
        example: '28',
        min: 1,
        max: 31,
        phase: 'first-payroll',
    },
    {
        ref: 'Q2.03',
        question: 'What is your payroll cut-off day?',
        help: 'Changes made after this day of the month go into the next payroll instead.',
        control: 'number',
        unit: 'day of the month',
        example: '25',
        min: 1,
        max: 31,
        phase: 'first-payroll',
    },
    {
        ref: 'Q2.04',
        question:
            'When someone joins, leaves or is on unpaid leave part way through a month, how do you work out their part-month pay?',
        help: 'Pick the rule your finance team uses today. If different rules apply to different staff, say so in the notes.',
        control: 'choice',
        phase: 'first-payroll',
        options: [
            { value: 'Fixed30', label: 'Always divide the monthly salary by 30 days' },
            { value: 'ActualDays', label: 'Divide by the actual number of days in that month' },
            { value: 'WorkingDays', label: 'Divide by the number of working days in that month' },
            { value: 'NoProration', label: 'Never prorate - they get the full monthly amount' },
        ],
        preview: (value) => prorationExample(value),
    },
    {
        ref: 'Q2.05',
        question: 'To how many decimal places should salary amounts be rounded?',
        control: 'choice',
        phase: 'first-payroll',
        options: [
            { value: '2', label: '2 decimal places' },
            { value: '3', label: '3 decimal places', hint: 'Kuwaiti Dinar is normally 3.' },
        ],
    },
    {
        ref: 'Q2.06',
        question: 'How are salaries paid?',
        control: 'choice',
        phase: 'first-payroll',
        options: [
            { value: 'BankOnly', label: 'Bank transfer only' },
            { value: 'Mixed', label: 'Mostly bank transfer, some cash' },
            { value: 'CashOnly', label: 'Cash only' },
        ],
    },
    {
        ref: 'Q2.08',
        question: 'Does that bank need the transfer file in a particular format?',
        help: 'If yes, please send us one real file the bank has already accepted.',
        control: 'choice',
        phase: 'first-payroll',
        options: [
            { value: 'Sample', label: 'Yes - we will attach a sample file' },
            { value: 'Standard', label: 'No - a standard file is fine' },
            { value: 'Unsure', label: 'Not sure - please advise' },
        ],
        // Nobody paid only in cash should ever see a bank-file question.
        showWhen: (answers) => answers['Q2.06'] !== 'CashOnly',
    },
    {
        ref: 'Q2.09',
        question: 'If deductions add up to more than the salary, what should happen?',
        control: 'choice',
        phase: 'first-payroll',
        options: [
            { value: 'CarryForward', label: 'Pay them zero and carry the balance to next month' },
            { value: 'Block', label: 'Stop the payroll so we can fix it' },
            { value: 'AllowNegative', label: 'Allow the payslip to show a negative amount' },
        ],
    },
    {
        ref: 'Q2.10',
        question: 'Do you need payroll costs posted into your accounting system?',
        help: 'If yes we will need your chart of accounts and a sample journal.',
        control: 'choice',
        phase: 'later',
        options: [
            { value: 'AtGoLive', label: 'Yes - at go-live' },
            { value: 'Later', label: 'Yes - but later' },
            { value: 'No', label: 'No' },
        ],
    },
];

const S1_4_WORKING_TIME: GoLiveQuestion[] = [
    {
        ref: 'Q4.01',
        question: 'How do you record whether someone came to work?',
        help: 'The first option is the fastest route to a correct first payroll. Attendance can be switched on later without redoing anything.',
        control: 'choice',
        phase: 'first-payroll',
        options: [
            {
                value: 'None',
                label: 'We do not track daily attendance',
                hint: 'Everyone is assumed present unless on leave.',
            },
            { value: 'Device', label: 'Staff clock in and out on a fingerprint or face device' },
            { value: 'Mobile', label: 'Staff clock in and out on a mobile app' },
            { value: 'Timesheet', label: 'Staff or managers submit timesheets' },
        ],
    },
    {
        ref: 'Q4.02',
        question: 'What make and model is the device, and how many do you have?',
        control: 'text',
        example: 'ZKTeco MB460, 3 devices',
        phase: 'later',
        showWhen: (answers) => answers['Q4.01'] === 'Device',
    },
    {
        ref: 'Q4.03',
        question: 'What are your normal working hours?',
        control: 'text',
        example: '08:00 to 16:00',
        phase: 'first-payroll',
    },
    {
        ref: 'Q4.07',
        question: 'How many minutes late can someone arrive before it counts as late?',
        control: 'number',
        unit: 'minutes',
        example: '10',
        min: 0,
        phase: 'later',
        showWhen: (answers) => answers['Q4.01'] !== undefined && answers['Q4.01'] !== 'None',
    },
    {
        ref: 'Q4.09',
        question: 'Do you pay overtime?',
        control: 'yesno',
        phase: 'first-payroll',
    },
    {
        ref: 'Q4.10',
        question: 'At what rate is overtime paid?',
        control: 'text',
        example: 'Normal day 1.25x, weekly off 1.5x, public holiday 2x',
        phase: 'first-payroll',
        showWhen: (answers) => answers['Q4.09'] === 'Yes',
    },
];

const S1_5_LEAVE: GoLiveQuestion[] = [
    {
        ref: 'Q5.01',
        question: 'How many days of paid annual leave does an employee get per year?',
        control: 'number',
        unit: 'days',
        example: '30',
        min: 0,
        phase: 'first-payroll',
    },
    {
        ref: 'Q5.03',
        question: 'When does the annual leave entitlement reset?',
        control: 'choice',
        phase: 'first-payroll',
        options: [
            { value: 'CalendarYear', label: 'On 1 January each year' },
            { value: 'FiscalYear', label: 'At the start of our financial year' },
            { value: 'WorkYear', label: 'On the work anniversary of each employee' },
            { value: 'NoAccrual', label: 'It never resets - it just keeps building up' },
        ],
    },
    {
        ref: 'Q5.04',
        question: 'Are weekly off days counted as leave days when they fall inside a leave period?',
        control: 'choice',
        phase: 'first-payroll',
        options: [
            { value: 'No', label: 'No - only working days are counted' },
            { value: 'Yes', label: 'Yes - all days in the period are counted' },
        ],
    },
    {
        ref: 'Q5.06',
        question: 'Can unused annual leave be carried into the next year?',
        control: 'choice',
        phase: 'first-payroll',
        options: [
            { value: 'Unlimited', label: 'Yes - with no limit' },
            { value: 'Capped', label: 'Yes - up to a maximum number of days' },
            { value: 'No', label: 'No - unused leave is lost' },
        ],
    },
    {
        ref: 'Q5.07',
        question: 'How many days can be carried?',
        control: 'number',
        unit: 'days',
        example: '60',
        min: 0,
        phase: 'first-payroll',
        showWhen: (answers) => answers['Q5.06'] === 'Capped',
    },
    {
        ref: 'Q5.16',
        question: 'Will you give us the leave balance for each employee as at go-live?',
        help: 'Getting this right matters: it is what employees will check first.',
        control: 'choice',
        phase: 'first-payroll',
        options: [
            { value: 'Provide', label: 'Yes - we will enter them on step S3.4' },
            { value: 'Zero', label: 'No - please start everyone at zero' },
            { value: 'Flat', label: 'No - please give everyone the same starting balance' },
        ],
    },
];

const S1_6_END_OF_SERVICE: GoLiveQuestion[] = [
    {
        ref: 'Q6.01',
        question: 'Do you pay end-of-service indemnity (gratuity)?',
        control: 'yesno',
        phase: 'first-payroll',
    },
    {
        ref: 'Q6.02',
        question: 'How many days per year for the first five years of service?',
        control: 'number',
        unit: 'days',
        example: '15',
        min: 0,
        phase: 'first-payroll',
        showWhen: (answers) => answers['Q6.01'] === 'Yes',
    },
    {
        ref: 'Q6.03',
        question: 'How many days per year after five years of service?',
        control: 'number',
        unit: 'days',
        example: '26',
        min: 0,
        phase: 'first-payroll',
        showWhen: (answers) => answers['Q6.01'] === 'Yes',
    },
    {
        ref: 'Q6.04',
        question: 'Which part of the salary is indemnity calculated on?',
        control: 'choice',
        phase: 'first-payroll',
        showWhen: (answers) => answers['Q6.01'] === 'Yes',
        options: [
            { value: 'BasicOnly', label: 'Basic salary only' },
            { value: 'BasicHousing', label: 'Basic salary plus housing allowance' },
            { value: 'FullGross', label: 'The full gross salary' },
            { value: 'Other', label: 'Some other combination - described in the notes' },
        ],
    },
    {
        ref: 'Q6.09',
        question: 'Do you deduct staff loans or salary advances through payroll?',
        control: 'yesno',
        phase: 'first-payroll',
    },
    {
        ref: 'Q6.11',
        question: 'Do you deduct social security or a pension contribution?',
        control: 'choice',
        phase: 'first-payroll',
        options: [
            { value: 'CitizensOnly', label: 'Yes - for citizens only' },
            { value: 'All', label: 'Yes - for all employees' },
            { value: 'No', label: 'No' },
        ],
    },
];

// ---------------------------------------------------------------- data sheets

const EMPLOYEE_COLUMNS: PasteGridColumn[] = [
    { key: 'code', header: 'Employee code', width: 120, required: true, hint: 'Their payroll number. Must be unique.' },
    { key: 'nameEn', header: 'Full name (English)', width: 210, required: true },
    { key: 'nameAr', header: 'Full name (Arabic)', width: 190, required: true },
    { key: 'gender', header: 'Gender', type: 'choice', width: 100, required: true, options: ['Male', 'Female'] },
    { key: 'birthDate', header: 'Date of birth', type: 'date', width: 125, required: true, hint: 'YYYY-MM-DD' },
    { key: 'nationality', header: 'Nationality', width: 130, required: true, optionsFrom: 'Nationality' },
    {
        key: 'civilId',
        header: 'Civil ID',
        width: 140,
        required: true,
        hint: 'Twelve digits, no spaces.',
        validate: (value) => (/^\d{12}$/.test(String(value)) ? null : { key: 'PasteGridDemoBadCivilId' }),
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
        validate: (value, row) => {
            const birth = row['birthDate'];
            if (typeof birth !== 'string' || typeof value !== 'string') {
                return null;
            }
            return value <= birth ? { key: 'PasteGridDemoJoinBeforeBirth' } : null;
        },
    },
    // Free text until S2.1 builds the department tree; see the note above.
    { key: 'department', header: 'Department', width: 155, required: true },
    { key: 'jobTitle', header: 'Job title', width: 155, required: true, optionsFrom: 'JobTitle' },
    { key: 'workLocation', header: 'Work location', width: 155, optionsFrom: 'WorkLocation' },
    {
        key: 'active',
        header: 'Still working with you?',
        type: 'choice',
        width: 150,
        required: true,
        options: ['Yes', 'No'],
    },
];

const SALARY_COLUMNS: PasteGridColumn[] = [
    { key: 'code', header: 'Employee code', width: 120, required: true, hint: 'Must match step S3.1.' },
    { key: 'name', header: 'Employee name', width: 210, hint: 'For your own checking - we match on the code.' },
    { key: 'basic', header: 'Basic Salary', type: 'number', width: 130, required: true, min: 0 },
    { key: 'housing', header: 'Housing Allowance', type: 'number', width: 150, min: 0 },
    { key: 'transport', header: 'Transport Allowance', type: 'number', width: 155, min: 0 },
    { key: 'mobileAllowance', header: 'Mobile Allowance', type: 'number', width: 145, min: 0 },
    { key: 'other1', header: 'Other Allowance 1', type: 'number', width: 150, min: 0 },
];

const BANK_COLUMNS: PasteGridColumn[] = [
    { key: 'code', header: 'Employee code', width: 120, required: true, hint: 'Must match step S3.1.' },
    { key: 'name', header: 'Employee name', width: 210 },
    {
        key: 'paidBy',
        header: 'Paid by',
        type: 'choice',
        width: 130,
        required: true,
        options: ['Bank transfer', 'Cash'],
    },
    { key: 'bank', header: 'Bank name', width: 200, optionsFrom: 'Bank' },
    {
        key: 'iban',
        header: 'IBAN',
        width: 260,
        hint: 'Copy it from the bank letter rather than retyping.',
        validate: (value) => (/^[A-Z]{2}\d{2}[A-Z0-9]{10,26}$/.test(String(value)) ? null : { key: 'GoLiveBadIban' }),
    },
];

const BALANCE_COLUMNS: PasteGridColumn[] = [
    { key: 'code', header: 'Employee code', width: 120, required: true, hint: 'Must match step S3.1.' },
    { key: 'name', header: 'Employee name', width: 210 },
    { key: 'leaveDays', header: 'Annual leave balance (days)', type: 'number', width: 190 },
    { key: 'loanBalance', header: 'Outstanding loan balance', type: 'number', width: 185, min: 0 },
    { key: 'loanInstalment', header: 'Monthly loan instalment', type: 'number', width: 180, min: 0 },
];

// ---------------------------------------------------------------- the registry

export const GO_LIVE_STAGES: GoLiveStage[] = [
    {
        id: 'S1',
        nameKey: 'GoLiveStageDecisions',
        steps: [
            {
                id: 'S1.1',
                path: 'company',
                nameKey: 'GoLiveStepCompany',
                introKey: 'GoLiveStepCompanyIntro',
                kind: 'questions',
                phase: 'first-payroll',
                effortKey: 'GoLiveEffort5Min',
                questions: S1_1_COMPANY,
            },
            {
                id: 'S1.2',
                path: 'pay-rules',
                nameKey: 'GoLiveStepPayRules',
                introKey: 'GoLiveStepPayRulesIntro',
                kind: 'questions',
                phase: 'first-payroll',
                effortKey: 'GoLiveEffort10Min',
                questions: S1_2_PAY_RULES,
            },
            {
                id: 'S1.4',
                path: 'working-time',
                nameKey: 'GoLiveStepWorkingTime',
                introKey: 'GoLiveStepWorkingTimeIntro',
                kind: 'questions',
                phase: 'first-payroll',
                effortKey: 'GoLiveEffort5Min',
                questions: S1_4_WORKING_TIME,
            },
            {
                id: 'S1.5',
                path: 'leave',
                nameKey: 'GoLiveStepLeave',
                introKey: 'GoLiveStepLeaveIntro',
                kind: 'questions',
                phase: 'first-payroll',
                effortKey: 'GoLiveEffort10Min',
                questions: S1_5_LEAVE,
            },
            {
                id: 'S1.6',
                path: 'end-of-service',
                nameKey: 'GoLiveStepEndOfService',
                introKey: 'GoLiveStepEndOfServiceIntro',
                kind: 'questions',
                phase: 'first-payroll',
                effortKey: 'GoLiveEffort5Min',
                questions: S1_6_END_OF_SERVICE,
            },
        ],
    },
    {
        id: 'S3',
        nameKey: 'GoLiveStagePeople',
        steps: [
            {
                id: 'S3.1',
                path: 'employees',
                nameKey: 'GoLiveStepEmployees',
                introKey: 'GoLiveStepEmployeesIntro',
                kind: 'data',
                phase: 'first-payroll',
                columns: EMPLOYEE_COLUMNS,
            },
            {
                id: 'S3.2',
                path: 'salaries',
                nameKey: 'GoLiveStepSalaries',
                introKey: 'GoLiveStepSalariesIntro',
                kind: 'data',
                phase: 'first-payroll',
                columns: SALARY_COLUMNS,
            },
            {
                id: 'S3.3',
                path: 'bank-details',
                nameKey: 'GoLiveStepBankDetails',
                introKey: 'GoLiveStepBankDetailsIntro',
                kind: 'data',
                phase: 'first-payroll',
                columns: BANK_COLUMNS,
            },
            {
                id: 'S3.4',
                path: 'opening-balances',
                nameKey: 'GoLiveStepOpeningBalances',
                introKey: 'GoLiveStepOpeningBalancesIntro',
                kind: 'data',
                phase: 'first-payroll',
                columns: BALANCE_COLUMNS,
            },
        ],
    },
    {
        id: 'S4',
        nameKey: 'GoLiveStageProveIt',
        steps: [
            {
                id: 'S4.1',
                path: 'readiness-check',
                nameKey: 'GoLiveStepReadinessCheck',
                introKey: 'GoLiveStepReadinessCheckIntro',
                kind: 'check',
                phase: 'first-payroll',
            },
            {
                id: 'S4.2',
                path: 'trial-payroll',
                nameKey: 'GoLiveStepTrialPayroll',
                introKey: 'GoLiveStepTrialPayrollIntro',
                kind: 'placeholder',
                phase: 'first-payroll',
                pendingKey: 'GoLiveStepTrialPayrollPending',
            },
        ],
    },
];

export const GO_LIVE_STEPS: GoLiveStep[] = GO_LIVE_STAGES.flatMap((s) => s.steps);

export function findStepByPath(path: string): GoLiveStep | undefined {
    return GO_LIVE_STEPS.find((s) => s.path === path);
}

export function findStepById(id: string): GoLiveStep | undefined {
    return GO_LIVE_STEPS.find((s) => s.id === id);
}

/** Next step in registry order, for the "save and continue" button. */
export function nextStep(path: string): GoLiveStep | undefined {
    const i = GO_LIVE_STEPS.findIndex((s) => s.path === path);
    return i >= 0 ? GO_LIVE_STEPS[i + 1] : undefined;
}

export function previousStep(path: string): GoLiveStep | undefined {
    const i = GO_LIVE_STEPS.findIndex((s) => s.path === path);
    return i > 0 ? GO_LIVE_STEPS[i - 1] : undefined;
}
