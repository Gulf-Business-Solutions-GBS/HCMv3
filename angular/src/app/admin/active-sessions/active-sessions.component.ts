import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { EntityDtoOfInt64, UserSessionDto, UserSessionServiceProxy } from '@shared/service-proxies/service-proxies';
import { finalize } from 'rxjs/operators';
import { SubHeaderComponent } from '../../shared/common/sub-header/sub-header.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import { TableModule } from 'primeng/table';
import { Tooltip } from 'primeng/tooltip';

@Component({
    templateUrl: './active-sessions.component.html',
    animations: [appModuleAnimation()],
    imports: [SubHeaderComponent, LocalizePipe, LuxonFormatPipe, TableModule, Tooltip],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ActiveSessionsComponent extends AppComponentBase implements OnInit {
    private readonly _userSessionService = inject(UserSessionServiceProxy);

    readonly loading = signal(false);
    readonly sessions = signal<UserSessionDto[]>([]);

    get isRevocationEnabled(): boolean {
        return this.appSession.isSessionRevocationEnabled;
    }

    ngOnInit(): void {
        this.loadSessions();
    }

    loadSessions(): void {
        this.loading.set(true);
        this._userSessionService
            .getSessions(undefined)
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe((result) => {
                this.sessions.set(result.items ?? []);
            });
    }

    hasOtherSessions(): boolean {
        return this.sessions().some((s) => !s.isCurrent);
    }

    revokeSession(session: UserSessionDto): void {
        this.message.confirm(this.l('RevokeSessionConfirmation'), this.l('AreYouSure'), (isConfirmed) => {
            if (isConfirmed) {
                const input = new EntityDtoOfInt64();
                input.id = session.id;
                this._userSessionService.revokeSession(input).subscribe(() => {
                    this.notify.success(this.l('SessionRevoked'));
                    this.loadSessions();
                });
            }
        });
    }

    revokeAllOtherSessions(): void {
        this.message.confirm(this.l('RevokeAllOtherSessionsConfirmation'), this.l('AreYouSure'), (isConfirmed) => {
            if (isConfirmed) {
                this._userSessionService.revokeAllOtherSessions().subscribe(() => {
                    this.notify.success(this.l('AllOtherSessionsRevoked'));
                    this.loadSessions();
                });
            }
        });
    }
}
