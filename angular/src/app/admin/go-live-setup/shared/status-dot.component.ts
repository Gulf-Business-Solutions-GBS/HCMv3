import { Component, computed, input } from '@angular/core';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { StepStatus } from './go-live.types';

/** Status lamp plus label. Encodes state in shape and colour, so the board reads at a glance. */
@Component({
    selector: 'app-status-dot',
    template: `
        <span class="sd" [class]="'sd-' + status()">
            <span class="sd-dot" aria-hidden="true"></span>
            @if (showLabel()) {
                <span class="sd-label">{{ labelKey() | localize }}</span>
            }
        </span>
    `,
    styles: [
        `
            .sd {
                display: inline-flex;
                align-items: center;
                gap: 6px;
                white-space: nowrap;
            }
            .sd-dot {
                width: 8px;
                height: 8px;
                border-radius: 50%;
                background: currentColor;
                flex: 0 0 auto;
            }
            .sd-label {
                font-size: 11px;
                font-weight: 600;
                letter-spacing: 0.04em;
                text-transform: uppercase;
            }
            .sd-not-started {
                color: #8797a0;
            }
            .sd-not-started .sd-dot {
                background: transparent;
                border: 1.5px solid currentColor;
            }
            .sd-in-progress {
                color: #9c6714;
            }
            .sd-attention {
                color: #a33228;
            }
            .sd-ready {
                color: #1c7a4f;
            }
        `,
    ],
    imports: [LocalizePipe],
})
export class StatusDotComponent {
    readonly status = input.required<StepStatus>();
    readonly showLabel = input(true);

    readonly labelKey = computed(() => {
        switch (this.status()) {
            case 'ready':
                return 'GoLiveStatusReady';
            case 'attention':
                return 'GoLiveStatusAttention';
            case 'in-progress':
                return 'GoLiveStatusInProgress';
            default:
                return 'GoLiveStatusNotStarted';
        }
    });
}
