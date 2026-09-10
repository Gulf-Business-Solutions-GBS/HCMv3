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
const read = (rel) => fs.readFileSync(abs(rel), 'utf8');
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
            props.set(m[2], { name: m[2], type: m[1], attrs: attrs.slice() });
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

/** DESIGN.md §10 -- the Rules block, derived and never hand-written. */
function rulesFor(prop, { uiRequired = false } = {}) {
    if (!prop) return null;
    const parts = [];
    const attrText = prop.attrs.join(' ');

    // "Required" must mean the reader cannot save without it. A non-nullable NUMBER is not that:
    // left blank it saves as 0, so Mobile, Civil ID and Probation Days all save empty. Required is
    // therefore [Required], a form label marked required, or a date/choice that always holds a value.
    const alwaysHasValue =
        !prop.isNullable && (prop.bareType === 'DateTime' || (prop.isEnumCandidate && !prop.lookupTarget));
    if (/\[Required\]/.test(attrText) || uiRequired || alwaysHasValue) parts.push('Required.');
    else parts.push('Optional.');

    const len = attrText.match(/\[(?:StringLength|MaxLength)\((\d+)\)/);
    if (len) parts.push(`Maximum ${len[1]} characters.`);

    const range = attrText.match(/\[Range\(([^,]+),\s*([^)]+)\)/);
    if (range) parts.push(`Between ${range[1].trim()} and ${range[2].trim()}.`);

    if (prop.isEnumCandidate) {
        const members = enumMembers(prop.bareType);
        if (members) parts.push(`One of: ${members.join(', ')}.`);
    }

    if (prop.lookupTarget) {
        // Entity type names are PascalCase ("MaritalStatus"); the reader should see words.
        parts.push(`Chosen from ${prop.lookupTarget.replace(/([a-z])([A-Z])/g, '$1 $2')} master data.`);
    } else {
        switch (prop.bareType) {
            case 'DateTime':
                parts.push('Date value.');
                break;
            case 'int':
            case 'long':
                if (!prop.isEnumCandidate) parts.push('Whole number.');
                break;
        }
    }
    return parts.join(' ');
}

// ---------------------------------------------------------------------------------------------
// Angular template parsing
// ---------------------------------------------------------------------------------------------

const flatten = (html) => html.replace(/\s+/g, ' ');

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
    const html = flatten(read(rel));
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
            uiRequired: /class="[^"]*\brequired\b/.test(`${m[1 + o]} ${m[3 + o]}`),
            section,
        });
    }
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
function findProp(props, name) {
    if (!name) return null;
    if (props.has(name)) return name;
    const lower = name.toLowerCase();
    const hits = [...props.keys()].filter((k) => k.toLowerCase() === lower);
    return hits.length === 1 ? hits[0] : null;
}

function resolveField(locKey, editMap, props) {
    const fromEdit = editMap.get(locKey);
    const editProp = fromEdit && findProp(props, fromEdit.property);
    if (editProp) {
        return { property: editProp, domId: fromEdit.domId, via: 'edit-template' };
    }
    const exact = findProp(props, locKey);
    if (exact && !props.get(exact).isNavigation) {
        return { property: exact, domId: null, via: 'entity-exact' };
    }
    const lookup = findProp(props, `${locKey}Id`);
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
        const editFields = page.surfaces.edit
            ? parseEditTemplate(
                  page.surfaces.edit.template,
                  page.entity.name,
                  page.surfaces.edit.sectionTitleClass
              )
            : [];
        const editMap = new Map();
        for (const ef of editFields) if (!editMap.has(ef.locKey)) editMap.set(ef.locKey, ef);

        // A page documents the fields of its PRIMARY surface, in that surface's order: the read
        // view when there is one, otherwise the form itself.
        const primary = page.surfaces.read
            ? parseReadTemplate(page.surfaces.read.template, page.surfaces.read.sectionTitleClass || 'card-title')
            : editFields.map((ef) => ({ locKey: ef.locKey, section: ef.section }));

        // Every way the same property appears on this page's surfaces, so F1 lands on one topic
        // whichever of them the reader is looking at.
        const editByProp = new Map();
        for (const ef of editFields) {
            const p = findProp(props, ef.property);
            if (p && !editByProp.has(p)) editByProp.set(p, ef);
        }

        const fields = [];
        const unresolved = [];
        const seen = new Set();
        let order = 0;

        for (const rf of primary) {
            const hit = resolveField(rf.locKey, editMap, props);
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

            fields.push({
                canonicalId,
                property: hit.property,
                locKey: rf.locKey,
                section: rf.section,
                order: order++,
                aliases: [...aliases],
                resolvedVia: hit.via,
                type: prop ? prop.type : 'unknown',
                rules: rulesFor(prop, { uiRequired: onForm?.uiRequired }),
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
        for (const ef of editFields) {
            if (findProp(props, ef.property)) continue;
            const already = unresolved.find((u) => u.locKey === ef.locKey);
            if (already) already.domId = ef.domId;
            else unresolved.push({ locKey: ef.locKey, section: ef.section, domId: ef.domId });
        }

        pages.push({
            helpId: page.helpId,
            title: page.title,
            audience: page.audience || ['end-user'],
            permissions: page.permissions || [],
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

function sectionMarkdown(field, lang) {
    const alias = `<!-- alias: ${field.aliases.join(', ')} -->`;
    const rules = field.rules ? `**Rules.** ${field.rules}` : '';
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

function frontmatter(page, lang) {
    const surfaces = Object.values(page.surfaces).map((s) => `  - ${s.template}`);
    return [
        '---',
        `helpId: ${page.helpId}`,
        `lang: ${lang}`,
        `title: ${page.title}`,
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
function refreshGenerated(sectionMd, field) {
    let out = sectionMd;
    const aliasLine = `<!-- alias: ${field.aliases.join(', ')} -->`;
    if (/^<!--\s*alias:.*-->$/m.test(out)) out = out.replace(/^<!--\s*alias:.*-->$/m, aliasLine);

    if (!field.rules) return out;
    const line = `**Rules.** ${field.rules}`;
    if (/^\*\*Rules\.\*\*.*$/m.test(out)) return out.replace(/^\*\*Rules\.\*\*.*$/m, line);
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
        const home = new Map();
        for (const page of inv.pages) {
            for (const f of page.fields) {
                if (!home.has(f.canonicalId)) {
                    home.set(f.canonicalId, pool.get(f.canonicalId)?.file ?? pageFile(page, lang));
                }
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
                    out.push(refreshGenerated(existing, field));
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
                out.push(byId.has(id) ? refreshGenerated(body, byId.get(id)) : body);
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
    const body = pub.filter((l) => !/^\s*\*\*Rules\.\*\*/.test(l));
    return { html: blocksToHtml(body), adminHtml: blocksToHtml(admin), when };
}

function cmdBuild(argv) {
    const langs = argv.includes('--lang') ? argv[argv.indexOf('--lang') + 1].split(',') : LANGS;
    const inv = buildInventory();
    let written = 0;

    for (const lang of langs) {
        const { pool } = loadTopicPool(lang);

        for (const page of inv.pages) {
            const rel = pageFile(page, lang);
            if (!exists(rel)) {
                console.log(`skip     ${lang}/${page.helpId} (no content file)`);
                continue;
            }
            const { head } = splitExisting(read(rel));
            const overviewMd = head.split(/\n## Overview\n/)[1] || '';

            const fields = [];
            const aliasIndex = {};
            for (const f of page.fields) {
                // From whichever file is the topic's home -- not necessarily this page's.
                const section = pool.get(f.canonicalId)?.body;
                const parsed = section
                    ? parseSectionBody(section)
                    : { html: '', adminHtml: '', when: null };
                for (const a of f.aliases) aliasIndex[a] = f.canonicalId;
                fields.push({
                    id: f.canonicalId,
                    label: f.locKey,
                    section: f.section,
                    aliases: f.aliases,
                    rules: f.rules,
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
                        title: page.title,
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
            console.log(`wrote    ${out}  (${fields.length} fields, ${stubbed} still stubs)`);
        }
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

            let written = 0;
            let stubbed = 0;
            let borrowed = 0;
            for (const f of page.fields) {
                const topic = pool.get(f.canonicalId);
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
            if (onAnyScreen.has(id)) continue;
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

const [, , cmd, ...argv] = process.argv;
const commands = { extract: cmdExtract, stubs: cmdStubs, build: cmdBuild, coverage: cmdCoverage };

if (!commands[cmd]) {
    console.error('usage: help-tool.mjs <extract|stubs|build|coverage> [--lang en,ar] [--check]');
    process.exit(2);
}
process.exit(commands[cmd](argv));
