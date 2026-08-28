import { Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { map } from 'rxjs/operators';
import { LocalizationService } from 'abp-ng2-module';
import { AppConsts } from '@shared/AppConsts';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { GoLiveStateService } from '../shared/go-live-state.service';
import { StatusDotComponent } from '../shared/status-dot.component';
import { PasteGridComponent } from '../shared/paste-grid/paste-grid.component';
import { QuestionCardComponent } from '../shared/question-card/question-card.component';
import { PasteGridIssue, PasteGridRow } from '../shared/paste-grid/paste-grid.types';
import { AnswerChange, visibleQuestions } from '../shared/question-card/go-live-question.types';
import { findStepByPath, nextStep, previousStep } from '../shared/go-live-registry';
import { CrossCheck } from '../shared/go-live.types';

/**
 * Renders whichever Go-Live Setup step the route names.
 *
 * One component for every step is the point: a question sheet, a bulk grid and the cross-check summary
 * are three renderers over registry data, not twenty hand-built screens. Adding a step means adding a
 * registry entry and nothing else.
 */
@Component({
    templateUrl: './go-live-step.component.html',
    imports: [RouterLink, LocalizePipe, StatusDotComponent, PasteGridComponent, QuestionCardComponent],
    styleUrl: './go-live-step.component.css',
})
export class GoLiveStepComponent {
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly localization = inject(LocalizationService);
    private readonly source = AppConsts.localization.defaultLocalizationSourceName;

    readonly state = inject(GoLiveStateService);

    /**
     * Read from paramMap rather than a route-bound input: the router reuses this component instance when
     * moving between steps, so a snapshot would go stale, and the app does not enable
     * withComponentInputBinding().
     */
    readonly stepPath = toSignal(this.route.paramMap.pipe(map((p) => p.get('stepPath') ?? '')), {
        initialValue: '',
    });

    readonly step = computed(() => findStepByPath(this.stepPath()));

    readonly previous = computed(() => previousStep(this.stepPath()));
    readonly next = computed(() => nextStep(this.stepPath()));

    readonly questions = computed(() => {
        const step = this.step();
        if (!step || step.kind !== 'questions') {
            return [];
        }
        return visibleQuestions(step.questions ?? [], this.state.answers());
    });

    readonly rows = computed<PasteGridRow[]>(() => {
        const step = this.step();
        return step ? this.state.rowsFor(step.id) : [];
    });

    onAnswered(change: AnswerChange): void {
        this.state.setAnswer(change);
    }

    onRowsChange(rows: PasteGridRow[]): void {
        const step = this.step();
        if (step) {
            this.state.setRows(step.id, rows);
        }
    }

    onIssuesChange(issues: PasteGridIssue[]): void {
        const step = this.step();
        if (step) {
            this.state.setIssues(step.id, issues);
        }
    }

    detailFor(check: CrossCheck): string {
        return this.l(check.detailKey, ...check.params);
    }

    goToStep(stepId: string | undefined): void {
        if (!stepId) {
            return;
        }
        const target = this.state.stages.flatMap((s) => s.steps).find((s) => s.id === stepId);
        if (target) {
            this.router.navigate(['/app/admin/go-live-setup/step', target.path]);
        }
    }

    l(key: string, ...args: unknown[]): string {
        let text = this.localization.localize(key, this.source);
        if (!text) {
            text = key;
        }
        if (!args.length) {
            return text;
        }
        return abp.utils.formatString.apply(this, [text, ...args] as never);
    }
}
