/**
 * Turns whatever the clipboard (or the user's keyboard) produced into the value the grid stores, or
 * an issue explaining why it could not.
 *
 * Must be idempotent: the grid re-runs coercion over already-stored values when it revalidates, so
 * coerce(coerce(x)) has to equal coerce(x).
 *
 * Pure -- no Angular, no localisation. Issues are keys, resolved by the component.
 */

import { CellIssue, CellValue, PasteGridColumn, PasteGridRow } from './paste-grid.types';

export interface CoerceResult {
    value: CellValue;
    issue: CellIssue | null;
}

/** Excel's day 0. Serial 1 is 1900-01-01, and the 1900-leap-year bug is baked into the offset. */
const EXCEL_EPOCH_UTC = Date.UTC(1899, 11, 30);
const MS_PER_DAY = 86400000;

/** Arabic-Indic and Eastern Arabic-Indic digits -> ASCII. Kuwait clients paste both. */
export function normalizeDigits(input: string): string {
    return input.replace(/[٠-٩۰-۹]/g, (d) => {
        const code = d.charCodeAt(0);
        const base = code >= 0x06f0 ? 0x06f0 : 0x0660;
        return String(code - base);
    });
}

/** Built from a char code so no raw non-breaking space sits in the source (no-irregular-whitespace). */
const NON_BREAKING_SPACE = new RegExp(String.fromCharCode(0xa0), 'g');

function clean(raw: unknown): string {
    if (raw === null || raw === undefined) {
        return '';
    }
    if (raw instanceof Date) {
        return toIsoDate(raw);
    }
    // Non-breaking space arrives from a surprising number of copy sources.
    return normalizeDigits(String(raw)).replace(NON_BREAKING_SPACE, ' ').trim();
}

function toIsoDate(d: Date): string {
    const y = d.getUTCFullYear();
    const m = `${d.getUTCMonth() + 1}`.padStart(2, '0');
    const day = `${d.getUTCDate()}`.padStart(2, '0');
    return `${y}-${m}-${day}`;
}

function isRealDate(y: number, m: number, d: number): boolean {
    if (m < 1 || m > 12 || d < 1 || d > 31) {
        return false;
    }
    const probe = new Date(Date.UTC(y, m - 1, d));
    return probe.getUTCFullYear() === y && probe.getUTCMonth() === m - 1 && probe.getUTCDate() === d;
}

/**
 * Day-first for the ambiguous slash forms: these clients are Kuwait/UK convention, and 03/04/2026 is
 * 3 April. A US-format file will silently misread, which is why the column hint says YYYY-MM-DD.
 */
function parseDate(text: string): string | null {
    let m = /^(\d{4})[-/.](\d{1,2})[-/.](\d{1,2})$/.exec(text);
    if (m) {
        const [y, mo, d] = [+m[1], +m[2], +m[3]];
        return isRealDate(y, mo, d) ? `${m[1]}-${`${mo}`.padStart(2, '0')}-${`${d}`.padStart(2, '0')}` : null;
    }

    m = /^(\d{1,2})[-/.](\d{1,2})[-/.](\d{4})$/.exec(text);
    if (m) {
        const [d, mo, y] = [+m[1], +m[2], +m[3]];
        return isRealDate(y, mo, d) ? `${y}-${`${mo}`.padStart(2, '0')}-${`${d}`.padStart(2, '0')}` : null;
    }

    // Excel sometimes hands over the raw serial rather than a formatted date.
    if (/^\d{1,6}$/.test(text)) {
        const serial = +text;
        if (serial >= 1 && serial <= 100000) {
            return toIsoDate(new Date(EXCEL_EPOCH_UTC + serial * MS_PER_DAY));
        }
    }

    return null;
}

function parseNumber(text: string): number | null {
    // Thousands separators and stray currency codes are normal in a pasted salary column.
    const stripped = text.replace(/[,\s]/g, '').replace(/^(KWD|KD|SAR|AED|USD)/i, '');
    if (stripped === '' || stripped === '-') {
        return null;
    }
    if (!/^-?\d*\.?\d+$/.test(stripped)) {
        return null;
    }
    const n = Number(stripped);
    return Number.isFinite(n) ? n : null;
}

export function coerce(col: PasteGridColumn, raw: unknown): CoerceResult {
    const type = col.type ?? 'text';
    const text = clean(raw);

    if (text === '') {
        return { value: null, issue: col.required ? { key: 'PasteGridRequired' } : null };
    }

    switch (type) {
        case 'readonly':
            return { value: text, issue: null };

        case 'number': {
            const n = parseNumber(text);
            if (n === null) {
                return { value: text, issue: { key: 'PasteGridNotANumber' } };
            }
            if (col.min !== undefined && n < col.min) {
                return { value: n, issue: { key: 'PasteGridBelowMin', params: [col.min] } };
            }
            if (col.max !== undefined && n > col.max) {
                return { value: n, issue: { key: 'PasteGridAboveMax', params: [col.max] } };
            }
            return { value: n, issue: null };
        }

        case 'date': {
            const iso = parseDate(text);
            return iso === null ? { value: text, issue: { key: 'PasteGridNotADate' } } : { value: iso, issue: null };
        }

        case 'choice': {
            const options = col.options ?? [];
            const needle = text.toLowerCase().replace(/\s+/g, ' ');
            const hit = options.find((o) => o.toLowerCase().replace(/\s+/g, ' ') === needle);
            if (hit) {
                return { value: hit, issue: null };
            }
            const near = closest(needle, options);
            return {
                value: text,
                issue: near ? { key: 'PasteGridNotInListDidYouMean', params: [near] } : { key: 'PasteGridNotInList' },
            };
        }

        default: {
            if (col.maxLength !== undefined && text.length > col.maxLength) {
                return { value: text, issue: { key: 'PasteGridTooLong', params: [col.maxLength] } };
            }
            return { value: text, issue: null };
        }
    }
}

/** Runs coercion then the column's own rule. The custom rule only sees a successfully coerced value. */
export function validateCell(col: PasteGridColumn, raw: unknown, row: PasteGridRow): CoerceResult {
    const result = coerce(col, raw);
    if (result.issue || !col.validate || result.value === null) {
        return result;
    }
    return { value: result.value, issue: col.validate(result.value, row) };
}

/**
 * Cheap nearest-option suggestion. "Egyptain" -> "Egyptian" is the single most common paste error in a
 * nationality column, and naming the intended value turns a dead end into a one-click fix.
 */
function closest(needle: string, options: string[]): string | null {
    let best: string | null = null;
    let bestScore = Infinity;
    const limit = Math.max(2, Math.floor(needle.length / 3));

    for (const option of options) {
        const score = levenshtein(needle, option.toLowerCase());
        if (score < bestScore) {
            bestScore = score;
            best = option;
        }
    }
    return bestScore <= limit ? best : null;
}

function levenshtein(a: string, b: string): number {
    if (a === b) {
        return 0;
    }
    if (Math.abs(a.length - b.length) > 4) {
        return Infinity;
    }

    let prev = Array.from({ length: b.length + 1 }, (_, i) => i);
    for (let i = 1; i <= a.length; i++) {
        const curr = [i];
        for (let j = 1; j <= b.length; j++) {
            curr[j] = Math.min(prev[j] + 1, curr[j - 1] + 1, prev[j - 1] + (a[i - 1] === b[j - 1] ? 0 : 1));
        }
        prev = curr;
    }
    return prev[b.length];
}
