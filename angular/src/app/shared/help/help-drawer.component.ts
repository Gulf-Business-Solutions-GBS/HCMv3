import { Component, ElementRef, computed, effect, inject, signal } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { HelpField, HelpService } from './help.service';

interface HelpGroup {
    title: string;
    fields: HelpField[];
}

/**
 * The single help surface. Page help and F1 field help are the same drawer -- F1 only differs in
 * scrolling to a section and highlighting it -- because two implementations of "explain this" drift
 * apart, and the one with the current content would not be both.
 */
@Component({
    selector: 'help-drawer',
    templateUrl: './help-drawer.component.html',
    styleUrl: './help-drawer.component.css',
    imports: [LocalizePipe],
})
export class HelpDrawerComponent extends AppComponentBase {
    readonly help = inject(HelpService);

    private _el: ElementRef<HTMLElement> = inject(ElementRef);

    /** Permissions do not change without a reload, so this is read once rather than per render. */
    readonly canSeeTechnical = this.help.canSeeTechnical();

    /**
     * The technical panel reads the EF model for the page entity, so a page that has no entity -- a
     * self-service screen documented as topics (DESIGN.md 5.4) -- has nothing to show. The button is
     * hidden rather than left to expand onto nothing.
     */
    readonly showTechnical = computed(() => this.canSeeTechnical && !!this.help.content()?.entity);

    /** Which topic's "Copy" button last succeeded, so it can briefly say "Copied". */
    readonly copiedId = signal<string | null>(null);

    /** Only the fields the host screen is actually rendering (DESIGN.md §8.1). */
    readonly groups = computed<HelpGroup[]>(() => {
        const doc = this.help.content();
        if (!doc) return [];
        const visible = this.help.visibleIds();

        const out: HelpGroup[] = [];
        for (const field of doc.fields) {
            if (visible && !this.isVisible(field, visible)) continue;
            const title = this.l(field.section);
            const last = out[out.length - 1];
            if (last && last.title === title) last.fields.push(field);
            else out.push({ title, fields: [field] });
        }
        return out;
    });

    readonly shownCount = computed(() =>
        this.groups().reduce((n, g) => n + g.fields.length, 0)
    );

    constructor() {
        super();

        effect(() => {
            const target = this.help.activeFieldId();
            if (!target || !this.help.content() || !this.help.isOpen()) return;
            this.scrollToTopic(target, 12);
        });
    }

    /**
     * Scrolls the drawer body to a topic by adjusting the container's own scrollTop.
     *
     * `scrollIntoView({behavior: 'smooth'})` was the obvious call and the wrong one: smooth
     * scrolling is animation, and animation can be frozen or dropped. When it is, the topic is
     * highlighted but never comes into view -- the user presses F1 and lands nowhere, which is
     * precisely the failure the feature exists to prevent. An instant, arithmetic scroll cannot
     * fail that way, and someone who pressed F1 wants the answer rather than the journey.
     */
    private scrollToTopic(fieldId: string, attemptsLeft: number): void {
        const body = this._el.nativeElement.querySelector<HTMLElement>('.help-body');
        const topic = this._el.nativeElement.querySelector<HTMLElement>(
            `#${CSS.escape(this.anchorId(fieldId))}`
        );

        // The signal that names the topic can change a beat before the drawer is in the DOM, and
        // the panel needs a laid-out height before an offset means anything. A single
        // setTimeout(0) loses that race often enough to matter, and losing it silently leaves the
        // topic highlighted somewhere the reader cannot see -- so retry briefly instead.
        if (!body || !topic || body.clientHeight === 0) {
            if (attemptsLeft > 0) {
                setTimeout(() => this.scrollToTopic(fieldId, attemptsLeft - 1), 40);
            }
            return;
        }
        body.scrollTop += topic.getBoundingClientRect().top - body.getBoundingClientRect().top;
    }

    /**
     * The DOM is read the moment the drawer opens, which on a cold open is before the JSON has
     * arrived -- so markers seen then are still raw `loc:` keys rather than canonical ids. Matching
     * on either form makes the drawer correct regardless of which landed first, instead of relying
     * on a reconcile pass winning a race.
     */
    private isVisible(field: HelpField, visible: Set<string>): boolean {
        if (visible.has(field.id)) return true;
        return field.aliases.some((alias) => visible.has(alias));
    }

    /** `Employees.NationalityId` to the clipboard -- what someone writing SQL actually wants. */
    async copy(text: string, fieldId: string): Promise<void> {
        try {
            await navigator.clipboard.writeText(text);
            this.copiedId.set(fieldId);
            setTimeout(() => {
                if (this.copiedId() === fieldId) this.copiedId.set(null);
            }, 1500);
        } catch {
            // Clipboard refused (insecure context, denied permission): the text is on screen to select.
        }
    }

    anchorId(fieldId: string): string {
        return `help-topic-${fieldId.replace(/\./g, '-')}`;
    }

    isActive(fieldId: string): boolean {
        return this.help.activeFieldId() === fieldId;
    }

    close(): void {
        this.help.close();
    }
}
