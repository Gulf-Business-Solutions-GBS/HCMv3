import { MyAssetComponent } from './my-asset/my-asset.component';
import { MyExitPermitComponent } from './my-exit-permit/my-exit-permit.component';
import { MyLoanComponent } from './my-loan/my-loan.component';

/**
 * Guards the shape of {@link MyRequestComponentBase}, which about twenty self-service screens extend.
 *
 * <p>WHY THIS EXISTS. The base was briefly decorated `@Directive()` so that a `viewChild` on it would
 * compile. It compiled perfectly and then threw NG0903 -- "Directives cannot inherit Components" -- on
 * every one of those screens the moment somebody opened one, because `AppComponentBase` is a `@Component`.
 * Angular resolves the inheritance chain when the definition is first read, which is at runtime and not at
 * build time, so nothing in the build said a word about it.</p>
 *
 * <p>Reading `ɵcmp` is what forces that resolution. No TestBed, no DI and no HTTP: the failure happens
 * while the definition is being built, long before anything would be rendered, so a spec that merely
 * touches the definition catches it -- and does so without needing a signed-in employee, which is what
 * made this screen family hard to exercise in the first place.</p>
 */
describe('MyRequestComponentBase inheritance', () => {
    const screens: { name: string; type: unknown }[] = [
        { name: 'MyAssetComponent', type: MyAssetComponent },
        { name: 'MyExitPermitComponent', type: MyExitPermitComponent },
        { name: 'MyLoanComponent', type: MyLoanComponent },
    ];

    for (const screen of screens) {
        it(`${screen.name} resolves its component definition`, () => {
            // Throws NG0903 if the base is ever made a directive again.
            const definition = (screen.type as { ɵcmp?: unknown }).ɵcmp;

            expect(definition).toBeDefined();
        });
    }
});
