import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { SubHeaderComponent } from '../../shared/common/sub-header/sub-header.component';
import { GoLiveStateService } from './shared/go-live-state.service';
import { StatusDotComponent } from './shared/status-dot.component';

/**
 * Routed shell for the Go-Live Setup.
 *
 * Holds the navigation rail and the overall progress figure; the board and the individual steps render
 * into the outlet. The rail is generated from the registry, so a new step appears here automatically.
 */
@Component({
    templateUrl: './go-live-shell.component.html',
    animations: [appModuleAnimation()],
    imports: [RouterOutlet, RouterLink, RouterLinkActive, SubHeaderComponent, StatusDotComponent, LocalizePipe],
    styleUrl: './go-live-shell.component.css',
})
export class GoLiveShellComponent extends AppComponentBase {
    readonly state = inject(GoLiveStateService);
}
