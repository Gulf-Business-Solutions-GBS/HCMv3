/** Column and cell contracts for the paste grid. No Angular dependency -- kept plain so the coercion
 *  rules in paste-grid.coercion.ts stay unit-testable. */

export type CellValue = string | number | null;

/** A row is a bag keyed by column key. Deliberately not a typed DTO: one grid serves every Go-Live Setup sheet. */
export type PasteGridRow = Record<string, CellValue>;

export type PasteGridColumnType = 'text' | 'number' | 'date' | 'choice' | 'readonly';

export interface PasteGridColumn {
    /** Property name on the row object, and the key used in issue reporting. */
    key: string;

    /** Column heading shown to the user. Already localised by the caller. */
    header: string;

    type?: PasteGridColumnType;

    /** Rendered width in pixels. Defaults to 150. */
    width?: number;

    /** Blocks go-live if empty, but only on a row that has some other value in it. */
    required?: boolean;

    /** Allowed values for `type: 'choice'`. Matching is case- and whitespace-insensitive; the stored
     *  value is always the canonical spelling from this list. */
    options?: string[];

    maxLength?: number;
    min?: number;
    max?: number;

    /** Shown on hover over the column heading. */
    hint?: string;

    /** Rule this grid cannot know about -- an IBAN checksum, a cross-column date order. Return a
     *  localisation key (optionally with params) to flag the cell, or null when the value is fine. */
    validate?: (value: CellValue, row: PasteGridRow) => CellIssue | null;
}

/** A validation failure, carried as a localisation key so coercion stays pure and testable. */
export interface CellIssue {
    key: string;
    params?: (string | number)[];
}

export interface CellRef {
    r: number;
    c: number;
}

/** One flattened problem, for the summary strip and for the caller. */
export interface PasteGridIssue {
    row: number;
    key: string;
    header: string;
    issue: CellIssue;
}

export function isRowEmpty(row: PasteGridRow): boolean {
    for (const k of Object.keys(row)) {
        const v = row[k];
        if (v !== null && v !== undefined && v !== '') {
            return false;
        }
    }
    return true;
}
