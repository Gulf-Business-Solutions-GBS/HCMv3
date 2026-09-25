import { Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { DateInputDirective } from './date-input.directive';

@Component({
    template: `<input type="date" name="startDate" [ngModel]="value()" (ngModelChange)="value.set($event)" />`,
    imports: [FormsModule, DateInputDirective],
})
class DateHostComponent {
    readonly value = signal<string | null>(null);
}

@Component({
    template: `<input type="datetime-local" name="when" [ngModel]="value()" (ngModelChange)="value.set($event)" />`,
    imports: [FormsModule, DateInputDirective],
})
class DateTimeHostComponent {
    readonly value = signal<string | null>(null);
}

@Component({
    template: `<input type="month" name="month" [ngModel]="value()" (ngModelChange)="value.set($event)" />`,
    imports: [FormsModule, DateInputDirective],
})
class MonthHostComponent {
    readonly value = signal<string | null>(null);
}

/** ngModel writes through a microtask, and a value written back reaches the field on the next change detection. */
function settle(fixture: ComponentFixture<unknown>): Promise<void> {
    return fixture.whenStable().then(() => {
        fixture.detectChanges();
    });
}

describe('DateInputDirective', () => {
    let fixture: ComponentFixture<DateHostComponent>;
    let host: DateHostComponent;
    let input: HTMLInputElement;

    async function type(text: string): Promise<void> {
        input.value = text;
        input.setSelectionRange(text.length, text.length);
        input.dispatchEvent(new Event('input'));
        await settle(fixture);
    }

    beforeEach(async () => {
        await TestBed.configureTestingModule({ imports: [DateHostComponent] }).compileComponents();

        fixture = TestBed.createComponent(DateHostComponent);
        fixture.autoDetectChanges(true);
        await fixture.whenStable();

        host = fixture.componentInstance;
        input = fixture.nativeElement.querySelector('input');
    });

    it('turns the native date field into a text box that says how to type it', () => {
        expect(input.type).toBe('text');
        expect(input.placeholder).toBe('dd/mm/yyyy');
    });

    it('shows a value it is given as dd/MM/yyyy', async () => {
        host.value.set('2026-09-24');
        await settle(fixture);

        expect(input.value).toBe('24/09/2026');
    });

    it('hands back the ISO date for a day typed as dd/MM/yyyy', async () => {
        await type('24/09/2026');

        expect(host.value()).toBe('2026-09-24');
    });

    it('puts the slashes in while they type', async () => {
        await type('24092026');

        expect(input.value).toBe('24/09/2026');
        expect(host.value()).toBe('2026-09-24');
    });

    it('reads 09 as September rather than as the day', async () => {
        await type('01/09/2026');

        expect(host.value()).toBe('2026-09-01');
    });

    it('holds nothing back while the date is half typed', async () => {
        await type('24/09');

        expect(host.value()).toBeNull();
    });

    it('refuses a day that month does not have', async () => {
        await type('31/02/2026');

        expect(host.value()).toBeNull();
    });

    it('refuses a two-digit year rather than guessing the century', async () => {
        await type('24/09/26');

        expect(host.value()).toBeNull();
    });

    it('clears the value when the field is emptied', async () => {
        await type('24/09/2026');
        await type('');

        expect(host.value()).toBeNull();
    });

    it('tidies what they typed once they leave the field', async () => {
        await type('9/9/2026');
        input.dispatchEvent(new Event('blur'));
        await settle(fixture);

        expect(input.value).toBe('09/09/2026');
        expect(host.value()).toBe('2026-09-09');
    });

    it('refuses a date beyond the latest the screen allows', async () => {
        input.setAttribute('max', '2026-09-30');

        await type('01/10/2026');

        expect(host.value()).toBeNull();
    });
});

describe('DateInputDirective on a date and time', () => {
    let fixture: ComponentFixture<DateTimeHostComponent>;
    let host: DateTimeHostComponent;
    let input: HTMLInputElement;

    beforeEach(async () => {
        await TestBed.configureTestingModule({ imports: [DateTimeHostComponent] }).compileComponents();

        fixture = TestBed.createComponent(DateTimeHostComponent);
        fixture.autoDetectChanges(true);
        await fixture.whenStable();

        host = fixture.componentInstance;
        input = fixture.nativeElement.querySelector('input');
    });

    it('shows the day before the time', async () => {
        host.value.set('2026-09-24T07:30');
        await settle(fixture);

        expect(input.value).toBe('24/09/2026 07:30');
    });

    it('hands back the day and the time it was given', async () => {
        input.value = '24/09/2026 07:30';
        input.setSelectionRange(16, 16);
        input.dispatchEvent(new Event('input'));
        await settle(fixture);

        expect(host.value()).toBe('2026-09-24T07:30');
    });
});

describe('DateInputDirective on a month', () => {
    let fixture: ComponentFixture<MonthHostComponent>;
    let host: MonthHostComponent;
    let input: HTMLInputElement;

    async function type(text: string): Promise<void> {
        input.value = text;
        input.setSelectionRange(text.length, text.length);
        input.dispatchEvent(new Event('input'));
        await settle(fixture);
    }

    beforeEach(async () => {
        await TestBed.configureTestingModule({ imports: [MonthHostComponent] }).compileComponents();

        fixture = TestBed.createComponent(MonthHostComponent);
        fixture.autoDetectChanges(true);
        await fixture.whenStable();

        host = fixture.componentInstance;
        input = fixture.nativeElement.querySelector('input');
    });

    it('asks for the month in figures rather than by name', () => {
        expect(input.type).toBe('text');
        expect(input.placeholder).toBe('mm/yyyy');
    });

    it('shows a stored month as mm/yyyy', async () => {
        host.value.set('2026-09');
        await settle(fixture);

        expect(input.value).toBe('09/2026');
    });

    it('hands back the month they typed', async () => {
        await type('092026');

        expect(input.value).toBe('09/2026');
        expect(host.value()).toBe('2026-09');
    });

    it('takes a month typed short, with its own slash', async () => {
        await type('9/2026');

        expect(host.value()).toBe('2026-09');
    });

    it('refuses a month there is no such thing as', async () => {
        await type('13/2026');

        expect(host.value()).toBeNull();
    });

    it('holds nothing back while the year is half typed', async () => {
        await type('09/20');

        expect(host.value()).toBeNull();
    });
});
