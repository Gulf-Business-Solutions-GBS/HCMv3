#!/usr/bin/env node
/**
 * HCMv3 help-content tooling. Phase 0: inventory, stubs, coverage.
 * See docs/help/DESIGN.md.
 *
 * Dependency-free by design -- no package.json, no npm install, runs on the Node that
 * already ships with the Angular toolchain.
 *
 *   node tools/help/help-tool.mjs extract    field inventory from templates + entity
 *   node tools/help/help-tool.mjs stubs      skeleton markdown (creates or merges)
 *   node tools/help/help-tool.mjs coverage   three-bucket report (DESIGN.md §12)
 */

import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');
const REGISTRY = 'docs/help/pages.json';
const OUT_DIR = 'artifacts/help';
const CONTENT_DIR = 'docs/help';
const LANGS = ['en', 'ar'];

const abs = (rel) => path.join(ROOT, rel);
// Line endings normalised to LF: with core.autocrlf=true the Markdown is checked out CRLF, and the
// `\n`-anchored patterns below (the `## Overview` split, the section split) would silently miss.
const read = (rel) => fs.readFileSync(abs(rel), 'utf8').replace(/\r\n?/g, '\n');
const exists = (rel) => fs.existsSync(abs(rel));

// ---------------------------------------------------------------------------------------------
// C# entity parsing
// ---------------------------------------------------------------------------------------------

const PRIMITIVES = new Set([
    'string', 'int', 'long', 'short', 'byte', 'bool', 'decimal', 'double', 'float',
    'DateTime', 'DateTimeOffset', 'TimeSpan', 'Guid', 'byte[]',
]);

/**
 * Properties of an entity, with the attributes that precede them.
 *
 * A property is treated as an EF NAVIGATION -- and so excluded from help -- when a sibling
 * property named `${name}Id` exists. That rule holds for every lookup on Employee (Title/TitleId,
 * Nationality/NationalityId, BirthPlace/BirthPlaceId) and correctly leaves enum-typed scalars
 * like Gender alone, since there is no GenderId.
 */
function parseEntity(file) {
    const src = read(file);
    const props = new Map();
    let attrs = [];

    for (const raw of src.split(/\r?\n/)) {
        const line = raw.trim();
        if (line.startsWith('[')) {
            attrs.push(line);
            continue;
        }
        const m = line.match(
            /^public\s+(?:virtual\s+)?([A-Za-z0-9_<>?.[\]]+)\s+([A-Za-z0-9_]+)\s*\{\s*get;\s*set;\s*\}/
        );
        if (m) {
            // `System.DateTime` and `DateTime` are the same type; some entities spell it out.
            props.set(m[2], { name: m[2], type: m[1].replace(/^System\./, ''), attrs: attrs.slice() });
            attrs = [];
            continue;
        }
        // Doc comments, blank lines, #region markers sit between the attribute block and the
        // property in this codebase, so they must not clear pending attributes.
        const isFiller = line === '' || line.startsWith('///') || line.startsWith('//') ||
            line.startsWith('*') || line.startsWith('#');
        if (!isFiller) attrs = [];
    }

    for (const p of props.values()) {
        p.isNavigation = props.has(`${p.name}Id`);
        p.isNullable = p.type.endsWith('?');
        const bare = p.type.replace(/\?$/, '');
        p.bareType = bare;
        p.isEnumCandidate = !PRIMITIVES.has(bare) && !p.isNavigation;
        // A foreign key is an int to the database but a dropdown to the reader, so the Rules block
        // names the master-data list rather than calling it a whole number.
        const navBase = p.name.endsWith('Id') ? p.name.slice(0, -2) : null;
        p.lookupTarget = navBase && props.has(navBase) ? props.get(navBase).type : null;
        // Most entities follow the flat-FK convention -- `StateId` with no `State` navigation --
        // so the list cannot be named, but it is still a choice and not a number to type.
        p.isFkWithoutNav = !p.lookupTarget && /[a-z]Id$/.test(p.name) &&
            ['int', 'long'].includes(bare) && p.name !== 'TenantId';
    }
    return props;
}

/** Cheap enum lookup: a type named Gender lives in a file named Gender.cs. */
let csFileIndex = null;
function indexCsFiles() {
    if (csFileIndex) return csFileIndex;
    csFileIndex = new Map();
    const walk = (dir) => {
        let entries;
        try {
            entries = fs.readdirSync(dir, { withFileTypes: true });
        } catch {
            return;
        }
        for (const e of entries) {
            if (e.isDirectory()) {
                if (['bin', 'obj', 'node_modules', '.git'].includes(e.name) || e.name.startsWith('bin_')) continue;
                walk(path.join(dir, e.name));
            } else if (e.name.endsWith('.cs')) {
                const key = e.name.slice(0, -3);
                if (!csFileIndex.has(key)) csFileIndex.set(key, path.join(dir, e.name));
            }
        }
    };
    walk(abs('aspnet-core/src'));
    return csFileIndex;
}

function enumMembers(typeName) {
    const file = indexCsFiles().get(typeName);
    if (!file) return null;
    let src;
    try {
        src = fs.readFileSync(file, 'utf8');
    } catch {
        return null;
    }
    const block = src.match(new RegExp(`enum\\s+${typeName}\\s*(?::[^{]+)?\\{([\\s\\S]*?)\\n\\}`));
    if (!block) return null;
    const members = [];
    for (const raw of block[1].split(/\r?\n/)) {
        const line = raw.trim();
        if (!line || line.startsWith('//') || line.startsWith('[') || line.startsWith('/*')) continue;
        const m = line.match(/^([A-Za-z_][A-Za-z0-9_]*)\s*(?:=\s*[^,]+)?,?$/);
        if (m) members.push(m[1]);
    }
    return members.length ? members : null;
}

/**
 * What an entity's application service enforces on save, read from its source.
 *
 * Only the entity's OWN service is consulted -- `CitiesAppService` for City -- so a rule found in a
 * service that manages several entities is never credited to the wrong one; when no service of
 * that name exists, nothing is claimed. The patterns are the ones the whole codebase uses: 86
 * services refuse duplicates with DuplicateCodeFound, 76 with NameAlreadyInUse. A simple-lookup
 * service inherits all three behaviours from SimpleLookupAppServiceBase.
 */
let appServiceIndex = null;
function serviceBehaviour(entityName, serviceOverride) {
    if (!appServiceIndex) {
        appServiceIndex = new Map();
        const walk = (dir) => {
            for (const e of fs.readdirSync(dir, { withFileTypes: true })) {
                if (e.isDirectory()) {
                    if (!['bin', 'obj'].includes(e.name)) walk(path.join(dir, e.name));
                } else if (/(AppService(Base)?|Validator)\.cs$/.test(e.name)) {
                    appServiceIndex.set(e.name.slice(0, -3), path.join(dir, e.name));
                }
            }
        };
        walk(abs('aspnet-core/src/HCMv3.Application'));
        // Domain validators an app service injects (e.g. EmployeeValidator) hold save-time checks too.
        walk(abs('aspnet-core/src/HCMv3.Core'));
    }
    const name = serviceOverride ?? `${pluralKey(entityName)}AppService`;
    const file = appServiceIndex.get(name);
    if (!file) return null;
    let text = fs.readFileSync(file, 'utf8');
    if (/SimpleLookupAppServiceBase</.test(text) && appServiceIndex.has('SimpleLookupAppServiceBase')) {
        text += fs.readFileSync(appServiceIndex.get('SimpleLookupAppServiceBase'), 'utf8');
    }
    // A check moved into an injected Core validator still applies to this screen, so read it with the service.
    for (const [, validator] of text.matchAll(/\bI(\w+Validator)\s+_\w+;/g)) {
        if (appServiceIndex.has(validator)) text += fs.readFileSync(appServiceIndex.get(validator), 'utf8');
    }
    return {
        service: name,
        autoCode: /AssignCodeIfEmpty/.test(text), // private helper or the shared IEmployeeCodeGenerator
        uniqueCode: /L\("DuplicateCodeFound"\)/.test(text),
        // The name check refuses a record whose English OR Arabic name matches another's, so
        // each of the two names must be unique on its own.
        uniqueName: /L\("NameAlreadyInUse"\)/.test(text),
        endAfterStart: /L\("EndDateCannotBeEarlierThanStartDate"\)/.test(text),
        noOverlap: queryScope(text, /L\("OverlapConflictFound"\)/),
        uniqueStart: queryScope(text, /L\("StartDateAlreadyExists"/),
        // Effective-dated history: each record ends the day before the next one starts.
        chained: /InfiniteEndDate/.test(text) ? queryScope(text, /\.StartDate\.AddDays\(-1\)/) : null,
    };
}

/**
 * Which records a check compares against, read from the query nearest above `anchor`: the
 * `x.SomethingId == ...` conditions in it. EmployeeDependants refuses an overlap only for the same
 * employee AND ticket type; stating "another record" there would be false, so the scope is named.
 * Returns null when the anchor is absent, and the scope list (possibly empty) when present.
 */
function queryScope(text, anchor) {
    const lines = text.split(/\r?\n/);
    const at = lines.findIndex((l) => anchor.test(l));
    if (at < 0) return null;
    let from = at;
    for (let i = at; i >= Math.max(0, at - 30); i--) {
        // A query with a condition -- `records.Skip(i + 1).FirstOrDefault()` is not one.
        if (/\.(Where|AnyAsync|Any|FirstOrDefaultAsync|FirstOrDefault|CountAsync)\(\s*\w+\s*=>/.test(lines[i])) {
            from = i;
            break;
        }
    }
    const scope = new Set();
    for (const l of lines.slice(from, at + 1)) {
        for (const m of l.matchAll(/\b\w+\.(\w+Id)\s*==\s*[\w.]+/g)) {
            if (!['Id', 'TenantId'].includes(m[1])) scope.add(m[1]);
        }
    }
    return [...scope];
}

/**
 * The app's own translations, so Arabic rules name choices and master-data lists exactly as the
 * screen does ("مفعّل", "الجنسيات") instead of the help inventing its own words for them.
 */
const locTexts = {};
function locText(lang, key) {
    if (!locTexts[lang]) {
        const file = lang === 'en' ? 'HCMv3.xml' : `HCMv3-${lang}.xml`;
        const xml = read(`aspnet-core/src/HCMv3.Core/Localization/HCMv3/${file}`);
        // Decoded, or "Full &amp; Final" reaches a page title literally.
        const decode = (s) => s.replace(/&lt;/g, '<').replace(/&gt;/g, '>').replace(/&quot;/g, '"')
            .replace(/&apos;/g, "'").replace(/&amp;/g, '&');
        locTexts[lang] = new Map([...xml.matchAll(/<text name="([^"]+)">([^<]*)<\/text>/g)].map((m) => [m[1], decode(m[2])]));
    }
    return locTexts[lang].get(key);
}
const arText = (key) => locText('ar', key);

/** Title -> Titles, City -> Cities, MaritalStatus -> MaritalStatuses: the master-data menu keys. */
const pluralKey = (type) =>
    /[^aeiou]y$/.test(type) ? `${type.slice(0, -1)}ies` : /s$/.test(type) ? `${type}es` : `${type}s`;

/**
 * "Character" after a number, by Arabic counting rules, which follow the last two digits:
 * 3-10 take the plural (105 أحرف), 11-99 the accusative singular (128 حرفاً), and round
 * hundreds and thousands the genitive singular (1000 حرف).
 */
function arCountedNoun(n) {
    const t = n % 100;
    if (t === 0) return 'حرف';
    if (t >= 3 && t <= 10) return 'أحرف';
    return 'حرفاً';
}

/**
 * "the same employee and ticket type" / "للموظف نفسه ونوع التذكرة نفسه" from a check's scope ids.
 * Names come from the app's own strings, so they read as the screen labels them.
 */
function scopeWords(lang, scope) {
    const names = (scope.length ? scope : ['EmployeeId']).map((id) => {
        const key = id.replace(/Id$/, '');
        if (key === 'Employee') return lang === 'ar' ? 'الموظف' : 'employee';
        const text = locText(lang, key) ?? key.replace(/([a-z])([A-Z])/g, '$1 $2');
        return lang === 'ar' ? text : text.toLowerCase();
    });
    // لـ + الموظف is written للموظف; before any other word it is simply joined: لنوع.
    if (lang === 'ar') return `ل${names.map((n) => `${n} نفسه`).join(' و')}`.replace(/^لال/, 'لل');
    return `the same ${names.join(' and ')}`;
}

/** Every rule phrase, per language. English is the original wording, kept byte-for-byte. */
const RULE_TEXT = {
    en: {
        system: () => 'Set by the system when the record is saved; not entered on the form.',
        required: () => 'Required.',
        requiredOnCreate: () => 'Required when you create the record.',
        optional: () => 'Optional.',
        maxLen: (r) => `Maximum ${r.n} characters.`,
        range: (r) => `Between ${r.a} and ${r.b}.`,
        oneOf: (r) => `One of: ${r.members.join(', ')}.`,
        oneOfKeys: (r) => `One of: ${r.keys.map((k) => locText('en', k) ?? k).join(', ')}.`,
        // The app's own label for the list, as Arabic does: the product renamed Wage Type to Wagetype
        // and a name split from the class would keep saying the old one.
        lookup: (r) => `Chosen from ${locText('en', r.target) ?? r.target.replace(/([a-z])([A-Z])/g, '$1 $2')} master data.`,
        lookupGeneric: () => 'Chosen from a list.',
        lookupEmployee: () => 'Chosen from the employee list.',
        autoCode: () => 'Left blank, the next number is assigned when the record is saved.',
        unique: () => 'Must be unique in this list.',
        date: () => 'Date value.',
        time: () => 'Time of day.',
        whole: () => 'Whole number.',
        number: () => 'Number.',
        yesNo: () => 'Yes or no.',
        endAfterStart: () => 'Cannot be before the start date.',
        noOverlap: (r) => `Its dates cannot overlap another record for ${scopeWords('en', r.scope)}.`,
        uniqueStart: (r) => `No two records for ${scopeWords('en', r.scope)} can start on the same date.`,
        chained: (r) =>
            `Saving closes the previous record for ${scopeWords('en', r.scope)} on the day before this date; ` +
            'the latest record stays open.',
    },
    ar: {
        system: () => 'يحدده النظام عند حفظ السجل، ولا يُدخل في النموذج.',
        required: () => 'إلزامي.',
        requiredOnCreate: () => 'إلزامي عند إنشاء السجل.',
        optional: () => 'اختياري.',
        maxLen: (r) => `الحد الأقصى ${r.n} ${arCountedNoun(Number(r.n))}.`,
        range: (r) => `بين ${r.a} و${r.b}.`,
        oneOf: (r) => `إحدى القيم: ${r.members.map((m) => arText(m) ?? m).join('، ')}.`,
        oneOfKeys: (r) => `إحدى القيم: ${r.keys.map((k) => arText(k) ?? k).join('، ')}.`,
        lookup: (r) => `يُختار من قائمة ${arText(pluralKey(r.target)) ?? r.target}.`,
        lookupGeneric: () => 'يُختار من قائمة.',
        lookupEmployee: () => 'يُختار من قائمة الموظفين.',
        autoCode: () => 'إذا تُرك فارغاً، يُمنح الرقم التالي عند حفظ السجل.',
        unique: () => 'يجب ألا يتكرر في هذه القائمة.',
        date: () => 'تاريخ.',
        time: () => 'وقت من اليوم.',
        whole: () => 'رقم صحيح.',
        number: () => 'رقم.',
        yesNo: () => 'نعم أو لا.',
        endAfterStart: () => 'لا يجوز أن يسبق تاريخ البداية.',
        noOverlap: (r) => `لا يجوز أن تتداخل فترته مع سجل آخر ${scopeWords('ar', r.scope)}.`,
        uniqueStart: (r) => `لا يجوز أن يبدأ سجلان ${scopeWords('ar', r.scope)} في التاريخ نفسه.`,
        chained: (r) =>
            `عند الحفظ يُغلق السجل السابق ${scopeWords('ar', r.scope)} في اليوم السابق لهذا التاريخ، ` +
            'ويبقى أحدث سجل مفتوحاً.',
    },
};

/** The label of the generated Rules line in each language's Markdown. */
const RULES_LABEL = { en: '**Rules.**', ar: '**القواعد.**' };
const RULES_LINE = /^\*\*(?:Rules|القواعد)\.\*\*.*$/m;

/**
 * DESIGN.md §10 -- the Rules block, derived and never hand-written. Decided once as a list of
 * rule parts, then rendered per language, so the Arabic can never state a different rule.
 */
function rulesFor(prop, { uiRequired = false, systemMaintained = false, behaviour = null, formOptions = null, inputProp = null } = {}) {
    if (!prop) return null;
    const parts = [];
    const attrText = prop.attrs.join(' ');

    // Shown on a screen but absent from its edit form: the system writes it (full names,
    // probation end date). "Optional"/"Required" would tell the reader to go and fill it in.
    if (systemMaintained) {
        parts.push({ k: 'system' });
    } else {
        // "Required" must mean the reader cannot save without it. A non-nullable NUMBER is not
        // that: left blank it saves as 0, so Mobile, Civil ID and Probation Days all save empty.
        // Nor is a non-nullable DATE: Asset.ExpiryDate left blank saves as the minimum date. Only
        // a choice always holds a value. Required is therefore [Required], a form label marked
        // required, or a choice.
        const alwaysHasValue = !prop.isNullable && prop.isEnumCandidate && !prop.lookupTarget;
        // A tick box is always either ticked or not -- neither required nor optional.
        if (prop.bareType === 'bool') parts.push({ k: 'yesNo' });
        else if (uiRequired === 'create' && !/\[Required\]/.test(attrText)) parts.push({ k: 'requiredOnCreate' });
        else parts.push({ k: /\[Required\]/.test(attrText) || uiRequired || alwaysHasValue ? 'required' : 'optional' });

        // The input DTO can accept less than the column holds -- the BioTime password is capped at
        // 128 so its ciphertext fits the 256-character column -- and the tighter limit is the one
        // the reader meets.
        const maxLenOf = (text) => Number(text.match(/\[(?:StringLength|MaxLength)\((\d+)\)/)?.[1]) || null;
        const lens = [maxLenOf(attrText), inputProp && maxLenOf(inputProp.attrs.join(' '))].filter(Boolean);
        if (lens.length) parts.push({ k: 'maxLen', n: String(Math.min(...lens)) });

        const range = attrText.match(/\[Range\(([^,]+),\s*([^)]+)\)/);
        if (range) parts.push({ k: 'range', a: range[1].trim(), b: range[2].trim() });

        if (prop.isEnumCandidate) {
            if (formOptions?.length) {
                // What the form offers, by its own localization keys -- shown as the screen shows them.
                parts.push({ k: 'oneOfKeys', keys: formOptions });
            } else {
                // No readable option list: fall back to the enum. There "All" is usually a filter
                // value (EmploymentType.ALL) that no form offers, so it is left out.
                const members = enumMembers(prop.bareType)?.filter((m) => !/^all$/i.test(m));
                if (members?.length) parts.push({ k: 'oneOf', members });
            }
        }

        // The kind of value first, then what the service enforces: "Required. Date value. Cannot
        // be before the start date." reads in the order a person fills the field in.
        // Only reference lists are "master data". An employee is picked from the employee list, and
        // a parent record such as the employee's own basic pay is not master data at all.
        if (prop.lookupTarget === 'Employee') {
            parts.push({ k: 'lookupEmployee' });
        } else if (prop.lookupTarget && /^Employee[A-Z]/.test(prop.lookupTarget)) {
            parts.push({ k: 'lookupGeneric' });
        } else if (prop.lookupTarget) {
            parts.push({ k: 'lookup', target: prop.lookupTarget });
        } else if (prop.isFkWithoutNav) {
            parts.push({ k: 'lookupGeneric' });
        } else if (prop.bareType === 'DateTime') {
            parts.push({ k: 'date' });
        } else if (prop.bareType === 'TimeSpan') {
            parts.push({ k: 'time' });
        } else if ((prop.bareType === 'int' || prop.bareType === 'long') && !prop.isEnumCandidate) {
            parts.push({ k: 'whole' });
        } else if (['decimal', 'double', 'float'].includes(prop.bareType)) {
            parts.push({ k: 'number' });
        }

        if (prop.name === 'EndDate' && behaviour?.endAfterStart) parts.push({ k: 'endAfterStart' });
        if (prop.name === 'StartDate') {
            if (behaviour?.noOverlap) parts.push({ k: 'noOverlap', scope: behaviour.noOverlap });
            if (behaviour?.uniqueStart) parts.push({ k: 'uniqueStart', scope: behaviour.uniqueStart });
            if (behaviour?.chained) parts.push({ k: 'chained', scope: behaviour.chained });
        }
        if (prop.name === 'Code' && behaviour?.autoCode) parts.push({ k: 'autoCode' });
        if ((prop.name === 'Code' && behaviour?.uniqueCode) ||
            ((prop.name === 'NameEn' || prop.name === 'NameAr') && behaviour?.uniqueName)) {
            parts.push({ k: 'unique' });
        }
    }

    const render = (lang) => parts.map((p) => RULE_TEXT[lang][p.k](p)).join(' ');
    return Object.fromEntries(Object.keys(RULE_TEXT).map((lang) => [lang, render(lang)]));
}

// ---------------------------------------------------------------------------------------------
// Angular template parsing
// ---------------------------------------------------------------------------------------------

const flatten = (html) => html.replace(/\s+/g, ' ');

/** selector -> { html, ts } for every component under angular/src/app, read once. */
let _components = null;
function componentsBySelector() {
    if (_components) return _components;
    _components = new Map();
    const walk = (dir) => {
        for (const e of fs.readdirSync(abs(dir), { withFileTypes: true })) {
            const rel = `${dir}/${e.name}`;
            if (e.isDirectory()) walk(rel);
            else if (e.name.endsWith('.component.ts')) {
                const ts = read(rel);
                const selector = ts.match(/selector:\s*'([^']+)'/)?.[1];
                const url = ts.match(/templateUrl:\s*'([^']+)'/)?.[1];
                if (!selector || !url) continue;
                const html = path.posix.join(path.posix.dirname(rel), url);
                if (exists(html)) _components.set(selector, { html, ts: rel });
            }
        }
    };
    walk('angular/src/app');
    return _components;
}

/**
 * A form's template with the templates of the child components it uses inlined where they sit.
 *
 * Forms get refactored: Wage Type and Employee Basic Pay moved their fields into a
 * `wageTypeForm` / `employeeBasicPayForm` child, and reading the dialog's own file alone then
 * found almost nothing -- the help for those pages silently lost most of its fields. Following the
 * children means the registry can keep naming the dialog, which is what the user opens.
 */
function expandTemplate(rel, depth = 0, seen = new Set([rel])) {
    let html = read(rel);
    const tsFile = rel.replace(/\.html$/, '.ts');
    let ts = exists(tsFile) ? read(tsFile) : '';
    if (depth > 3) return { html, ts };
    const components = componentsBySelector();
    // Only tags that ARE components are replaced, so the ordinary elements around them are left
    // alone and a child nested inside a <div> is still found.
    const used = [...new Set([...html.matchAll(/<([A-Za-z][\w-]*)/g)].map((m) => m[1]))].filter(
        (tag) => components.has(tag) && !seen.has(components.get(tag).html)
    );
    for (const tag of used) {
        const child = components.get(tag);
        seen.add(child.html);
        const inner = expandTemplate(child.html, depth + 1, seen);
        ts += '\n' + inner.ts;
        const re = new RegExp(`<${tag}\\b[^>]*?(?:\\/>|>[\\s\\S]*?<\\/${tag}>)`, 'g');
        html = html.replace(re, () => inner.html);
    }
    return { html, ts };
}

/**
 * The edit modal is the AUTHORITATIVE localization-key -> entity-property map: its labels carry
 * both (`<label for="Employee_TitleId">{{ 'Title' | localize }}</label>`). The read view has only
 * the key, so it borrows this mapping rather than guessing.
 */
/** Matches an element carrying `sectionTitleClass` whose content is a localized key. */
const sectionTitlePattern = (cls) =>
    `<[a-z0-9]+[^>]*class="[^"]*\\b${cls}\\b[^"]*"[^>]*>\\s*\\{\\{\\s*'([^']+)'\\s*\\|\\s*localize\\s*\\}\\}`;

/**
 * Edit form fields in screen order, each with the section it sits under.
 *
 * The labels here are also the AUTHORITATIVE localization-key -> entity-property map: they carry
 * both (`<label for="Employee_TitleId">{{ 'Title' | localize }}</label>`). A read view has only
 * the key, so it borrows this mapping rather than guessing.
 */
function parseEditTemplate(rel, entityName, sectionTitleClass) {
    const expanded = expandTemplate(rel);
    const html = flatten(expanded.html);
    const label =
        `<label([^>]*)for="(${entityName}_[A-Za-z0-9_]+)"([^>]*)>\\s*\\{\\{\\s*'([^']+)'\\s*\\|\\s*localize\\s*\\}\\}`;
    const re = new RegExp(
        sectionTitleClass ? `${sectionTitlePattern(sectionTitleClass)}|${label}` : label,
        'g'
    );
    // With a section pattern first, the label groups shift by one.
    const o = sectionTitleClass ? 1 : 0;
    const out = [];
    let section = null;
    let m;
    while ((m = re.exec(html)) !== null) {
        if (sectionTitleClass && m[1]) {
            section = m[1];
            continue;
        }
        const domId = m[2 + o];
        out.push({
            locKey: m[4 + o],
            domId,
            property: domId.slice(entityName.length + 1),
            // `[class.required]="!record().id"` marks a field required only while the record is new:
            // the Time Attendance API password, which an edit may leave blank to keep the saved one.
            uiRequired: /class="[^"]*\brequired\b/.test(`${m[1 + o]} ${m[3 + o]}`)
                ? true
                : /\[class\.required\]="!\s*\w+(?:\(\))?\.id"/.test(`${m[1 + o]} ${m[3 + o]}`) ? 'create' : false,
            section,
            at: m.index,
        });
    }

    const ts = expanded.ts;

    // Fields rendered in a loop: `@for (flag of includeFlags) { ... [id]="'WageType_' + flag.field" }`.
    // Their ids are built at runtime, so the label scan above cannot see them -- Wage Type's 18
    // include flags, the seven cost elements on account forms and the payroll status dates were all
    // missing. The loop's array is read from the component: plain strings, or objects carrying a
    // `field` and a localization `key`.
    const sectionsAt = [];
    if (sectionTitleClass) {
        for (const s of html.matchAll(new RegExp(sectionTitlePattern(sectionTitleClass), 'g'))) {
            sectionsAt.push({ at: s.index, key: s[1] });
        }
    }
    const idRe = new RegExp(`\\[id\\]="'${entityName}_'\\s*\\+\\s*(\\w+)(?:\\.(\\w+))?"`, 'g');
    for (const d of html.matchAll(idRe)) {
        const [, loopVar, member] = d;
        const loops = [...html.slice(0, d.index).matchAll(new RegExp(`@for\\s*\\(\\s*${loopVar}\\s+of\\s+(\\w+)`, 'g'))];
        const arrayName = loops.at(-1)?.[1];
        const body = arrayName && ts.match(new RegExp(`\\b${arrayName}\\b[^=]*=\\s*\\[([\\s\\S]*?)\\]\\s*;`))?.[1];
        if (!body) continue;
        const items = member
            ? [...body.matchAll(/\{([^}]*)\}/g)].map((x) => ({
                  value: x[1].match(new RegExp(`\\b${member}\\s*:\\s*'([^']+)'`))?.[1],
                  key: x[1].match(/\bkey\s*:\s*'([^']+)'/)?.[1],
              }))
            : [...body.matchAll(/'([^']+)'/g)].map((x) => ({ value: x[1] }));
        const section = sectionsAt.filter((s) => s.at < d.index).at(-1)?.key ?? null;
        items.forEach((item, i) => {
            if (!item.value) return;
            const pascal = item.value.charAt(0).toUpperCase() + item.value.slice(1);
            out.push({
                locKey: item.key ?? pascal,
                domId: `${entityName}_${item.value}`,
                property: pascal,
                uiRequired: false,
                section,
                at: d.index + i / 1000,
            });
        });
    }
    out.sort((a, b) => a.at - b.at);

    // The choices a dropdown actually offers, read from the form rather than the enum: an enum can
    // hold members the form hides (EmploymentType.ALL) or offer ones that look like filters but are
    // real (RecoveryOption.All -- "recovered by payroll and externally"). Options are either
    // literal `<option>`s or an `@for` over an array in the component.
    out.forEach((f, i) => {
        const control = html.slice(f.at, i + 1 < out.length ? out[i + 1].at : html.length);
        const select = control.match(/<select[\s\S]*?<\/select>/)?.[0];
        if (!select) return;
        const literal = [...select.matchAll(/<option[^>]*\[ngValue\]="[^"]+"[^>]*>\s*\{\{\s*'([^']+)'\s*\|\s*localize/g)]
            .map((x) => x[1]);
        if (literal.length) {
            f.options = literal;
            return;
        }
        const list = select.match(/@for\s*\(\s*\w+\s+of\s+(\w+)/)?.[1];
        const arr = list && ts.match(new RegExp(`\\b${list}\\s*=\\s*\\[([\\s\\S]*?)\\]\\s*;`))?.[1];
        const names = arr ? [...arr.matchAll(/\b(?:name|label)\s*:\s*'([^']+)'/g)].map((x) => x[1]) : [];
        if (names.length) f.options = names;
    });
    for (const f of out) delete f.at;
    return out;
}

/** Read view: ordered card titles and `<dt>` field labels. */
function parseReadTemplate(rel, sectionTitleClass) {
    const html = flatten(read(rel));
    const re = new RegExp(
        `${sectionTitlePattern(sectionTitleClass)}` +
        `|<dt[^>]*>\\s*\\{\\{\\s*'([^']+)'\\s*\\|\\s*localize\\s*\\}\\}`,
        'g'
    );
    const out = [];
    let section = null;
    let m;
    while ((m = re.exec(html)) !== null) {
        if (m[1]) section = m[1];
        else out.push({ locKey: m[2], section });
    }
    return out;
}

// ---------------------------------------------------------------------------------------------
// Resolution: localization key -> canonical entity property (DESIGN.md §4)
// ---------------------------------------------------------------------------------------------

/**
 * The entity's own spelling of a property name.
 *
 * Case-insensitive because templates and entities disagree: the edit form's input is
 * `Employee_PaciNumber` while the property is `PACINumber`. Matching exactly split one field into
 * two topics and left F1 on that input resolving to nothing.
 */
function findProp(props, name, aliases) {
    if (!name) return null;
    // pages.json `propertyAliases` maps what a form calls a field onto what the entity now calls it,
    // for fields whose storage changed without the form changing: the employee photo input still posts
    // the image (Employee_Photo) while the entity keeps only Employee.PhotoPath.
    if (aliases && Object.prototype.hasOwnProperty.call(aliases, name)) name = aliases[name];
    if (props.has(name)) return name;
    // Underscores too: Asset's input is `Asset_VinNumber` for the property `VIN_Number`.
    const fold = (s) => s.toLowerCase().replace(/_/g, '');
    const hits = [...props.keys()].filter((k) => fold(k) === fold(name));
    return hits.length === 1 ? hits[0] : null;
}

function resolveField(locKey, editMap, props, aliases) {
    const fromEdit = editMap.get(locKey);
    const editProp = fromEdit && findProp(props, fromEdit.property, aliases);
    if (editProp) {
        return { property: editProp, domId: fromEdit.domId, via: 'edit-template' };
    }
    const exact = findProp(props, locKey, aliases);
    if (exact && !props.get(exact).isNavigation) {
        return { property: exact, domId: null, via: 'entity-exact' };
    }
    const lookup = findProp(props, `${locKey}Id`, aliases);
    if (lookup) {
        return { property: lookup, domId: null, via: 'entity-lookup-id' };
    }
    return null;
}

// ---------------------------------------------------------------------------------------------
// extract
// ---------------------------------------------------------------------------------------------

function loadRegistry() {
    return JSON.parse(read(REGISTRY));
}

function buildInventory() {
    const registry = loadRegistry();
    const pages = [];

    for (const page of registry.pages) {
        const props = parseEntity(page.entity.file);
        const behaviour = serviceBehaviour(page.entity.name, page.service);
        // What the form posts: its length limits can be tighter than the column's.
        const inputDto = indexCsFiles().get(`CreateOrEdit${page.entity.name}Dto`);
        const inputProps = inputDto ? parseEntity(path.relative(ROOT, inputDto)) : new Map();
        const editFields = page.surfaces.edit
            ? parseEditTemplate(
                  page.surfaces.edit.template,
                  page.domPrefix ?? page.entity.name,
                  page.surfaces.edit.sectionTitleClass
              )
            : [];
        // Where an input's id does not spell its property -- the payroll engine's `PayrollEngine_RunType`
        // binds `PayrollRunType` -- the page says so explicitly rather than the tool guessing.
        for (const ef of editFields) ef.property = page.propertyMap?.[ef.property] ?? ef.property;
        const editMap = new Map();
        for (const ef of editFields) if (!editMap.has(ef.locKey)) editMap.set(ef.locKey, ef);

        // A page documents the fields of its PRIMARY surface, in that surface's order: the read
        // view when there is one, otherwise the form itself.
        const primary = page.surfaces.read
            ? parseReadTemplate(page.surfaces.read.template, page.surfaces.read.sectionTitleClass || 'card-title')
            : editFields.map((ef) => ({ locKey: ef.locKey, section: ef.section }));

        const propertyAliases = page.propertyAliases ?? {};

        // Every way the same property appears on this page's surfaces, so F1 lands on one topic
        // whichever of them the reader is looking at.
        const editByProp = new Map();
        for (const ef of editFields) {
            const p = findProp(props, ef.property, propertyAliases);
            if (p && !editByProp.has(p)) editByProp.set(p, ef);
        }

        const fields = [];
        const unresolved = [];
        const seen = new Set();
        let order = 0;

        for (const rf of primary) {
            const hit = resolveField(rf.locKey, editMap, props, propertyAliases);
            if (!hit) {
                unresolved.push({ locKey: rf.locKey, section: rf.section });
                continue;
            }
            const canonicalId = `${page.entity.name}.${hit.property}`;
            if (seen.has(canonicalId)) continue;
            seen.add(canonicalId);

            const prop = props.get(hit.property);
            const aliases = new Set([`loc:${rf.locKey}`]);
            const onForm = editByProp.get(hit.property);
            if (onForm) {
                aliases.add(`dom:${onForm.domId}`);
                aliases.add(`loc:${onForm.locKey}`);
            }

            const rulesByLang = rulesFor(prop, {
                uiRequired: onForm?.uiRequired,
                systemMaintained: editFields.length > 0 && !onForm,
                behaviour,
                formOptions: onForm?.options,
                inputProp: inputProps.get(hit.property),
            });
            fields.push({
                canonicalId,
                property: hit.property,
                locKey: rf.locKey,
                section: rf.section ?? page.defaultSection ?? null,
                order: order++,
                aliases: [...aliases],
                resolvedVia: hit.via,
                type: prop ? prop.type : 'unknown',
                rules: rulesByLang?.en ?? null,
                rulesByLang,
            });
        }

        // Fields the edit surface has but this page does not -- recorded, not documented, so
        // coverage can tell "not on this screen" apart from "undocumented".
        const editOnly = [];
        for (const [prop, ef] of editByProp) {
            const canonicalId = `${page.entity.name}.${prop}`;
            if (!seen.has(canonicalId)) editOnly.push({ canonicalId, locKey: ef.locKey, domId: ef.domId });
        }
        // An input whose id names no property: F1 on it would find nothing, even if its label
        // happens to resolve by another route.
        // pages.json `ignoreInputs` lists input ids that are deliberately not record fields (e.g. the
        // inputs of an on-form calculator); they are neither documented nor reported.
        const ignoreInputs = new Set(page.ignoreInputs ?? []);
        for (const ef of editFields) {
            if (findProp(props, ef.property, propertyAliases) || ignoreInputs.has(ef.domId)) continue;
            const already = unresolved.find((u) => u.locKey === ef.locKey);
            if (already) already.domId = ef.domId;
            else unresolved.push({ locKey: ef.locKey, section: ef.section, domId: ef.domId });
        }
        const ignoredKeys = new Set(editFields.filter((ef) => ignoreInputs.has(ef.domId)).map((ef) => ef.locKey));
        for (let i = unresolved.length - 1; i >= 0; i--) {
            if (!unresolved[i].domId && ignoredKeys.has(unresolved[i].locKey)) unresolved.splice(i, 1);
        }

        pages.push({
            helpId: page.helpId,
            title: page.title,
            audience: page.audience || ['end-user'],
            permissions: page.permissions || [],
            defaultSection: page.defaultSection ?? null,
            entity: page.entity,
            surfaces: page.surfaces,
            fields,
            unresolved,
            editOnly,
        });
    }
    return { generatedAt: new Date().toISOString(), pages };
}

function cmdExtract() {
    const inv = buildInventory();
    fs.mkdirSync(abs(OUT_DIR), { recursive: true });
    const out = path.join(OUT_DIR, 'inventory.json');
    fs.writeFileSync(abs(out), JSON.stringify(inv, null, 2) + '\n');

    for (const p of inv.pages) {
        console.log(`\n${p.helpId}  (${p.entity.name})`);
        console.log(`  documented fields : ${p.fields.length}`);
        console.log(`  unresolved labels : ${p.unresolved.length}`);
        console.log(`  edit-only fields  : ${p.editOnly.length}`);
        const byVia = {};
        for (const f of p.fields) byVia[f.resolvedVia] = (byVia[f.resolvedVia] || 0) + 1;
        for (const [via, n] of Object.entries(byVia)) console.log(`    via ${via}: ${n}`);
        for (const u of p.unresolved) console.log(`    UNRESOLVED  ${u.section} / ${u.locKey}`);
    }
    console.log(`\nwrote ${out}`);
    return 0;
}

// ---------------------------------------------------------------------------------------------
// stubs
// ---------------------------------------------------------------------------------------------

const TODO = '_TODO_';

const rulesIn = (field, lang) => field.rulesByLang?.[lang] ?? field.rules;

function sectionMarkdown(field, lang) {
    const alias = `<!-- alias: ${field.aliases.join(', ')} -->`;
    const rules = rulesIn(field, lang) ? `${RULES_LABEL[lang]} ${rulesIn(field, lang)}` : '';
    const what = lang === 'ar' ? '**ما هو.**' : '**What it is.**';
    const example = lang === 'ar' ? '**مثال.**' : '**Example.**';
    return [
        `## ${field.locKey} {#${field.canonicalId}}`,
        alias,
        '',
        `${what} ${TODO}`,
        '',
        `${example} ${TODO}`,
        '',
        rules,
        '',
    ].filter((l) => l !== null).join('\n');
}

/**
 * The page's title in a language. A discovered form is titled "<list> — Create / Edit"; its list
 * name is the page's section key, which the app already translates.
 */
function titleIn(page, lang) {
    if (lang === 'en' || !/— Create \/ Edit$/.test(page.title)) return page.title;
    const list = page.defaultSection && locText(lang, page.defaultSection);
    return list ? `${list} — ${lang === 'ar' ? 'إنشاء / تحرير' : 'Create / Edit'}` : page.title;
}

function frontmatter(page, lang) {
    const surfaces = Object.values(page.surfaces).map((s) => `  - ${s.template}`);
    return [
        '---',
        `helpId: ${page.helpId}`,
        `lang: ${lang}`,
        `title: ${titleIn(page, lang)}`,
        `entity: ${page.entity.fullName}`,
        'surfaces:',
        ...surfaces,
        `audience: [${page.audience.join(', ')}]`,
        `permissions: [${page.permissions.join(', ')}]`,
        '---',
        '',
        '## Overview',
        '',
        `${TODO}`,
        '',
        '',
    ].join('\n');
}

/** The Markdown under a head's `## Overview` heading, or null when the head has none. */
function overviewOf(head) {
    const parts = head.split(/\n## Overview\n/);
    return parts.length > 1 ? parts[1] : null;
}

/** Split an existing file into a head (frontmatter + Overview) and sections keyed by anchor. */
function splitExisting(md) {
    const parts = md.split(/\n(?=## )/);
    const head = [];
    const sections = new Map();
    for (const part of parts) {
        const m = part.match(/^## .*\{#([^}]+)\}/);
        if (m) sections.set(m[1], part.replace(/\s+$/, ''));
        else head.push(part.replace(/\s+$/, ''));
    }
    return { head: head.join('\n\n'), sections };
}

/** Regenerate the machine-written lines -- Rules and the alias note; prose is never touched. */
function refreshGenerated(sectionMd, field, lang) {
    let out = sectionMd;
    const aliasLine = `<!-- alias: ${field.aliases.join(', ')} -->`;
    if (/^<!--\s*alias:.*-->$/m.test(out)) out = out.replace(/^<!--\s*alias:.*-->$/m, aliasLine);

    const rules = rulesIn(field, lang);
    if (!rules) return out;
    // Matches either language's label, so an English "Rules." line in an Arabic file is
    // rewritten in Arabic rather than left beside a second one.
    const line = `${RULES_LABEL[lang]} ${rules}`;
    if (RULES_LINE.test(out)) return out.replace(RULES_LINE, line);
    return `${out}\n\n${line}`;
}

const pageFile = (page, lang) => path.join(CONTENT_DIR, lang, `${page.helpId}.md`);

/**
 * Every `##` topic in every help file of one language, keyed by canonical id.
 *
 * Topics are pooled rather than read per screen because one field appears on several screens --
 * the Personal Information card and the edit form show the same 39 -- and DESIGN.md §2.1 says it
 * is described once. Each topic lives in exactly one file (its "home"); every other screen that
 * shows the field borrows it from there. Two files defining the same id is a coverage error.
 */
function loadTopicPool(lang) {
    const dir = path.join(CONTENT_DIR, lang);
    const pool = new Map();
    const duplicates = [];
    if (!exists(dir)) return { pool, duplicates };

    const files = fs.readdirSync(abs(dir)).filter((n) => n.endsWith('.md')).sort();
    for (const name of files) {
        const rel = path.join(dir, name);
        for (const [id, body] of splitExisting(read(rel)).sections) {
            if (pool.has(id)) duplicates.push({ id, files: [pool.get(id).file, rel] });
            else pool.set(id, { file: rel, body });
        }
    }
    return { pool, duplicates };
}

/**
 * A field's topic: its own (`City.Code`), or failing that the shared default for its property
 * name (`*.Code`, kept in `_shared.md`).
 *
 * Code, NameEn and NameAr sit on almost every form -- 75 of General Master Info's 151 fields --
 * and mean the same thing on each. Writing them once per entity would be the duplication
 * DESIGN.md §2.1 forbids; a default written once is not. A default may only say what is true of
 * EVERY entity: what differs, such as whether a code is auto-assigned, comes from the Rules line,
 * which is generated per entity, or from an entity's own topic, which always wins.
 */
const wildcardId = (id) => `*.${id.slice(id.indexOf('.') + 1)}`;
const topicFor = (pool, id) => pool.get(id) ?? pool.get(wildcardId(id));

/** First definition of each field across all pages -- its rules and aliases are the same everywhere. */
function fieldsById(inv) {
    const byId = new Map();
    for (const page of inv.pages) {
        for (const f of page.fields) {
            const known = byId.get(f.canonicalId);
            if (!known) byId.set(f.canonicalId, { ...f, aliases: [...f.aliases] });
            else for (const a of f.aliases) if (!known.aliases.includes(a)) known.aliases.push(a);
        }
    }
    return byId;
}

function cmdStubs(argv) {
    const langs = argv.includes('--lang')
        ? argv[argv.indexOf('--lang') + 1].split(',')
        : LANGS;
    const inv = buildInventory();
    const byId = fieldsById(inv);
    let created = 0;
    let merged = 0;
    let added = 0;

    for (const lang of langs) {
        const { pool } = loadTopicPool(lang);

        // A topic's home is the file already holding it; failing that, the first page in the
        // registry that shows the field. Stubs are only ever written into a field's home.
        // A field covered by a shared default has no home and is never stubbed; writing it an
        // entity-specific topic later is still possible, and that topic then wins.
        const home = new Map();
        for (const page of inv.pages) {
            for (const f of page.fields) {
                if (home.has(f.canonicalId)) continue;
                const own = pool.get(f.canonicalId)?.file;
                if (own) home.set(f.canonicalId, own);
                else if (pool.has(wildcardId(f.canonicalId))) home.set(f.canonicalId, null);
                else home.set(f.canonicalId, pageFile(page, lang));
            }
        }

        for (const page of inv.pages) {
            const rel = pageFile(page, lang);
            fs.mkdirSync(path.dirname(abs(rel)), { recursive: true });
            const isNew = !exists(rel);
            const { head, sections } = isNew
                ? { head: frontmatter(page, lang).replace(/\s+$/, ''), sections: new Map() }
                : splitExisting(read(rel));

            const out = [head];
            const emitted = new Set();
            let newHere = 0;
            for (const f of page.fields) {
                if (home.get(f.canonicalId) !== rel) continue;
                const existing = sections.get(f.canonicalId);
                const field = byId.get(f.canonicalId);
                if (existing) {
                    out.push(refreshGenerated(existing, field, lang));
                } else {
                    out.push(sectionMarkdown(field, lang).replace(/\s+$/, ''));
                    newHere++;
                }
                emitted.add(f.canonicalId);
            }
            // Everything else already in the file stays: topics homed here for fields this page
            // does not show, and sections no longer on any screen, which coverage reports as
            // stale so a human decides whether the field moved or the doc is wrong.
            for (const [id, body] of sections) {
                if (emitted.has(id)) continue;
                out.push(byId.has(id) ? refreshGenerated(body, byId.get(id), lang) : body);
            }

            fs.writeFileSync(abs(rel), out.join('\n\n') + '\n');
            added += newHere;
            if (isNew) {
                created++;
                console.log(`created  ${rel}  (${newHere} sections)`);
            } else {
                merged++;
                console.log(`merged   ${rel}  (+${newHere} new sections)`);
            }
        }
    }
    console.log(`\n${created} created, ${merged} merged, ${added} sections added`);
    return 0;
}

// ---------------------------------------------------------------------------------------------
// build -- markdown -> runtime JSON (DESIGN.md §11)
// ---------------------------------------------------------------------------------------------

const ASSET_DIR = 'angular/src/assets/help';

/** The dialog index is language-independent, so it is built once, from the English pass. */
const INDEX_LANG = 'en';

const escapeHtml = (s) =>
    s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');

/** Inline markdown only -- the content style is deliberately narrow (see DESIGN.md §5.3). */
function inlineMd(text) {
    return escapeHtml(text)
        .replace(/\[([^\]]+)\]\(([^)]+)\)/g, '<a href="$2" target="_blank" rel="noopener">$1</a>')
        .replace(/\*\*([^*]+)\*\*/g, '<strong>$1</strong>')
        .replace(/`([^`]+)`/g, '<code>$1</code>')
        .replace(/(^|[^*])\*([^*]+)\*/g, '$1<em>$2</em>')
        .replace(/ -- /g, ' — ');
}

function blocksToHtml(lines) {
    const html = [];
    let para = [];
    let list = [];

    const flushPara = () => {
        if (para.length) html.push(`<p>${inlineMd(para.join(' '))}</p>`);
        para = [];
    };
    const flushList = () => {
        if (list.length) html.push(`<ul>${list.map((i) => `<li>${inlineMd(i)}</li>`).join('')}</ul>`);
        list = [];
    };

    for (const raw of lines) {
        const line = raw.trim();
        if (!line) {
            flushPara();
            flushList();
            continue;
        }
        const item = line.match(/^[-*]\s+(.*)$/);
        if (item) {
            flushPara();
            list.push(item[1]);
            continue;
        }
        flushList();
        para.push(line);
    }
    flushPara();
    flushList();
    return html.join('');
}

/** Split one `##` section into its public body, its admin-only body, and its condition. */
function parseSectionBody(md) {
    const lines = md.split(/\r?\n/).slice(1); // drop the "## …" heading
    const pub = [];
    const admin = [];
    let when = null;
    let inAdmin = false;

    for (const line of lines) {
        const t = line.trim();
        const w = t.match(/^<!--\s*when:\s*(.+?)\s*-->$/);
        if (w) {
            when = w[1];
            continue;
        }
        if (/^<!--/.test(t)) continue; // alias markers and other authoring notes
        if (t === ':::admin') {
            inAdmin = true;
            continue;
        }
        if (t === ':::') {
            inAdmin = false;
            continue;
        }
        (inAdmin ? admin : pub).push(line);
    }

    // Rules are regenerated at build time from the entity, never taken from prose.
    const body = pub.filter((l) => !/^\s*\*\*(?:Rules|القواعد)\.\*\*/.test(l));
    return { html: blocksToHtml(body), adminHtml: blocksToHtml(admin), when };
}

function cmdBuild(argv) {
    const langs = argv.includes('--lang') ? argv[argv.indexOf('--lang') + 1].split(',') : LANGS;
    const inv = buildInventory();
    let written = 0;

    // Which page documents a given input id. This is how F1 inside a create/edit dialog finds its
    // page: dialogs are not annotated, they are recognised by the ASP.NET Zero ids they carry.
    // Only fully written pages go in -- a page still holding stubs would answer F1 with _TODO_.
    const domIndex = {};

    for (const lang of langs) {
        const { pool } = loadTopicPool(lang);

        for (const page of inv.pages) {
            const rel = pageFile(page, lang);
            if (!exists(rel)) {
                console.log(`skip     ${lang}/${page.helpId} (no content file)`);
                continue;
            }
            const { head } = splitExisting(read(rel));
            // Each language names the page itself; the registry title is the English fallback.
            const overviewMd = overviewOf(head) ?? '';

            const fields = [];
            const aliasIndex = {};
            for (const f of page.fields) {
                // From whichever file is the topic's home -- not necessarily this page's -- or
                // the shared default for the property.
                const section = topicFor(pool, f.canonicalId)?.body;
                const parsed = section
                    ? parseSectionBody(section)
                    : { html: '', adminHtml: '', when: null };
                for (const a of f.aliases) aliasIndex[a] = f.canonicalId;
                fields.push({
                    id: f.canonicalId,
                    label: f.locKey,
                    section: f.section,
                    aliases: f.aliases,
                    rules: rulesIn(f, lang),
                    when: parsed.when,
                    html: parsed.html,
                    adminHtml: parsed.adminHtml,
                    stub: !parsed.html || parsed.html.includes(TODO),
                });
            }

            const out = path.join(ASSET_DIR, lang, `${page.helpId}.json`);
            fs.mkdirSync(path.dirname(abs(out)), { recursive: true });
            fs.writeFileSync(
                abs(out),
                JSON.stringify(
                    {
                        helpId: page.helpId,
                        lang,
                        title: head.match(/^title:\s*(.+)$/m)?.[1].trim() ?? page.title,
                        entity: page.entity.fullName,
                        overviewHtml: blocksToHtml(overviewMd.split(/\r?\n/)),
                        aliasIndex,
                        fields,
                    },
                    null,
                    2
                ) + '\n'
            );
            written++;
            const stubbed = fields.filter((f) => f.stub).length;
            if (lang === INDEX_LANG && fields.length && !stubbed) {
                for (const alias of Object.keys(aliasIndex)) {
                    if (!alias.startsWith('dom:')) continue;
                    const id = alias.slice(4);
                    (domIndex[id] ??= []).push(page.helpId);
                }
            }
            console.log(`wrote    ${out}  (${fields.length} fields, ${stubbed} still stubs)`);
        }
    }
    if (langs.includes(INDEX_LANG)) {
        const out = path.join(ASSET_DIR, 'index.json');
        fs.writeFileSync(abs(out), JSON.stringify({ dom: domIndex }, null, 2) + '\n');
        const pages = new Set(Object.values(domIndex).flat());
        console.log(`wrote    ${out}  (${Object.keys(domIndex).length} ids, ${pages.size} pages)`);
    }

    console.log(`\n${written} file(s) written`);
    return 0;
}

// ---------------------------------------------------------------------------------------------
// coverage (DESIGN.md §12)
// ---------------------------------------------------------------------------------------------

function cmdCoverage(argv) {
    const strict = argv.includes('--check');
    const inv = buildInventory();
    let warnings = 0;
    let errors = 0;

    const onAnyScreen = new Set(inv.pages.flatMap((p) => p.fields.map((f) => f.canonicalId)));

    for (const lang of LANGS) {
        const { pool, duplicates } = loadTopicPool(lang);

        for (const page of inv.pages) {
            const rel = pageFile(page, lang);
            console.log(`\n${page.helpId} [${lang}]`);

            if (!exists(rel)) {
                // Arabic is a Phase 4 gate; before that its absence is only a warning.
                const isError = lang === 'en';
                console.log(`  ${isError ? 'ERROR' : 'warn '}  no content file (${rel})`);
                isError ? errors++ : warnings++;
                continue;
            }

            // A page with an Overview must build to a non-empty one. An empty result means the build
            // could not read it -- CRLF line endings once did that to every page, and nothing noticed.
            // The heading is found by a pattern that tolerates any line ending, and the body exactly
            // as cmdBuild takes it, so a parse the build gets wrong is reported here.
            const { head } = splitExisting(read(rel));
            if (/^## Overview\s*$/m.test(head) && !blocksToHtml((overviewOf(head) ?? '').split('\n'))) {
                console.log(`  ERROR  ## Overview builds to empty HTML (${rel})`);
                errors++;
            }

            let written = 0;
            let stubbed = 0;
            let borrowed = 0;
            for (const f of page.fields) {
                const topic = topicFor(pool, f.canonicalId);
                if (!topic) {
                    console.log(`  warn   undocumented field: ${f.canonicalId} (${f.locKey})`);
                    warnings++;
                    continue;
                }
                if (topic.file !== rel) borrowed++;
                if (topic.body.includes(TODO)) stubbed++;
                else written++;
            }

            const total = page.fields.length;
            console.log(
                `  ${written}/${total} written, ${stubbed} stubbed, ` +
                `${total - written - stubbed} missing` +
                (borrowed ? `  (${borrowed} shared from other screens' files)` : '')
            );
        }

        // Whole-language checks: a topic belongs to no single screen, so these cannot be per page.
        for (const [id, topic] of pool) {
            if (onAnyScreen.has(id) || id.startsWith('*.')) continue;
            if (/<!--\s*when:/.test(topic.body)) {
                console.log(`  ok     [${lang}] conditional field not on screen: ${id}`);
                continue;
            }
            console.log(`  ERROR  [${lang}] stale section, no such field on any screen: ${id} (${topic.file})`);
            errors++;
        }
        for (const d of duplicates) {
            console.log(`  ERROR  [${lang}] ${d.id} is described twice: ${d.files.join(' and ')}`);
            errors++;
        }
    }

    for (const page of inv.pages) {
        for (const u of page.unresolved) {
            const where = u.domId ? ` (input id ${u.domId})` : '';
            console.log(`  ERROR  ${page.helpId}: label resolves to no entity property: ${u.locKey}${where}`);
            errors++;
        }
    }

    console.log(`\n${errors} error(s), ${warnings} warning(s)`);
    if (strict && errors > 0) return 1;
    return 0;
}

// ---------------------------------------------------------------------------------------------
// discover -- register a module's forms without hand-writing pages.json
// ---------------------------------------------------------------------------------------------

const kebab = (s) => s.replace(/([a-z0-9])([A-Z])/g, '$1-$2').toLowerCase();

/**
 * Registers every create/edit form in one admin module as a help page.
 *
 * A form is any template whose labels follow the ASP.NET Zero `for="Entity_Property"`
 * convention; the prefix names the entity. Titles come from the app's own English strings, so the
 * page is called what the menu calls it. Forms already registered are left untouched, so this is
 * safe to re-run as a module grows. Nothing is written for a form whose entity file cannot be
 * found -- it is reported instead, because a guessed entity would give every field the wrong id.
 */
function cmdDiscover(argv) {
    const [moduleDir, prefix] = argv;
    if (!moduleDir || !prefix) {
        console.error('usage: help-tool.mjs discover <admin-module-folder> <help-id-prefix>');
        return 2;
    }
    const root = `angular/src/app/admin/${moduleDir}`;
    if (!exists(root)) {
        console.error(`no such module folder: ${root}`);
        return 2;
    }

    const registry = loadRegistry();
    const norm = (p) => p.replace(/\\/g, '/');
    const known = new Set(registry.pages.flatMap((p) => Object.values(p.surfaces).map((s) => norm(s.template))));
    const templates = [];
    const walk = (dir) => {
        for (const e of fs.readdirSync(abs(dir), { withFileTypes: true })) {
            const rel = `${dir}/${e.name}`;
            if (e.isDirectory()) walk(rel);
            else if (e.name.endsWith('.component.html')) templates.push(rel);
        }
    };
    walk(root);

    // Permissions named anywhere in the module's templates; a page lists the create/edit ones.
    const modulePermissions = new Set();
    for (const t of templates) {
        for (const m of read(t).matchAll(/'(Pages\.[A-Za-z0-9_.]+\.(?:Create|Edit))'\s*\|\s*permission/g)) {
            modulePermissions.add(m[1]);
        }
    }

    let added = 0;
    const skipped = [];
    for (const t of templates) {
        const labels = [...read(t).matchAll(/for="([A-Z][A-Za-z0-9]*)_[A-Za-z0-9]+"/g)];
        if (!labels.length || known.has(norm(t))) continue;

        const entityName = labels[0][1];
        const csIndex = indexCsFiles();
        const coreFile = [entityName, `${entityName}Entity`]
            .map((n) => csIndex.get(n))
            .find((f) => f && norm(f).includes('/HCMv3.Core/'));
        if (!coreFile) {
            skipped.push(`${t} (no entity ${entityName} in HCMv3.Core)`);
            continue;
        }
        const src = fs.readFileSync(coreFile, 'utf8');
        const className = path.basename(coreFile, '.cs');
        const ns = src.match(/namespace\s+([\w.]+)/)?.[1];

        // The menu usually names the plural ("Cities"), but not always ("Number Series").
        const titleKey = [pluralKey(entityName), entityName].find((k) => locText('en', k));
        const enTitle = titleKey ? locText('en', titleKey) : entityName.replace(/([a-z])([A-Z])/g, '$1 $2');
        const permissionBase = [...modulePermissions].find((p) =>
            p.toLowerCase().includes(`.${pluralKey(entityName).toLowerCase()}.`)
        );

        const page = {
            helpId: `${prefix}.${kebab(entityName)}.edit`,
            title: `${enTitle} — Create / Edit`,
            audience: ['admin'],
            permissions: permissionBase
                ? [...modulePermissions].filter((p) => p.startsWith(permissionBase.replace(/\.(Create|Edit)$/, '.')))
                : [],
            entity: { name: className, fullName: `${ns}.${className}`, file: norm(path.relative(ROOT, coreFile)) },
            surfaces: { edit: { template: norm(t) } },
        };
        // A form with no section headings still needs a group title in the drawer: the form's own
        // title, or failing that the module's ("General Master Info").
        const moduleKey = moduleDir.replace(/(^|-)([a-z])/g, (_, __, c) => c.toUpperCase());
        const sectionKey = titleKey ?? (locText('en', moduleKey) ? moduleKey : null);
        if (sectionKey) page.defaultSection = sectionKey;
        // The shared simple-lookup form edits an abstract base; its rules live in the base service.
        if (/abstract class/.test(src) && csIndex.has(`${entityName}AppServiceBase`)) {
            page.service = `${entityName}AppServiceBase`;
        }
        // The DOM prefix is not always the class name (SimpleLookup_Code edits SimpleLookupEntity).
        if (className !== entityName) page.domPrefix = entityName;

        registry.pages.push(page);
        known.add(norm(t));
        added++;
        console.log(`added    ${page.helpId.padEnd(44)} ${page.entity.fullName}`);
    }

    fs.writeFileSync(abs(REGISTRY), JSON.stringify(registry, null, 2) + '\n');
    for (const s of skipped) console.log(`SKIPPED  ${s}`);
    console.log(`\n${added} page(s) registered, ${skipped.length} skipped`);
    return 0;
}

// ---------------------------------------------------------------------------------------------

const [, , cmd, ...argv] = process.argv;
const commands = { extract: cmdExtract, stubs: cmdStubs, build: cmdBuild, coverage: cmdCoverage, discover: cmdDiscover };

if (!commands[cmd]) {
    console.error('usage: help-tool.mjs <extract|stubs|build|coverage|discover> [--lang en,ar] [--check]');
    process.exit(2);
}
process.exit(commands[cmd](argv));
