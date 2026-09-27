import { TestBed } from '@angular/core/testing';
import { PermissionCheckerService } from 'abp-ng2-module';
import { DirectEntryServiceProxy } from '@shared/service-proxies/service-proxies';
import { of, throwError } from 'rxjs';
import { RecordDirectlyService, RecordWithoutApprovalPermission } from './record-directly.service';

/**
 * What matters here is what the user is OFFERED, because the offer is a promise: a form showing "record
 * without approval" on a type the server will not bypass wastes the decision, and the user only finds out
 * after they have made it.
 *
 * <p>So these cover the three things that decide the offer -- the permission, the catalog, and the fact
 * that thirteen forms asking the same question must not become thirteen requests.</p>
 */
describe('RecordDirectlyService', () => {
    let granted: boolean;
    let calls: number;
    let codes: string[];

    function build(): RecordDirectlyService {
        granted = true;
        calls = 0;
        codes = ['EmployeeAsset', 'EmployeeLoan'];

        TestBed.configureTestingModule({
            providers: [
                RecordDirectlyService,
                {
                    provide: PermissionCheckerService,
                    useValue: { isGranted: (name: string) => granted && name === RecordWithoutApprovalPermission },
                },
                {
                    provide: DirectEntryServiceProxy,
                    useValue: {
                        getDirectEntryRequestTypes: () => {
                            calls++;
                            return of(codes);
                        },
                    },
                },
            ],
        });

        return TestBed.inject(RecordDirectlyService);
    }

    it('says yes for a type the catalog allows', (done) => {
        build()
            .canRecordDirectly('EmployeeAsset')
            .subscribe((can) => {
                expect(can).toBe(true);
                done();
            });
    });

    it('says no for a type the catalog does not allow', (done) => {
        build()
            .canRecordDirectly('EmployeeTicket')
            .subscribe((can) => {
                expect(can).toBe(false);
                done();
            });
    });

    // Without the permission the endpoint would be refused, and an error on every form opening is worse
    // than a question never asked.
    it('says no without the permission, and does not call the server at all', (done) => {
        const service = build();
        granted = false;

        service.canRecordDirectly('EmployeeAsset').subscribe((can) => {
            expect(can).toBe(false);
            expect(calls).toBe(0);
            done();
        });
    });

    it('asks the server once however many forms ask it', (done) => {
        const service = build();

        service.canRecordDirectly('EmployeeAsset').subscribe(() => {
            service.canRecordDirectly('EmployeeLoan').subscribe(() => {
                service.canRecordDirectly('EmployeeTicket').subscribe(() => {
                    expect(calls).toBe(1);
                    done();
                });
            });
        });
    });

    // A failure here must not stop a form opening. The choice simply is not offered, and the save routes
    // for approval exactly as it did before -- the safe side of this fence.
    it('says no, rather than throwing, when the lookup fails', (done) => {
        TestBed.resetTestingModule();
        TestBed.configureTestingModule({
            providers: [
                RecordDirectlyService,
                { provide: PermissionCheckerService, useValue: { isGranted: () => true } },
                {
                    provide: DirectEntryServiceProxy,
                    useValue: {
                        getDirectEntryRequestTypes: () => throwError(() => new Error('boom')),
                    },
                },
            ],
        });

        const service = TestBed.inject(RecordDirectlyService);

        service.canRecordDirectly('EmployeeAsset').subscribe({
            next: (can) => {
                expect(can).toBe(false);
                done();
            },
            error: () => fail('the lookup failing must not reach the form'),
        });
    });
});
