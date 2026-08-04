import { Component, inject } from '@angular/core';
import { ToastService } from './toast.service';

@Component({
    selector: 'careers-toasts',
    template: `
        <div class="toast-stack" role="status" aria-live="polite">
            @for (toast of toastService.toasts(); track toast.id) {
                <div class="toast toast--{{ toast.severity }}">
                    <div class="toast__body">
                        <p class="toast__title">{{ toast.title }}</p>
                        @if (toast.detail) {
                            <p class="toast__detail">{{ toast.detail }}</p>
                        }
                    </div>
                    <button type="button" class="toast__close" (click)="toastService.dismiss(toast.id)" aria-label="Close">
                        &times;
                    </button>
                </div>
            }
        </div>
    `,
    styles: [
        `
            .toast-stack {
                position: fixed;
                top: 1rem;
                right: 1rem;
                z-index: 1080;
                display: flex;
                flex-direction: column;
                gap: 0.5rem;
                max-width: min(24rem, calc(100vw - 2rem));
            }
            .toast {
                display: flex;
                align-items: flex-start;
                gap: 0.75rem;
                padding: 0.875rem 1rem;
                border-radius: 0.75rem;
                background: #fff;
                box-shadow: 0 12px 32px rgba(15, 23, 42, 0.18);
                border-left: 4px solid #64748b;
                animation: toast-in 160ms ease-out;
            }
            .toast--success {
                border-left-color: #16a34a;
            }
            .toast--error {
                border-left-color: #dc2626;
            }
            .toast--info {
                border-left-color: #2563eb;
            }
            .toast__body {
                flex: 1;
            }
            .toast__title {
                margin: 0;
                font-weight: 600;
                font-size: 0.9375rem;
                color: #0f172a;
            }
            .toast__detail {
                margin: 0.25rem 0 0;
                font-size: 0.8125rem;
                color: #475569;
            }
            .toast__close {
                border: 0;
                background: transparent;
                font-size: 1.25rem;
                line-height: 1;
                cursor: pointer;
                color: #94a3b8;
            }
            @keyframes toast-in {
                from {
                    opacity: 0;
                    transform: translateY(-6px);
                }
                to {
                    opacity: 1;
                    transform: translateY(0);
                }
            }
        `,
    ],
})
export class ToastComponent {
    readonly toastService = inject(ToastService);
}
