import { parseTsv, toTsv } from './tsv';
import { coerce, normalizeDigits, validateCell } from './paste-grid.coercion';
import { PasteGridColumn } from './paste-grid.types';

describe('parseTsv', () => {
    it('should split a plain rectangle on tabs and newlines', () => {
        expect(parseTsv('a\tb\nc\td')).toEqual([
            ['a', 'b'],
            ['c', 'd'],
        ]);
    });

    it('should drop the phantom trailing row Excel adds to a copied range', () => {
        expect(parseTsv('a\tb\r\nc\td\r\n')).toEqual([
            ['a', 'b'],
            ['c', 'd'],
        ]);
    });

    it('should keep a tab that sits inside a quoted field', () => {
        expect(parseTsv('"Block 4\tStreet 12"\tSalmiya')).toEqual([['Block 4\tStreet 12', 'Salmiya']]);
    });

    it('should keep a newline that sits inside a quoted field', () => {
        expect(parseTsv('"line one\nline two"\tx')).toEqual([['line one\nline two', 'x']]);
    });

    it('should unescape a doubled quote', () => {
        expect(parseTsv('"he said ""hi"""')).toEqual([['he said "hi"']]);
    });

    it('should treat a quote that is not at the start of a field as data', () => {
        expect(parseTsv('5" pipe\tx')).toEqual([['5" pipe', 'x']]);
    });

    it('should preserve empty cells so column alignment survives', () => {
        expect(parseTsv('a\t\tc')).toEqual([['a', '', 'c']]);
    });

    it('should round-trip through toTsv', () => {
        const grid = [
            ['a', 'b\tc'],
            ['d\ne', 'f"g'],
        ];
        expect(parseTsv(toTsv(grid))).toEqual(grid);
    });
});

describe('normalizeDigits', () => {
    it('should convert Arabic-Indic digits to ASCII', () => {
        expect(normalizeDigits('٢٨٤٠١٢٧٠٣٥٧٩')).toBe('284012703579');
    });

    it('should convert Eastern Arabic-Indic digits to ASCII', () => {
        expect(normalizeDigits('۱۲۳')).toBe('123');
    });
});

describe('coerce', () => {
    const text: PasteGridColumn = { key: 't', header: 'T' };
    const required: PasteGridColumn = { key: 't', header: 'T', required: true };
    const num: PasteGridColumn = { key: 'n', header: 'N', type: 'number' };
    const date: PasteGridColumn = { key: 'd', header: 'D', type: 'date' };
    const choice: PasteGridColumn = {
        key: 'c',
        header: 'C',
        type: 'choice',
        options: ['Kuwaiti', 'Egyptian', 'Sri Lankan'],
    };

    it('should flag an empty required cell', () => {
        expect(coerce(required, '').issue).toEqual({ key: 'PasteGridRequired' });
    });

    it('should allow an empty optional cell', () => {
        expect(coerce(text, '   ')).toEqual({ value: null, issue: null });
    });

    it('should strip thousands separators from a number', () => {
        expect(coerce(num, '1,250.500')).toEqual({ value: 1250.5, issue: null });
    });

    it('should strip a currency prefix from a pasted salary', () => {
        expect(coerce(num, 'KD 650').value).toBe(650);
    });

    it('should reject a non-numeric value', () => {
        expect(coerce(num, 'n/a').issue).toEqual({ key: 'PasteGridNotANumber' });
    });

    it('should enforce min and max', () => {
        const bounded: PasteGridColumn = { key: 'n', header: 'N', type: 'number', min: 0, max: 10 };
        expect(coerce(bounded, '-1').issue).toEqual({ key: 'PasteGridBelowMin', params: [0] });
        expect(coerce(bounded, '11').issue).toEqual({ key: 'PasteGridAboveMax', params: [10] });
    });

    it('should accept an ISO date unchanged', () => {
        expect(coerce(date, '2026-08-01')).toEqual({ value: '2026-08-01', issue: null });
    });

    it('should read a slash date as day-first', () => {
        expect(coerce(date, '03/04/2026').value).toBe('2026-04-03');
    });

    it('should pad a single-digit month and day', () => {
        expect(coerce(date, '2026-4-3').value).toBe('2026-04-03');
    });

    it('should convert an Excel serial to ISO', () => {
        expect(coerce(date, '46235').value).toBe('2026-08-01');
    });

    it('should reject an impossible date', () => {
        expect(coerce(date, '2026-02-30').issue).toEqual({ key: 'PasteGridNotADate' });
    });

    it('should match a choice case-insensitively and store the canonical spelling', () => {
        expect(coerce(choice, 'kuwaiti')).toEqual({ value: 'Kuwaiti', issue: null });
    });

    it('should collapse whitespace when matching a choice', () => {
        expect(coerce(choice, 'Sri   Lankan').value).toBe('Sri Lankan');
    });

    it('should suggest the nearest option for a near miss', () => {
        expect(coerce(choice, 'Egyptain').issue).toEqual({
            key: 'PasteGridNotInListDidYouMean',
            params: ['Egyptian'],
        });
    });

    it('should not invent a suggestion for something unrelated', () => {
        expect(coerce(choice, 'Martian').issue).toEqual({ key: 'PasteGridNotInList' });
    });

    it('should be idempotent, because the grid revalidates stored values', () => {
        for (const [col, raw] of [
            [num, '1,250.5'],
            [date, '03/04/2026'],
            [choice, 'kuwaiti'],
            [text, ' padded '],
        ] as [PasteGridColumn, string][]) {
            const once = coerce(col, raw);
            const twice = coerce(col, once.value);
            expect(twice).toEqual(once);
        }
    });

    it('should normalise Arabic-Indic digits before parsing', () => {
        expect(coerce(num, '١٢٣').value).toBe(123);
    });
});

describe('validateCell', () => {
    const col: PasteGridColumn = {
        key: 'civilId',
        header: 'Civil ID',
        required: true,
        validate: (value) => (/^\d{12}$/.test(String(value)) ? null : { key: 'BadCivilId' }),
    };

    it('should run the column rule after coercion succeeds', () => {
        expect(validateCell(col, '284012703579', {}).issue).toBeNull();
        expect(validateCell(col, '2840127', {}).issue).toEqual({ key: 'BadCivilId' });
    });

    it('should not run the column rule when coercion already failed', () => {
        expect(validateCell(col, '', {}).issue).toEqual({ key: 'PasteGridRequired' });
    });
});
