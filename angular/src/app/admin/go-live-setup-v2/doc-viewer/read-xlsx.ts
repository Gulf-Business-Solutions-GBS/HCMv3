import { unzipSync, strFromU8 } from 'fflate';

/** One sheet of a workbook: its name, and its cells as text, the first row being the headings. */
export interface XlsxSheet {
    name: string;
    rows: string[][];
}

/**
 * Reads back a workbook this application generated.
 *
 * <p>Written rather than taken off the shelf because the obvious library could not read our own
 * files: exceljs throws on both the ClosedXML workbooks the bank file generator writes and the
 * MiniExcel ones the grid reports write. What is needed here is far less than a spreadsheet library
 * anyway -- the cells, as they would be read, to show before the file is saved. Formatting, formulas
 * and dates as numbers are the spreadsheet's business, not the preview's.</p>
 *
 * <p>An .xlsx is a zip of XML: a workbook naming the sheets, a relationships file saying which part
 * each sheet lives in, one part per sheet, and a shared table for repeated strings.</p>
 */
export function readXlsx(bytes: Uint8Array): XlsxSheet[] {
    const files = unzipSync(bytes);
    const text = (path: string): string | null => (files[path] ? strFromU8(files[path]) : null);

    const sharedStrings = readSharedStrings(text('xl/sharedStrings.xml'));
    const targets = readRelationships(text('xl/_rels/workbook.xml.rels'));
    const dateStyles = readDateStyles(text('xl/styles.xml'));

    return readSheetList(text('xl/workbook.xml'), targets, files).map(({ name, path }) => ({
        name,
        rows: readSheet(text(path), sharedStrings, dateStyles),
    }));
}

/**
 * Which cell styles mean "this number is a date". A spreadsheet keeps a date as a count of days, and
 * only its format says so -- without this a pay period reads as 46295.
 */
function readDateStyles(stylesXml: string | null): Set<number> {
    if (!stylesXml) {
        return new Set();
    }

    // Formats the file defines itself are dates when their pattern mentions a day, month or year.
    const custom = new Set<number>();
    for (const tag of stylesXml.match(/<(?:\w+:)?numFmt\b[^>]*\/?>/g) ?? []) {
        const id = Number(attribute(tag, 'numFmtId'));
        const code = attribute(tag, 'formatCode') ?? '';
        if (!Number.isNaN(id) && /[dmy]/i.test(code.replace(/\[[^\]]*\]|"[^"]*"/g, ''))) {
            custom.add(id);
        }
    }

    // The rest are the built-in date and time formats.
    const builtIn = (id: number) => (id >= 14 && id <= 22) || (id >= 27 && id <= 36) || (id >= 45 && id <= 58);

    const styles = new Set<number>();
    const cellXfs = stylesXml.match(/<(?:\w+:)?cellXfs\b[\s\S]*?<\/(?:\w+:)?cellXfs>/)?.[0] ?? '';
    (cellXfs.match(/<(?:\w+:)?xf\b[^>]*\/?>/g) ?? []).forEach((xf, index) => {
        const id = Number(attribute(xf, 'numFmtId'));
        if (!Number.isNaN(id) && (builtIn(id) || custom.has(id))) {
            styles.add(index);
        }
    });

    return styles;
}

/** A spreadsheet counts days from 1900, and gets 1900 wrong on purpose; day 60 is its phantom leap day. */
function dateText(serial: number): string {
    const days = serial > 59 ? serial - 1 : serial;
    const date = new Date(Date.UTC(1900, 0, 1) + (days - 1) * 86400000);
    const pad = (n: number) => String(n).padStart(2, '0');
    return pad(date.getUTCDate()) + '-' + pad(date.getUTCMonth() + 1) + '-' + date.getUTCFullYear();
}

/** The names of the sheets, in the workbook's own order, each with the part that holds it. */
function readSheetList(
    workbookXml: string | null,
    targets: Record<string, string>,
    files: Record<string, Uint8Array>
): { name: string; path: string }[] {
    const sheets: { name: string; path: string }[] = [];

    for (const tag of workbookXml?.match(/<(?:\w+:)?sheet\b[^>]*\/?>/g) ?? []) {
        const name = attribute(tag, 'name') ?? 'Sheet';
        const relationship = attribute(tag, 'r:id') ?? attribute(tag, 'id');
        const target = relationship ? targets[relationship] : null;
        const path = target ? normalise(target) : null;
        if (path && files[path]) {
            sheets.push({ name, path });
        }
    }

    // A workbook whose relationships we could not follow still has its sheets where they always are.
    if (sheets.length === 0) {
        for (const path of Object.keys(files).filter((f) => /^xl\/worksheets\/sheet\d+\.xml$/.test(f)).sort()) {
            sheets.push({ name: path.replace(/^xl\/worksheets\/|\.xml$/g, ''), path });
        }
    }

    return sheets;
}

function readRelationships(relsXml: string | null): Record<string, string> {
    const targets: Record<string, string> = {};

    for (const tag of relsXml?.match(/<(?:\w+:)?Relationship\b[^>]*\/?>/g) ?? []) {
        const id = attribute(tag, 'Id');
        const target = attribute(tag, 'Target');
        if (id && target) {
            targets[id] = target;
        }
    }

    return targets;
}

/** Strings written once and referenced by index; a cell of t="s" carries that index. */
function readSharedStrings(xml: string | null): string[] {
    if (!xml) {
        return [];
    }

    return (xml.match(/<(?:\w+:)?si\b[\s\S]*?<\/(?:\w+:)?si>|<(?:\w+:)?si\b[^>]*\/>/g) ?? []).map((si) =>
        (si.match(/<(?:\w+:)?t\b[^>]*>([\s\S]*?)<\/(?:\w+:)?t>/g) ?? [])
            .map((t) => decode(t.replace(/^<(?:\w+:)?t\b[^>]*>|<\/(?:\w+:)?t>$/g, '')))
            .join('')
    );
}

function readSheet(xml: string | null, sharedStrings: string[], dateStyles: Set<number>): string[][] {
    if (!xml) {
        return [];
    }

    const rows: string[][] = [];

    for (const rowXml of xml.match(/<(?:\w+:)?row\b[\s\S]*?<\/(?:\w+:)?row>|<(?:\w+:)?row\b[^>]*\/>/g) ?? []) {
        const cells: string[] = [];

        for (const cellXml of rowXml.match(/<(?:\w+:)?c\b[\s\S]*?<\/(?:\w+:)?c>|<(?:\w+:)?c\b[^>]*\/>/g) ?? []) {
            // The reference (A1, B1, ...) is what says which column a cell is in: blanks are not written.
            const column = columnOf(attribute(cellXml, 'r'));
            const at = column >= 0 ? column : cells.length;
            while (cells.length < at) {
                cells.push('');
            }
            cells[at] = cellValue(cellXml, sharedStrings, dateStyles);
        }

        rows.push(cells);
    }

    return rows;
}

function cellValue(cellXml: string, sharedStrings: string[], dateStyles: Set<number>): string {
    const type = attribute(cellXml, 't');

    if (type === 'inlineStr') {
        return (cellXml.match(/<(?:\w+:)?t\b[^>]*>([\s\S]*?)<\/(?:\w+:)?t>/g) ?? [])
            .map((t) => decode(t.replace(/^<(?:\w+:)?t\b[^>]*>|<\/(?:\w+:)?t>$/g, '')))
            .join('');
    }

    // <v> holds the value: a shared-string index, a number, or the result of a formula.
    const raw = cellXml.match(/<(?:\w+:)?v\b[^>]*>([\s\S]*?)<\/(?:\w+:)?v>/)?.[1];
    if (raw === undefined) {
        return '';
    }

    const value = decode(raw);
    if (type === 's') {
        return sharedStrings[Number(value)] ?? '';
    }

    const style = Number(attribute(cellXml, 's'));
    const serial = Number(value);
    return !Number.isNaN(style) && dateStyles.has(style) && serial > 0 ? dateText(serial) : value;
}

/** "BC12" is column 55, counting from zero. */
function columnOf(reference: string | null): number {
    const letters = reference?.match(/^[A-Z]+/)?.[0];
    if (!letters) {
        return -1;
    }

    let column = 0;
    for (const letter of letters) {
        column = column * 26 + (letter.charCodeAt(0) - 64);
    }

    return column - 1;
}

function attribute(tag: string, name: string): string | null {
    const match = tag.match(new RegExp(`\\s${name.replace(':', '\\:')}="([^"]*)"`));
    return match ? decode(match[1]) : null;
}

function normalise(target: string): string {
    const path = target.replace(/^\//, '');
    return path.startsWith('xl/') ? path : 'xl/' + path;
}

function decode(xml: string): string {
    return xml
        .replace(/&lt;/g, '<')
        .replace(/&gt;/g, '>')
        .replace(/&quot;/g, '"')
        .replace(/&apos;/g, "'")
        .replace(/&#(\d+);/g, (_, code) => String.fromCharCode(Number(code)))
        .replace(/&amp;/g, '&');
}
