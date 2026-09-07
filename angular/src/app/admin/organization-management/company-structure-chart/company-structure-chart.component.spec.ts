import { TestBed, ComponentFixture } from '@angular/core/testing';
import { LOCALE_ID, provideZonelessChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideAnimations } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { SERVICE_PROXY_PROVIDERS } from '@shared/service-proxies/service-proxy.providers';
import {
    API_BASE_URL,
    CompanyStructureChartFilterOptionsDto,
    CompanyStructureChartServiceProxy,
    CompanyStructureNodeDto,
} from '@shared/service-proxies/service-proxies';
import { CompanyStructureChartComponent } from './company-structure-chart.component';

export function getRemoteServiceBaseUrl(): string {
    return 'https://localhost:44301';
}

/**
 * Renders the Company Structure Overview against a stubbed service proxy.
 *
 * These assertions are on the real DOM rather than on component state: the point of this screen is the
 * card layout, so a test that only checked the signals would pass even if the cards never rendered.
 */
describe('CompanyStructureChartComponent', () => {
    let fixture: ComponentFixture<CompanyStructureChartComponent>;
    let service: CompanyStructureChartServiceProxy;

    const filterOptionsJson = {
        roots: [{ id: 1, treeCode: '00001', code: '1001', nameEn: 'ALI ABDULWAHAB AL MUTAWA', nameAr: 'المطوع' }],
        organizationUnitTypes: [
            { id: 10, code: 'Level1', nameEn: 'Legal Entity', nameAr: 'الكيان القانوني' },
            { id: 11, code: 'Level2', nameEn: 'Department', nameAr: 'إدارة' },
        ],
    };

    const nodeJson = {
        asOfDate: '2026-09-06T00:00:00',
        focus: {
            id: 1,
            treeCode: '00001',
            code: '1001',
            nameEn: 'ALI ABDULWAHAB AL MUTAWA',
            nameAr: 'المطوع',
            parentId: null,
            organizationUnitTypeId: 10,
            organizationUnitTypeCode: 'Level1',
            organizationUnitTypeNameEn: 'Legal Entity',
            organizationUnitTypeNameAr: 'الكيان القانوني',
            directEmployeeCount: 2443,
            totalEmployeeCount: 2945,
            childUnitCount: 18,
        },
        ancestors: [],
        children: [
            {
                id: 2,
                treeCode: '00001.00001',
                code: '2001',
                nameEn: 'Consumer Goods',
                nameAr: 'السلع الاستهلاكية',
                parentId: 1,
                organizationUnitTypeId: 11,
                organizationUnitTypeCode: 'Level2',
                organizationUnitTypeNameEn: 'Department',
                organizationUnitTypeNameAr: 'إدارة',
                directEmployeeCount: 841,
                totalEmployeeCount: 954,
                childUnitCount: 53,
            },
            {
                id: 3,
                treeCode: '00001.00002',
                code: '2008',
                nameEn: 'Human Resources and Administration',
                nameAr: 'الموارد البشرية',
                parentId: 1,
                organizationUnitTypeId: 11,
                organizationUnitTypeCode: 'Level2',
                organizationUnitTypeNameEn: 'Department',
                organizationUnitTypeNameAr: 'إدارة',
                directEmployeeCount: 64,
                totalEmployeeCount: 73,
                childUnitCount: 0,
            },
        ],
    };

    beforeEach(async () => {
        // abp.js ships the namespaces but the language is filled in from AbpUserConfiguration at runtime,
        // which never happens under Karma -- so stand it up here the way a signed-in session would.
        (window as any).abp = (window as any).abp || {};
        (window as any).abp.localization = (window as any).abp.localization || {};
        (window as any).abp.localization.currentLanguage = { name: 'en' };

        TestBed.configureTestingModule({
            imports: [CompanyStructureChartComponent],
            providers: [
                provideZonelessChangeDetection(),
                provideRouter([]),
                provideHttpClient(),
                provideAnimations(),
                ...SERVICE_PROXY_PROVIDERS,
                { provide: API_BASE_URL, useValue: getRemoteServiceBaseUrl() },
                { provide: LOCALE_ID, useValue: 'en' },
            ],
        });

        await TestBed.compileComponents();

        fixture = TestBed.createComponent(CompanyStructureChartComponent);
        service = fixture.debugElement.injector.get(CompanyStructureChartServiceProxy);

        spyOn(service, 'getFilterOptions').and.returnValue(
            of(CompanyStructureChartFilterOptionsDto.fromJS(filterOptionsJson))
        );
        spyOn(service, 'getNode').and.returnValue(of(CompanyStructureNodeDto.fromJS(nodeJson)));
        spyOn(service, 'searchNodes').and.returnValue(of([]));
    });

    function cards(): HTMLElement[] {
        return Array.from(fixture.nativeElement.querySelectorAll('.org-chart-card'));
    }

    it('should render the focus card and one card per child', async () => {
        fixture.detectChanges();
        await fixture.whenStable();
        fixture.detectChanges();

        // The focus card plus its two children.
        expect(cards().length).toBe(3);

        const focus = fixture.nativeElement.querySelector('.org-chart-card--focused');
        expect(focus).toBeTruthy();
        expect(focus.textContent).toContain('Legal Entity');
        expect(focus.textContent).toContain('1001');
        expect(focus.textContent).toContain('ALI ABDULWAHAB AL MUTAWA');
    });

    it('should show direct and total headcounts and the child-unit strip', async () => {
        fixture.detectChanges();
        await fixture.whenStable();
        fixture.detectChanges();

        const focus = fixture.nativeElement.querySelector('.org-chart-card--focused');
        const metrics = Array.from(focus.querySelectorAll('.org-chart-card__metric-value')).map(
            (element) => (element as HTMLElement).textContent.trim()
        );

        expect(metrics).toEqual(['2443', '2945']);
        expect(focus.querySelector('.org-chart-card__strip').textContent.trim()).toBe('18');
    });

    it('should hide the strip on a child with no sub-units', async () => {
        fixture.detectChanges();
        await fixture.whenStable();
        fixture.detectChanges();

        const childCards = cards().filter((card) => !card.classList.contains('org-chart-card--focused'));
        const consumerGoods = childCards.find((card) => card.textContent.includes('Consumer Goods'));
        const humanResources = childCards.find((card) => card.textContent.includes('Human Resources'));

        expect(consumerGoods.querySelector('.org-chart-card__strip').textContent.trim()).toBe('53');
        expect(humanResources.querySelector('.org-chart-card__strip')).toBeNull();
    });

    it('should drill into the clicked child', async () => {
        fixture.detectChanges();
        await fixture.whenStable();
        fixture.detectChanges();

        (service.getNode as jasmine.Spy).calls.reset();

        const consumerGoods = cards().find(
            (card) =>
                !card.classList.contains('org-chart-card--focused') &&
                card.textContent.includes('Consumer Goods')
        );
        consumerGoods.click();

        const input = (service.getNode as jasmine.Spy).calls.mostRecent().args[0];
        expect(input.organizationUnitId).toBe(2);
    });

    it('should colour each card by its organization unit type', async () => {
        fixture.detectChanges();
        await fixture.whenStable();
        fixture.detectChanges();

        const focus = fixture.nativeElement.querySelector('.org-chart-card--focused') as HTMLElement;
        const child = cards().find(
            (card) => !card.classList.contains('org-chart-card--focused')
        ) as HTMLElement;

        // Level1 and Level2 are different accents, which is what makes the levels readable at a glance.
        expect(focus.style.getPropertyValue('--org-chart-accent')).toBe('#7239ea');
        expect(child.style.getPropertyValue('--org-chart-accent')).toBe('#009ef7');
    });

    it('should render the breadcrumb for a nested focus node', async () => {
        (service.getNode as jasmine.Spy).and.returnValue(
            of(
                CompanyStructureNodeDto.fromJS({
                    ...nodeJson,
                    focus: { ...nodeJson.children[0], childUnitCount: 0 },
                    ancestors: [{ id: 1, treeCode: '00001', code: '1001', nameEn: 'ALI ABDULWAHAB AL MUTAWA' }],
                    children: [],
                })
            )
        );

        fixture.detectChanges();
        await fixture.whenStable();
        fixture.detectChanges();

        const breadcrumb = fixture.nativeElement.querySelector('.org-chart-breadcrumb');
        expect(breadcrumb).toBeTruthy();
        expect(breadcrumb.textContent).toContain('ALI ABDULWAHAB AL MUTAWA');
        expect(breadcrumb.textContent).toContain('Consumer Goods');
        expect(cards().length).toBe(1);
    });

    it('should show the empty state when the tenant has no organization units', async () => {
        (service.getNode as jasmine.Spy).and.returnValue(
            of(CompanyStructureNodeDto.fromJS({ asOfDate: '2026-09-06T00:00:00', ancestors: [], children: [] }))
        );

        fixture.detectChanges();
        await fixture.whenStable();
        fixture.detectChanges();

        expect(cards().length).toBe(0);
        expect(fixture.nativeElement.querySelector('.org-chart-explorer__empty')).toBeTruthy();
    });
});
