/**
 * Clipboard TSV parsing/serialisation for the paste grid.
 *
 * Excel puts a field in double quotes when it contains a tab, a newline or a quote, and doubles any
 * embedded quote. A naive `split('\t')` therefore corrupts any multi-line cell (a pasted address, a
 * comment), which is why this is a character scanner rather than a pair of splits.
 *
 * Pure functions with no Angular dependency -- see tsv.spec.ts.
 */

/** Parse clipboard text into rows of raw string cells. Ragged rows are returned as-is. */
export function parseTsv(text: string): string[][] {
    const rows: string[][] = [];
    let row: string[] = [];
    let field = '';
    let inQuotes = false;
    let i = 0;

    while (i < text.length) {
        const ch = text[i];

        if (inQuotes) {
            if (ch === '"') {
                if (text[i + 1] === '"') {
                    field += '"';
                    i += 2;
                    continue;
                }
                inQuotes = false;
                i++;
                continue;
            }
            field += ch;
            i++;
            continue;
        }

        // A quote only opens a quoted field at the start of that field; a stray quote mid-field is data.
        if (ch === '"' && field === '') {
            inQuotes = true;
            i++;
            continue;
        }
        if (ch === '\t') {
            row.push(field);
            field = '';
            i++;
            continue;
        }
        if (ch === '\r' || ch === '\n') {
            if (ch === '\r' && text[i + 1] === '\n') {
                i++;
            }
            row.push(field);
            rows.push(row);
            row = [];
            field = '';
            i++;
            continue;
        }

        field += ch;
        i++;
    }

    row.push(field);
    rows.push(row);

    // Excel terminates a copied range with a newline, which leaves one phantom empty row.
    if (rows.length > 1) {
        const last = rows[rows.length - 1];
        if (last.length === 1 && last[0] === '') {
            rows.pop();
        }
    }

    return rows;
}

/** Serialise a rectangle back to clipboard text that Excel will accept. */
export function toTsv(rows: string[][]): string {
    return rows.map((r) => r.map(escapeField).join('\t')).join('\r\n');
}

function escapeField(value: string): string {
    if (value === null || value === undefined) {
        return '';
    }
    return /[\t\r\n"]/.test(value) ? `"${value.replace(/"/g, '""')}"` : value;
}
