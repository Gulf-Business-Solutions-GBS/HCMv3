import { Component, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { LocalizationService } from 'abp-ng2-module';
import { AppConsts } from '@shared/AppConsts';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { GoLiveStateService } from '../shared/go-live-state.service';
import { GoLiveApplyService } from '../shared/go-live-apply.service';
import { StatusDotComponent } from '../shared/status-dot.component';
import { findStepById } from '../shared/go-live-registry';
import { GoLiveStep, CrossCheck } from '../shared/go-live.types';

/**
 * The Go-Live Setup home screen.
 *
 * Answers one question above everything else -- can we run the first payroll yet -- and, when the
 * answer is no, says exactly what is stopping it and links straight to the step that fixes it.
 */
@Component({
    templateUrl: './readiness-board.component.html',
    imports: [RouterLink, StatusDotComponent, LocalizePipe],
    styleUrl: './readiness-board.component.css',
})
export class ReadinessBoardComponent {
    private readonly router = inject(Router);
    private readonly localization = inject(LocalizationService);
    private readonly source = AppConsts.localization.defaultLocalizationSourceName;

    readonly state = inject(GoLiveStateService);
    readonly applyService = inject(GoLiveApplyService);

    progressLabel(step: GoLiveStep): string {
        const { done, total } = this.state.progressOf(step);
        if (step.kind === 'data') {
            return done === 0 ? '' : this.l('GoLiveRowsEntered', done);
        }
        if (step.kind === 'questions') {
            return this.l('GoLiveAnsweredOf', done, total);
        }
        return '';
    }

    /** Row label for the results table: "S3.1 Employee". */
    stageLabel(stage: { stepId?: string; target?: string }): string {
        return [stage.stepId, stage.target].filter((p) => !!p).join(' ');
    }

    detailFor(check: CrossCheck): string {
        return this.l(check.detailKey, ...check.params);
    }

    goToStep(stepId: string | undefined): void {
        const step = stepId ? findStepById(stepId) : undefined;
        if (step) {
            this.router.navigate(['/app/admin/go-live-setup/step', step.path]);
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
