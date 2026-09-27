import { TestBed, ComponentFixture } from '@angular/core/testing';
import { NO_ERRORS_SCHEMA, LOCALE_ID, provideZonelessChangeDetection, importProvidersFrom } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideAnimations } from '@angular/platform-browser/animations';
import { ModalModule } from 'ngx-bootstrap/modal';
import { DateTime } from 'luxon';
import { of } from 'rxjs';
import { SERVICE_PROXY_PROVIDERS } from '@shared/service-proxies/service-proxy.providers';
import {
    API_BASE_URL,
    ApprovalRequire,
    WorkflowActivityServiceProxy,
    WorkflowApproverOrigin,
    WorkflowDelegationRelation,
    WorkflowParticipantsDto,
} from '@shared/service-proxies/service-proxies';
import { WorkflowParticipantsPanelComponent } from './workflow-participants-panel.component';

/**
 * Covers where a delegation is SHOWN.
 *
 * <p>It used to have a tab to itself, which meant reading the approver list, spotting a name the org chart
 * would not have predicted, and then opening a second list to find out why. The explanation now sits on the
 * row that raises the question.</p>
 *
 * <p>The panel is only reachable from self-service screens, which need a signed-in employee -- so driving
 * the component directly is what makes this checkable at all.</p>
 */
describe('WorkflowParticipantsPanelComponent delegations', () => {
    let fixture: ComponentFixture<WorkflowParticipantsPanelComponent>;

    /** One approver standing in for a colleague, as request 6 looked on the test server. */
    const participants = WorkflowParticipantsDto.fromJS({
        workflowRequestId: 6,
        requestNumber: '6',
        workflowStep: 3,
        requestStatus: 0,
        currentApprovalRequire: ApprovalRequire.Single,
        approvers: [
            {
                person: { employeeId: 1005, code: '1005', nameEn: 'Dummy Amir Hassan' },
                workflowStep: 3,
                isApproved: false,
                origin: WorkflowApproverOrigin.CurrentStep,
                isNotRequired: false,
                approvalRequire: ApprovalRequire.Single,
                delegations: [
                    {
                        person: { employeeId: 1005, code: '1005', nameEn: 'Dummy Amir Hassan' },
                        delegatedBy: { employeeId: 1003, code: '1003', nameEn: 'Dummy Alex Morgan' },
                        relation: WorkflowDelegationRelation.ApproverIsStandingIn,
                        startDate: DateTime.fromISO('2026-09-01').toISO(),
                        endDate: DateTime.fromISO('2026-10-31').toISO(),
                    },
                ],
            },
        ],
        delegates: [],
        readLog: [],
    });

    beforeEach(async () => {
        // The localization source is loaded from the server at startup, so it is absent here and `l()`
        // falls back to returning the key -- which silently drops the name the label is built around.
        // Registering just the two strings under test keeps the assertion about what the panel renders
        // rather than about what the test harness failed to load.
        const abpGlobal = (window as any).abp;
        abpGlobal.localization = abpGlobal.localization ?? {};
        abpGlobal.localization.values = abpGlobal.localization.values ?? {};
        abpGlobal.localization.values['HCMv3'] = {
            ...(abpGlobal.localization.values['HCMv3'] ?? {}),
            OnThisRequestInPlaceOf: 'On this request in place of {0}',
            CoverFor: 'Cover for {0}',
        };

        TestBed.configureTestingModule({
            imports: [WorkflowParticipantsPanelComponent],
            schemas: [NO_ERRORS_SCHEMA],
            providers: [
                provideZonelessChangeDetection(),
                importProvidersFrom(ModalModule.forRoot()),
                provideRouter([]),
                provideHttpClient(),
                provideAnimations(),
                ...SERVICE_PROXY_PROVIDERS,
                { provide: API_BASE_URL, useValue: 'https://localhost:44301' },
                { provide: LOCALE_ID, useValue: 'en' },
            ],
        });

        await TestBed.compileComponents();

        fixture = TestBed.createComponent(WorkflowParticipantsPanelComponent);

        const activityService = fixture.debugElement.injector.get(WorkflowActivityServiceProxy);
        spyOn(activityService, 'getParticipants').and.returnValue(of(participants));

        fixture.componentRef.setInput('workflowRequestId', 6);
        fixture.detectChanges();
    });

    it('names the colleague whose authority the approver is using, on the approver row', () => {
        const text = fixture.nativeElement.textContent as string;

        expect(text).toContain('Dummy Amir Hassan');
        expect(text).toContain('Dummy Alex Morgan');
    });

    it('says how long the cover runs', () => {
        const text = fixture.nativeElement.textContent as string;

        // Formatted by the component, so assert on the parts rather than on one locale's punctuation.
        expect(text).toContain('2026');
        expect(text).toMatch(/Sep|09/);
        expect(text).toMatch(/Oct|10/);
    });

    it('offers no separate Delegates tab', () => {
        const tabs = Array.from(fixture.nativeElement.querySelectorAll('.nav-link')).map((t: any) =>
            (t.textContent || '').trim()
        );

        expect(tabs.some((t) => t.toLowerCase().includes('delegate'))).toBeFalse();
    });
});
