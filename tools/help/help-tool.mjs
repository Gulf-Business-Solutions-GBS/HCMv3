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
function rulesFor(prop) {
    if (!prop) return null;
    const parts = [];
    const attrText = prop.attrs.join(' ');

    const valueType = !prop.isNullable && prop.bareType !== 'string' && prop.bareType !== 'byte[]';
    if (/\[Required\]/.test(attrText) || valueType) parts.push('Required.');
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
function parseEditTemplate(rel, entityName) {
    const html = flatten(read(rel));
    const re = new RegExp(
        `<label([^>]*)for="(${entityName}_[A-Za-z0-9_]+)"([^>]*)>\\s*\\{\\{\\s*'([^']+)'\\s*\\|\\s*localize\\s*\\}\\}`,
        'g'
    );
    const byLocKey = new Map();
    let m;
    while ((m = re.exec(html)) !== null) {
        const domId = m[2];
        const locKey = m[4];
        const attrs = `${m[1]} ${m[3]}`;
        if (!byLocKey.has(locKey)) {
            byLocKey.set(locKey, {
                domId,
                property: domId.slice(entityName.length + 1),
                uiRequired: /class="[^"]*\brequired\b/.test(attrs),
            });
        }
    }
    return byLocKey;
}

/** Read view: ordered card titles and `<dt>` field labels. */
function parseReadTemplate(rel, sectionTitleClass) {
    const html = flatten(read(rel));
    const re = new RegExp(
        `<h\\d[^>]*class="[^"]*${sectionTitleClass}[^"]*"[^>]*>\\s*\\{\\{\\s*'([^']+)'\\s*\\|\\s*localize\\s*\\}\\}` +
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

function resolveField(locKey, editMap, props) {
    const fromEdit = editMap.get(locKey);
    if (fromEdit && props.has(fromEdit.property)) {
        return { property: fromEdit.property, domId: fromEdit.domId, via: 'edit-template' };
    }
    if (props.has(locKey) && !props.get(locKey).isNavigation) {
        return { property: locKey, domId: null, via: 'entity-exact' };
    }
    if (props.has(`${locKey}Id`)) {
        return { property: `${locKey}Id`, domId: null, via: 'entity-lookup-id' };
    }
    if (fromEdit) {
        return { property: fromEdit.property, domId: fromEdit.domId, via: 'edit-template-unverified' };
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
        const editMap = page.surfaces.edit
            ? parseEditTemplate(page.surfaces.edit.template, page.entity.name)
            : new Map();
        const readFields = parseReadTemplate(
            page.surfaces.read.template,
            page.surfaces.read.sectionTitleClass || 'card-title'
        );

        const fields = [];
        const unresolved = [];
        const seen = new Set();
        let order = 0;

        for (const rf of readFields) {
            const hit = resolveField(rf.locKey, editMap, props);
            if (!hit) {
                unresolved.push({ locKey: rf.locKey, section: rf.section });
                continue;
            }
            const canonicalId = `${page.entity.name}.${hit.property}`;
            if (seen.has(canonicalId)) continue;
            seen.add(canonicalId);

            const prop = props.get(hit.property);
            const aliases = [`loc:${rf.locKey}`];
            if (hit.domId) aliases.push(`dom:${hit.domId}`);

            fields.push({
                canonicalId,
                property: hit.property,
                locKey: rf.locKey,
                section: rf.section,
                order: order++,
                aliases,
                resolvedVia: hit.via,
                type: prop ? prop.type : 'unknown',
                rules: rulesFor(prop),
            });
        }

        // Fields the edit surface has but the read view does not -- recorded, not documented,
        // so coverage can tell "not on this screen" apart from "undocumented".
        const editOnly = [];
        for (const [locKey, info] of editMap) {
            const canonicalId = `${page.entity.name}.${info.property}`;
            if (!seen.has(canonicalId)) editOnly.push({ canonicalId, locKey, domId: info.domId });
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

/** Regenerate only the Rules line; every hand-written line survives untouched. */
function refreshRules(sectionMd, field) {
    if (!field.rules) return sectionMd;
    const line = `**Rules.** ${field.rules}`;
    if (/^\*\*Rules\.\*\*.*$/m.test(sectionMd)) {
        return sectionMd.replace(/^\*\*Rules\.\*\*.*$/m, line);
    }
    return `${sectionMd}\n\n${line}`;
}

function cmdStubs(argv) {
    const langs = argv.includes('--lang')
        ? argv[argv.indexOf('--lang') + 1].split(',')
        : LANGS;
    const inv = buildInventory();
    let created = 0;
    let merged = 0;
    let added = 0;

    for (const page of inv.pages) {
        for (const lang of langs) {
            const rel = path.join(CONTENT_DIR, lang, `${page.helpId}.md`);
            fs.mkdirSync(path.dirname(abs(rel)), { recursive: true });

            if (!exists(rel)) {
                const body = page.fields.map((f) => sectionMarkdown(f, lang)).join('\n');
                fs.writeFileSync(abs(rel), frontmatter(page, lang) + body);
                created++;
                added += page.fields.length;
                console.log(`created  ${rel}  (${page.fields.length} sections)`);
                continue;
            }

            const { head, sections } = splitExisting(read(rel));
            const out = [head];
            let newHere = 0;
            for (const f of page.fields) {
                const existing = sections.get(f.canonicalId);
                if (existing) {
                    out.push(refreshRules(existing, f));
                } else {
                    out.push(sectionMarkdown(f, lang).replace(/\s+$/, ''));
                    newHere++;
                }
            }
            // Sections no longer on the screen are KEPT; coverage reports them as stale so a
            // human decides whether the field moved or the doc is wrong.
            for (const [id, body] of sections) {
                if (!page.fields.some((f) => f.canonicalId === id)) out.push(body);
            }
            fs.writeFileSync(abs(rel), out.join('\n\n') + '\n');
            merged++;
            added += newHere;
            console.log(`merged   ${rel}  (+${newHere} new sections)`);
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

    for (const page of inv.pages) {
        for (const lang of langs) {
            const rel = path.join(CONTENT_DIR, lang, `${page.helpId}.md`);
            if (!exists(rel)) {
                console.log(`skip     ${lang}/${page.helpId} (no content file)`);
                continue;
            }
            const { head, sections } = splitExisting(read(rel));
            const overviewMd = head.split(/\n## Overview\n/)[1] || '';

            const fields = [];
            const aliasIndex = {};
            for (const f of page.fields) {
                const section = sections.get(f.canonicalId);
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

    for (const page of inv.pages) {
        for (const lang of LANGS) {
            const rel = path.join(CONTENT_DIR, lang, `${page.helpId}.md`);
            console.log(`\n${page.helpId} [${lang}]`);

            if (!exists(rel)) {
                // Arabic is a Phase 4 gate; before that its absence is only a warning.
                const isError = lang === 'en';
                console.log(`  ${isError ? 'ERROR' : 'warn '}  no content file (${rel})`);
                isError ? errors++ : warnings++;
                continue;
            }

            const { sections } = splitExisting(read(rel));
            const known = new Set(page.fields.map((f) => f.canonicalId));
            const documented = [];
            const stubbed = [];

            for (const f of page.fields) {
                const body = sections.get(f.canonicalId);
                if (!body) {
                    console.log(`  warn   undocumented field: ${f.canonicalId} (${f.locKey})`);
                    warnings++;
                } else if (body.includes(TODO)) {
                    stubbed.push(f.canonicalId);
                } else {
                    documented.push(f.canonicalId);
                }
            }

            for (const [id, body] of sections) {
                if (known.has(id)) continue;
                if (/<!--\s*when:/.test(body)) {
                    console.log(`  ok     conditional field not on screen: ${id}`);
                    continue;
                }
                console.log(`  ERROR  stale section, no such field on screen: ${id}`);
                errors++;
            }

            const total = page.fields.length;
            console.log(
                `  ${documented.length}/${total} written, ${stubbed.length} stubbed, ` +
                `${total - documented.length - stubbed.length} missing`
            );
        }

        for (const u of page.unresolved) {
            console.log(`  ERROR  label resolves to no entity property: ${u.locKey} (${u.section})`);
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
