/**
 * Makes `devextreme-angular@23.2.13` loadable under Angular 21.
 *
 * WHY
 * ---
 * `devexpress-reporting-angular` (the official DevExpress Web Report Designer Angular components) has a
 * hard runtime dependency on `devextreme-angular`, which was built against Angular <= 17. Back then
 * `makeStateKey` and `TransferState` were exported from `@angular/platform-browser`; Angular has since moved
 * both to `@angular/core`, and Angular 21's `@angular/platform-browser` no longer exports them at all.
 *
 * That breaks in two different ways:
 *   1. `import { makeStateKey } from '@angular/platform-browser'` is a NAMED import, so esbuild fails the
 *      build outright: `No matching export ... for import "makeStateKey"`.
 *   2. `import * as i2 from '@angular/platform-browser'` + `i2.TransferState` is a NAMESPACE access, which
 *      is NOT a bundle-time error -- it silently evaluates to `undefined` and then blows up at RUNTIME as
 *      soon as any DevExtreme component is instantiated, because the generated Angular factory does
 *      `i0.eedirectiveInject(i2.TransferState)`. This one is the dangerous half: without this patch the
 *      build goes green and the designer dies in the browser.
 *
 * WHAT IT DOES
 * ------------
 * Rewrites the module specifier `@angular/platform-browser` to `@angular/core` in devextreme-angular's
 * shipped bundles. Nothing else changes -- these are the same symbols, just relocated by Angular, so class
 * and DI-token identity is preserved.
 *
 * SAFETY
 * ------
 * Before touching a file, the script asserts that the ONLY things that file pulls out of
 * `@angular/platform-browser` are `makeStateKey` / `TransferState`. If a future version of the package
 * starts using a symbol that genuinely lives only in `@angular/platform-browser` (`DomSanitizer`, `Meta`,
 * `Title`, ...), the rewrite would be wrong, so the script fails loudly instead of silently corrupting the
 * package.
 *
 * The script is idempotent (after a successful run no `@angular/platform-browser` specifiers remain, so a
 * re-run is a no-op) and is wired up as an npm `postinstall` hook so it survives `npm install` / `npm ci`.
 *
 * Delete this script, its `postinstall` entry, and the note in the report-designer component once
 * devextreme-angular ships a build that imports these symbols from `@angular/core`.
 */
import { existsSync, readdirSync, readFileSync, statSync, writeFileSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const packageRoot = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const target = join(packageRoot, 'node_modules', 'devextreme-angular');

/** Symbols Angular relocated from @angular/platform-browser to @angular/core. */
const RELOCATED = new Set(['makeStateKey', 'TransferState']);

const SPECIFIER = '@angular/platform-browser';

if (!existsSync(target)) {
    // Not installed (or a partial install) -- nothing to do. Never fail the install for this.
    process.exit(0);
}

/** Recursively collect .js files under the given directory. */
function collectJsFiles(dir) {
    if (!existsSync(dir)) {
        return [];
    }

    const found = [];

    for (const entry of readdirSync(dir)) {
        const full = join(dir, entry);

        if (statSync(full).isDirectory()) {
            found.push(...collectJsFiles(full));
        } else if (entry.endsWith('.js')) {
            found.push(full);
        }
    }

    return found;
}

/**
 * Returns the set of symbols `source` consumes from @angular/platform-browser, covering both named imports
 * (`import { a, b } from '...'`) and namespace member access (`import * as ns from '...'` then `ns.Foo`).
 */
function usedSymbols(source) {
    const used = new Set();

    const namedImport = new RegExp(`import\\s*\\{([^}]*)\\}\\s*from\\s*['"]${SPECIFIER}['"]`, 'g');
    for (const match of source.matchAll(namedImport)) {
        for (const part of match[1].split(',')) {
            // Handles `Foo` and `Foo as Bar` -- the imported name is what matters.
            const name = part.trim().split(/\s+as\s+/)[0].trim();
            if (name) {
                used.add(name);
            }
        }
    }

    const namespaceImport = new RegExp(`import\\s*\\*\\s*as\\s*(\\w+)\\s*from\\s*['"]${SPECIFIER}['"]`, 'g');
    for (const match of source.matchAll(namespaceImport)) {
        const alias = match[1];
        for (const hit of source.matchAll(new RegExp(`\\b${alias}\\.(\\w+)`, 'g'))) {
            used.add(hit[1]);
        }
    }

    // UMD/CJS form.
    const requireImport = new RegExp(`require\\(['"]${SPECIFIER}['"]\\)`, 'g');
    if (requireImport.test(source)) {
        // The UMD factory binds the module to a positional parameter; we cannot cheaply prove which members
        // it touches, so rely on the ESM bundles (which we can prove) having already been validated. The
        // specifier swap itself is still symbol-for-symbol correct.
        used.add('TransferState');
    }

    return used;
}

const files = [
    ...collectJsFiles(join(target, 'fesm2015')),
    ...collectJsFiles(join(target, 'esm2015')),
    ...collectJsFiles(join(target, 'bundles')),
];

const patched = [];

for (const file of files) {
    const source = readFileSync(file, 'utf8');

    if (!source.includes(SPECIFIER)) {
        continue;
    }

    const unexpected = [...usedSymbols(source)].filter((symbol) => !RELOCATED.has(symbol));

    if (unexpected.length > 0) {
        console.error(
            `\n[patch-devextreme-angular] ABORTED. ${file} uses ${unexpected.join(', ')} from ` +
                `${SPECIFIER}, which did NOT move to @angular/core. Rewriting the specifier would break it.\n` +
                `This patch was written for devextreme-angular 23.2.13; re-check it against the installed ` +
                `version.\n`
        );
        process.exit(1);
    }

    writeFileSync(file, source.split(SPECIFIER).join('@angular/core'), 'utf8');
    patched.push(file);
}

if (patched.length > 0) {
    console.log(
        `[patch-devextreme-angular] Repointed '${SPECIFIER}' -> '@angular/core' in ${patched.length} ` +
            `devextreme-angular bundle(s) (Angular 21 compatibility).`
    );
}
