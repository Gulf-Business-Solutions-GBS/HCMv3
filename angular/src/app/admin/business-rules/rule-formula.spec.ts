import { RuleFormulaError, parseFormula, printFormula } from './rule-formula';
import { RuleExpression } from './rule-model';

/**
 * The formula language is a VIEW of the stored rule tree, so the property that matters most is not "does it
 * parse" but "does it round-trip". If print(parse(x)) drifts, opening a rule in the formula editor and
 * saving it would silently rewrite somebody's rule -- the one failure mode that would make this feature
 * worse than no feature.
 */
describe('parseFormula', () => {
    it('should parse a field compared to a number', () => {
        expect(parseFormula('Employee.ProbationDays <= 0')).toEqual({
            kind: 'compare',
            op: 'LessThanOrEqual',
            left: { kind: 'field', parameter: 'Employee', field: 'ProbationDays' },
            right: { kind: 'const', dataType: 'Integer', value: '0' },
        } as RuleExpression);
    });

    it('should parse a quoted text value', () => {
        const parsed = parseFormula('Applier.JobTitleCode = "CHRO"');

        expect(parsed.right).toEqual({ kind: 'const', dataType: 'String', value: 'CHRO' });
    });

    it('should accept single quotes as well as double', () => {
        expect(parseFormula("Applier.JobTitleCode = 'CHRO'").right).toEqual({
            kind: 'const',
            dataType: 'String',
            value: 'CHRO',
        });
    });

    it('should read a decimal point as a decimal, not an integer', () => {
        expect(parseFormula('Leave.TotalLeaveDays > 1.5').right).toEqual({
            kind: 'const',
            dataType: 'Decimal',
            value: '1.5',
        });
    });

    it('should parse booleans', () => {
        expect(parseFormula('Applier.IsManager = true').right).toEqual({
            kind: 'const',
            dataType: 'Boolean',
            value: 'true',
        });
    });

    /** The date literal exists so Date constants survive a round trip. See rule-formula.ts. */
    it('should parse a date literal as a Date constant', () => {
        expect(parseFormula('Employee.JoiningDate > #2026-01-01#').right).toEqual({
            kind: 'const',
            dataType: 'Date',
            value: '2026-01-01',
        });
    });

    it('should parse a function call with arguments', () => {
        expect(parseFormula('AddCalendarDays(Employee.JoiningDate, 100)')).toEqual({
            kind: 'fn',
            name: 'AddCalendarDays',
            args: [
                { kind: 'field', parameter: 'Employee', field: 'JoiningDate' },
                { kind: 'const', dataType: 'Integer', value: '100' },
            ],
        } as RuleExpression);
    });

    it('should parse a bare identifier as a variable', () => {
        expect(parseFormula('Tenure > 5').left).toEqual({ kind: 'var', name: 'Tenure' });
    });

    it('should flatten a chain of ANDs into one junction', () => {
        const parsed = parseFormula('A.b = 1 AND C.d = 2 AND E.f = 3');

        expect(parsed.kind).toBe('logical');
        expect(parsed.op).toBe('And');
        expect(parsed.operands!.length).toBe(3);
    });

    it('should bind AND tighter than OR', () => {
        const parsed = parseFormula('A.b = 1 OR C.d = 2 AND E.f = 3');

        expect(parsed.op).toBe('Or');
        expect(parsed.operands!.length).toBe(2);
        expect(parsed.operands![1].op).toBe('And');
    });

    it('should let parentheses override precedence', () => {
        const parsed = parseFormula('(A.b = 1 OR C.d = 2) AND E.f = 3');

        expect(parsed.op).toBe('And');
        expect(parsed.operands![0].op).toBe('Or');
    });

    it('should parse NOT', () => {
        const parsed = parseFormula('NOT Applier.JobTitleCode = "CHRO"');

        expect(parsed.kind).toBe('not');
        expect(parsed.operand!.kind).toBe('compare');
    });

    it('should parse IS EMPTY and IS NOT EMPTY', () => {
        expect(parseFormula('Employee.Email IS EMPTY').op).toBe('IsEmpty');
        expect(parseFormula('Employee.Email IS NOT EMPTY').op).toBe('IsNotEmpty');
    });

    it('should parse IN and NOT IN against a list', () => {
        const parsed = parseFormula('Leave.AbsenceTypeId IN (1, 2, 3)');

        expect(parsed.op).toBe('In');
        expect(parsed.right!.kind).toBe('list');
        expect(parsed.right!.items!.length).toBe(3);

        expect(parseFormula('Leave.AbsenceTypeId NOT IN (1)').op).toBe('NotIn');
    });

    it('should parse the text operators', () => {
        expect(parseFormula('Employee.Code CONTAINS "X"').op).toBe('Contains');
        expect(parseFormula('Employee.Code STARTS WITH "X"').op).toBe('StartsWith');
        expect(parseFormula('Employee.Code ENDS WITH "X"').op).toBe('EndsWith');
    });

    it('should bind multiplication tighter than addition', () => {
        const parsed = parseFormula('A.b + C.d * 2');

        expect(parsed.op).toBe('Add');
        expect(parsed.right!.op).toBe('Multiply');
    });

    it('should accept both spellings of equality and inequality', () => {
        expect(parseFormula('A.b = 1').op).toBe('Equal');
        expect(parseFormula('A.b == 1').op).toBe('Equal');
        expect(parseFormula('A.b <> 1').op).toBe('NotEqual');
        expect(parseFormula('A.b != 1').op).toBe('NotEqual');
    });

    it('should report the position of a syntax error so the editor can point at it', () => {
        let error: RuleFormulaError | undefined;

        try {
            parseFormula('Employee.Code = ');
        } catch (e) {
            error = e as RuleFormulaError;
        }

        expect(error).toBeDefined();
        expect(error!.position).toBe(16);
    });

    it('should reject an unclosed quote rather than guessing', () => {
        expect(() => parseFormula('Employee.Code = "abc')).toThrowError(RuleFormulaError);
    });

    it('should reject an empty formula', () => {
        expect(() => parseFormula('   ')).toThrowError(RuleFormulaError);
    });

    it('should reject trailing rubbish instead of silently ignoring it', () => {
        expect(() => parseFormula('A.b = 1 )')).toThrowError(RuleFormulaError);
    });
});

describe('printFormula round trip', () => {
    const formulas = [
        'Employee.ProbationDays <= 0',
        'Applier.JobTitleCode = "CHRO"',
        'Applier.JobTitleCode = "CHRO" AND Applier.IsManager = true',
        'Request.RequestType = "EmployeeAbsence" AND NOT Applier.JobTitleCode = "CHRO" AND Applier.IsManager = true',
        'AddCalendarDays(Employee.JoiningDate, 100)',
        'GetNumberOfCalendarDays(Leave.StartDate, Leave.EndDate) < 7',
        'Employee.Email IS EMPTY',
        'Employee.Email IS NOT EMPTY',
        'Leave.AbsenceTypeId IN (1, 2, 3)',
        'Leave.AbsenceTypeId NOT IN (7)',
        'Employee.Code STARTS WITH "E"',
        'Employee.JoiningDate > #2026-01-01#',
        'A.b + C.d * 2',
        '(A.b OR C.d) AND E.f',
        'Coalesce(A.b, C.d, "x")',
        'Tenure >= 5',
    ];

    formulas.forEach((formula) => {
        it(`should round trip: ${formula}`, () => {
            expect(printFormula(parseFormula(formula))).toBe(formula);
        });
    });

    /** Parentheses come from precedence, so redundant ones are dropped -- the meaning must not change. */
    it('should drop redundant parentheses without changing the tree', () => {
        const withParens = parseFormula('(A.b = 1) AND (C.d = 2)');
        const without = parseFormula('A.b = 1 AND C.d = 2');

        expect(printFormula(withParens)).toBe('A.b = 1 AND C.d = 2');
        expect(withParens).toEqual(without);
    });

    it('should add the parentheses precedence requires', () => {
        expect(printFormula(parseFormula('(A.b + C.d) * 2'))).toBe('(A.b + C.d) * 2');
    });

    it('should escape a quote inside a text value', () => {
        const parsed = parseFormula('A.b = "say \\"hi\\""');

        expect(parsed.right!.value).toBe('say "hi"');
        expect(printFormula(parsed)).toBe('A.b = "say \\"hi\\""');
    });

    it('should print nothing for a missing expression', () => {
        expect(printFormula(undefined)).toBe('');
        expect(printFormula(null)).toBe('');
    });
});
