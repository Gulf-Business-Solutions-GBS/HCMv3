import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { AppConsts } from '@shared/AppConsts';
import { Observable, map, of, shareReplay, catchError } from 'rxjs';

/**
 * Fetches employee photos with the signed-in user's token and hands back object URLs.
 *
 * <p>Needed because /api/employee-photo/{id} is [AbpMvcAuthorize(JwtBearer)]: a plain &lt;img src&gt; sends
 * no Authorization header, so the browser gets 401 and draws a broken-image icon. Going through HttpClient
 * puts the request through the app's interceptor, which attaches the token.</p>
 *
 * <p>Cached per URL because one activity feed can show the same approver on a dozen rows, and each of those
 * would otherwise be its own request.</p>
 */
class EmployeePhotoCache {
    private readonly cache = new Map<string, Observable<string | undefined>>();

    constructor(private readonly http: HttpClient) {}

    get(url: string): Observable<string | undefined> {
        let existing = this.cache.get(url);

        if (!existing) {
            existing = this.http.get(url, { responseType: 'blob' }).pipe(
                map((blob) => (blob && blob.size ? URL.createObjectURL(blob) : undefined)),
                // A missing photo is not an error worth telling anyone about: the avatar falls back to
                // initials and the page carries on.
                catchError(() => of(undefined)),
                shareReplay(1)
            );

            this.cache.set(url, existing);
        }

        return existing;
    }
}

let sharedCache: EmployeePhotoCache | undefined;

/**
 * An employee's photo, or their initials when there is none.
 *
 * <p>Takes the photo URL the server put on the DTO rather than an employee id, so it only ever requests a
 * photo that is known to exist -- the server sets the field to null otherwise. The URL arrives relative
 * ("/api/employee-photo/12"), which resolves against the Angular origin and 404s; it is made absolute
 * against the API host here, which is the same thing the org chart and the search bar do inline.</p>
 */
@Component({
    selector: 'employee-avatar',
    imports: [CommonModule],
    template: `
        <span class="employee-avatar" [style.width.px]="size()" [style.height.px]="size()">
            @if (src()) {
                <img [src]="src()" [alt]="name()" />
            } @else {
                <span class="employee-avatar__initials">{{ initials() }}</span>
            }
        </span>
    `,
    styles: [
        `
            .employee-avatar {
                display: inline-flex;
                align-items: center;
                justify-content: center;
                border-radius: 50%;
                overflow: hidden;
                background-color: var(--bs-gray-200);
                color: var(--bs-gray-600);
                font-weight: 600;
                font-size: 0.8rem;
                flex: 0 0 auto;
            }

            .employee-avatar img {
                width: 100%;
                height: 100%;
                object-fit: cover;
            }
        `,
    ],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EmployeeAvatarComponent {
    private readonly http = inject(HttpClient);

    /** As the server sent it -- relative or absolute, or undefined when the employee has no photo. */
    readonly photoUrl = input<string | undefined>(undefined);
    readonly name = input<string>('');
    readonly size = input<number>(36);

    readonly src = signal<string | undefined>(undefined);

    readonly initials = computed(() => {
        const name = (this.name() ?? '').trim();
        if (!name) {
            return '?';
        }

        const parts = name.split(/\s+/);

        return (parts[0][0] + (parts.length > 1 ? parts[parts.length - 1][0] : '')).toUpperCase();
    });

    constructor() {
        if (!sharedCache) {
            sharedCache = new EmployeePhotoCache(this.http);
        }

        effect((onCleanup) => {
            const url = this.photoUrl();
            this.src.set(undefined);

            if (!url) {
                return;
            }

            const absolute = url.startsWith('http') ? url : AppConsts.remoteServiceBaseUrl + url;

            const subscription = sharedCache!.get(absolute).subscribe((objectUrl) => this.src.set(objectUrl));

            onCleanup(() => subscription.unsubscribe());
        });
    }
}
