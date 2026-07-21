import { TestBed, ComponentFixture } from '@angular/core/testing';
import { NO_ERRORS_SCHEMA, LOCALE_ID, provideZonelessChangeDetection, importProvidersFrom } from '@angular/core';
import { RolesComponent } from './roles.component';
import { RoleServiceProxy, ListResultDtoOfRoleListDto, API_BASE_URL } from '@shared/service-proxies/service-proxies';
import { Observable, Observer } from 'rxjs';
import { provideRouter } from '@angular/router';
import { PermissionTreeModalComponent } from '../shared/permission-tree-modal.component';
import { ModalModule } from 'ngx-bootstrap/modal';
import { SERVICE_PROXY_PROVIDERS } from '@shared/service-proxies/service-proxy.providers';
import { provideHttpClient } from '@angular/common/http';
import { provideAnimations } from '@angular/platform-browser/animations';

export function getRemoteServiceBaseUrl(): string {
    return 'https://localhost:44301';
}

describe('RolesComponent', () => {
    let fixture: ComponentFixture<RolesComponent>;

    beforeEach(async () => {
        TestBed.configureTestingModule({
            imports: [RolesComponent, PermissionTreeModalComponent],
            schemas: [NO_ERRORS_SCHEMA],
            providers: [
                provideZonelessChangeDetection(),
                importProvidersFrom(ModalModule.forRoot()),
                provideRouter([]),
                provideHttpClient(),
                provideAnimations(),
                ...SERVICE_PROXY_PROVIDERS,
                { provide: API_BASE_URL, useValue: getRemoteServiceBaseUrl() },
                { provide: LOCALE_ID, useValue: 'en' },
            ],
        });

        await TestBed.compileComponents();

        fixture = TestBed.createComponent(RolesComponent);

        let _roleService = fixture.debugElement.injector.get(RoleServiceProxy);

        spyOn(_roleService, 'getRoles').and.returnValue(
            Observable.create((observer: Observer<ListResultDtoOfRoleListDto>) => {
                let list = ListResultDtoOfRoleListDto.fromJS(
                    JSON.parse(
                        '{"items":[{"name":"Admin","displayName":"Admin","isStatic":true,"isDefault":true,"creationTime":"2019-08-22T09:39:10.227975","id":1},{"name":"test","displayName":"test","isStatic":false,"isDefault":false,"creationTime":"2019-08-22T17:19:55.3166397","id":8}]}'
                    )
                );
                observer.next(list);
                return observer;
            })
        );
    });

    it('should primengTableHelper has two records', () => {
        let component = fixture.debugElement.componentInstance as RolesComponent;
        component.getRoles();

        let helper = component.primengTableHelper;

        expect(helper.records()).not.toBe(undefined);
        expect(helper.records().length).toBe(2);
    });
});
