import { ComponentFixture, TestBed } from '@angular/core/testing';
import { LocalizationService } from 'abp-ng2-module';
import { PasteGridComponent } from './paste-grid.component';
import { PasteGridColumn, PasteGridRow } from './paste-grid.types';

/**
 * Mounts the real component, so this also proves the template compiles under strict template checking
 * (a missing entry in the `imports` array only shows up here or in a full `ng build`).
 */
describe('PasteGridComponent', () => {
    let fixture: ComponentFixture<PasteGridComponent>;
    let grid: PasteGridComponent;

    const columns: PasteGridColumn[] = [
        { key: 'code', header: 'Code', required: true },
        { key: 'name', header: 'Name', required: true },
        { key: 'salary', header: 'Salary', type: 'number' },
        { key: 'gender', header: 'Gender', type: 'choice', options: ['Male', 'Female'] },
    ];

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [PasteGridComponent],
            providers: [{ provide: LocalizationService, useValue: { localize: (k: string) => k } }],
        }).compileComponents();

        fixture = TestBed.createComponent(PasteGridComponent);
        grid = fixture.componentInstance;
        fixture.componentRef.setInput('columns', columns);
        fixture.componentRef.setInput('rows', []);
        fixture.detectChanges();
    });

    function paste(text: string): void {
        const data = new DataTransfer();
        data.setData('text/plain', text);
        grid.onPaste(new ClipboardEvent('paste', { clipboardData: data }));
        fixture.detectChanges();
    }

    function rowsWithData(): PasteGridRow[] {
        return grid.rows().filter((r) => Object.values(r).some((v) => v !== null && v !== ''));
    }

    it('should pad blank rows so there is always somewhere to paste', () => {
        expect(grid.rows().length).toBeGreaterThan(0);
        expect(grid.filledRowCount()).toBe(0);
    });

    it('should not flag required cells on untouched blank rows', () => {
        expect(grid.issueCount()).toBe(0);
    });

    it('should write a pasted block starting at the active cell', () => {
        paste('1001\tAhmed Ali\t650\tMale\n1002\tBader Naser\t800\tMale');

        const rows = rowsWithData();
        expect(rows.length).toBe(2);
        expect(rows[0]['code']).toBe('1001');
        expect(rows[0]['name']).toBe('Ahmed Ali');
        expect(rows[0]['salary']).toBe(650);
        expect(rows[1]['code']).toBe('1002');
        expect(grid.issueCount()).toBe(0);
    });

    it('should coerce pasted values to their column type', () => {
        paste('1001\tAhmed Ali\t1,250.500\tmale');

        const row = rowsWithData()[0];
        expect(row['salary']).toBe(1250.5);
        expect(row['gender']).toBe('Male');
    });

    it('should flag a value that is not in a choice list', () => {
        paste('1001\tAhmed Ali\t650\tMail');

        expect(grid.hasIssue(0, 'gender')).toBe(true);
        expect(grid.issueCount()).toBe(1);
    });

    it('should flag a missing required cell once the row has other data', () => {
        paste('\tAhmed Ali');

        expect(grid.hasIssue(0, 'code')).toBe(true);
    });

    it('should paste into the cell the user selected, not always at the top', () => {
        grid.active.set({ r: 3, c: 1 });
        paste('Moved Here');

        expect(grid.rows()[3]['name']).toBe('Moved Here');
        expect(grid.rows()[0]['name']).toBeNull();
    });

    it('should fill the whole selection when a single value is pasted over a range', () => {
        grid.active.set({ r: 0, c: 3 });
        grid.selectionEnd.set({ r: 2, c: 3 });
        paste('Female');

        expect(grid.rows()[0]['gender']).toBe('Female');
        expect(grid.rows()[1]['gender']).toBe('Female');
        expect(grid.rows()[2]['gender']).toBe('Female');
        expect(grid.rows()[3]['gender']).toBeNull();
    });

    it('should grow the sheet when the pasted block is longer than the rows on screen', () => {
        const lines = Array.from({ length: 120 }, (_, i) => `${1000 + i}\tName ${i}`).join('\n');
        paste(lines);

        expect(grid.filledRowCount()).toBe(120);
    });

    it('should clear issues when the offending row is emptied', () => {
        paste('1001\tAhmed Ali\t650\tMail');
        expect(grid.issueCount()).toBe(1);

        grid.active.set({ r: 0, c: 0 });
        grid.selectionEnd.set({ r: 0, c: 3 });
        grid.clearSelection();
        fixture.detectChanges();

        expect(grid.issueCount()).toBe(0);
    });

    it('should restore the previous data on undo', () => {
        paste('1001\tAhmed Ali');
        expect(grid.rows()[0]['code']).toBe('1001');

        grid.undo();
        fixture.detectChanges();

        expect(grid.rows()[0]['code']).toBeNull();
        expect(grid.issueCount()).toBe(0);
    });

    it('should copy the selected rectangle back out as TSV', () => {
        paste('1001\tAhmed Ali\n1002\tBader Naser');
        grid.active.set({ r: 0, c: 0 });
        grid.selectionEnd.set({ r: 1, c: 1 });

        const data = new DataTransfer();
        grid.onCopy(new ClipboardEvent('copy', { clipboardData: data }));

        expect(data.getData('text/plain')).toBe('1001\tAhmed Ali\r\n1002\tBader Naser');
    });

    it('should report issues to the caller', () => {
        const seen: number[] = [];
        grid.issuesChange.subscribe((issues) => seen.push(issues.length));

        paste('1001\tAhmed Ali\t650\tMail');

        expect(seen[seen.length - 1]).toBe(1);
    });

    it('should expose a truncation ceiling rather than growing without limit', () => {
        fixture.componentRef.setInput('maxRows', 5);
        const lines = Array.from({ length: 12 }, (_, i) => `${i}\tName`).join('\n');
        spyOn(abp.notify, 'warn');

        paste(lines);

        expect(grid.filledRowCount()).toBe(5);
        expect(abp.notify.warn).toHaveBeenCalled();
    });
});
