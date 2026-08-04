import { Injectable, signal } from '@angular/core';
import { LocalizationService } from './localization.service';
import { inject } from '@angular/core';

export type ToastSeverity = 'success' | 'error' | 'info';

export interface Toast {
    id: number;
    severity: ToastSeverity;
    title: string;
    detail?: string;
}

/**
 * Minimal signal-backed toast stack. The portal deliberately does not pull in the admin SPA's
 * sweetalert2/abp.notify stack: those are ~200kb of dependencies for what is three lines of state on a
 * public site that must stay fast on a phone.
 */
@Injectable({ providedIn: 'root' })
export class ToastService {
    private _localization = inject(LocalizationService);
    private _nextId = 1;

    readonly toasts = signal<Toast[]>([]);

    success(title: string, detail?: string): void {
        this.push('success', title, detail);
    }

    error(title: string, detail?: string): void {
        this.push('error', title, detail);
    }

    info(title: string, detail?: string): void {
        this.push('info', title, detail);
    }

    /**
     * Surfaces an error from an NSwag proxy call. ABP serialises `UserFriendlyException` as
     * `{ error: { message, details } }`, and the careers services rely on that heavily (for example
     * "portal not recognized" when the subdomain does not resolve to a tenant), so the server's own message
     * is shown whenever one is present rather than a generic failure string.
     */
    apiError(error: unknown): void {
        const parsed = ToastService.parseAbpError(error);
        this.error(
            parsed?.message ?? this._localization.localize('CareersUnexpectedError'),
            parsed?.details
        );
    }

    static parseAbpError(error: any): { message: string; details?: string } | null {
        // NSwag's ApiException carries the raw response body on `.response`.
        const raw = error?.response ?? error?.error ?? error;

        let body = raw;
        if (typeof raw === 'string') {
            try {
                body = JSON.parse(raw);
            } catch {
                return null;
            }
        }

        const abpError = body?.error;
        if (!abpError?.message) {
            return null;
        }

        // ABP puts per-field failures in validationErrors; join them so the candidate sees what to fix.
        const validation = abpError.validationErrors as { message: string }[] | undefined;
        const details = validation?.length
            ? validation.map((v) => v.message).join(' ')
            : abpError.details;

        return { message: abpError.message, details };
    }

    dismiss(id: number): void {
        this.toasts.update((list) => list.filter((t) => t.id !== id));
    }

    private push(severity: ToastSeverity, title: string, detail?: string): void {
        const toast: Toast = { id: this._nextId++, severity, title, detail };
        this.toasts.update((list) => [...list, toast]);
        setTimeout(() => this.dismiss(toast.id), severity === 'error' ? 8000 : 4000);
    }
}
