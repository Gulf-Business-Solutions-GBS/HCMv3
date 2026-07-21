import { Component, OnInit, forwardRef, HostBinding, inject, input } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { UserDelegationServiceProxy, UserDelegationDto } from '@shared/service-proxies/service-proxies';
import { NG_VALUE_ACCESSOR, FormsModule } from '@angular/forms';
import { ImpersonationService } from '@app/admin/users/impersonation.service';

import { LuxonFormatPipe } from '../../../../shared/utils/luxon-format.pipe';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'active-delegated-users-combo',
    template: `
        @if (delegations?.length) {
            <div
                class="d-flex align-items-center ms-1 ms-lg-3 active-user-delegations"
                data-kt-menu-trigger="click"
                data-kt-menu-attach="parent"
                data-kt-menu-placement="bottom-start">
                <div [class]="customStyle()" [attr.title]="'SwitchToUser' | localize">
                    <i class="ki-duotone ki-profile-user fs-2">
                        <span class="path1"></span>
                        <span class="path2"></span>
                        <span class="path3"></span>
                        <span class="path4"></span>
                    </i>
                </div>
            </div>
            <div
                class="menu menu-sub menu-sub-dropdown menu-column menu-rounded menu-gray-800 menu-state-bg-light-primary fw-semibold w-250px w-md-300px py-4"
                data-kt-menu="true">
                <div class="menu-item px-3">
                    <div class="menu-content text-muted pb-2 px-3 fs-7">
                        {{ 'SwitchToUser' | localize }}
                    </div>
                </div>
                <div class="separator mb-3 opacity-75"></div>
                @for (delegation of delegations; track delegation) {
                    <div class="menu-item px-3">
                        <a
                            href="javascript:void(0);"
                            (click)="switchToDelegatedUser(delegation)"
                            class="menu-link px-3 d-flex flex-column">
                            <span class="fs-8 text-gray-800">
                                {{ delegation.username }} ({{
                                    'ExpiresAt' | localize: (delegation.endTime | luxonFormat: 'F')
                                }})
                            </span>
                        </a>
                    </div>
                }
            </div>
        }
    `,
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef(() => ActiveDelegatedUsersComboComponent),
            multi: true,
        },
    ],
    imports: [FormsModule, LuxonFormatPipe, LocalizePipe],
})
export class ActiveDelegatedUsersComboComponent extends AppComponentBase implements OnInit {
    private _userDelegationService = inject(UserDelegationServiceProxy);
    private _impersonationService = inject(ImpersonationService);

    @HostBinding('style.display') public display = 'flex';
    readonly customStyle = input(
        'btn btn-icon btn-custom btn-icon-muted btn-active-light btn-active-color-primary w-35px h-35px w-md-40px h-md-40px'
    );

    delegations: UserDelegationDto[] = [];

    ngOnInit(): void {
        this._userDelegationService.getActiveUserDelegations().subscribe((result) => {
            this.delegations = result;
        });
    }

    switchToDelegatedUser(delegation: UserDelegationDto): void {
        this.message.confirm(
            this.l('SwitchToDelegatedUserWarningMessage', delegation.username),
            this.l('AreYouSure'),
            (isConfirmed) => {
                if (isConfirmed) {
                    this._impersonationService.delegatedImpersonate(delegation.id, this.appSession.tenantId);
                }
            }
        );
    }
}
