/**
 * Downloads the Web.Host OpenAPI document and writes a reduced copy containing ONLY the careers-portal
 * surface, which NSwag then turns into TypeScript clients.
 *
 * WHY FILTER AT ALL. The full HCMv3 document is ~2,250 paths and ~2,450 schemas -- the entire authenticated
 * admin API. Generating clients from it would produce a multi-megabyte `service-proxies.ts` in a PUBLIC,
 * anonymous-facing bundle whose source map would hand a reader the complete shape of the internal HR API
 * (payroll, disciplinary actions, salary registers). Restricting generation to the three careers services
 * keeps the portal's proxy file small, reviewable, and free of internal surface area. The output is still
 * 100% NSwag-generated -- nothing here hand-writes a proxy or a DTO.
 *
 * Usage: node filter-swagger.mjs <swaggerUrl> <outputFile>
 */
import { writeFileSync } from 'node:fs';

const [, , swaggerUrl, outputFile] = process.argv;

if (!swaggerUrl || !outputFile) {
    console.error('Usage: node filter-swagger.mjs <swaggerUrl> <outputFile>');
    process.exit(1);
}

/** The Web.Host dev certificate is self-signed; this script only ever talks to it. */
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';

/** Only these dynamic-API controllers belong in the public portal bundle. */
const INCLUDED_CONTROLLERS = ['Careers', 'CareersAccount', 'CareersCandidate'];

const PATH_PREFIXES = INCLUDED_CONTROLLERS.map((c) => `/api/services/app/${c}/`);

const document = await fetch(swaggerUrl).then((r) => {
    if (!r.ok) {
        throw new Error(`Could not fetch ${swaggerUrl}: HTTP ${r.status}`);
    }
    return r.json();
});

const keptPaths = {};
for (const [path, operations] of Object.entries(document.paths ?? {})) {
    if (PATH_PREFIXES.some((prefix) => path.startsWith(prefix))) {
        keptPaths[path] = operations;
    }
}

if (Object.keys(keptPaths).length === 0) {
    throw new Error(
        'No careers paths found in the OpenAPI document. Is Web.Host running the build that contains ' +
            'ICareersAppService / ICareersAccountAppService / ICareersCandidateAppService?'
    );
}

const allSchemas = document.components?.schemas ?? {};

/** Transitively collect every schema reachable from the kept operations. */
const reachable = new Set();
const collect = (node) => {
    if (node === null || typeof node !== 'object') {
        return;
    }

    if (Array.isArray(node)) {
        node.forEach(collect);
        return;
    }

    const ref = node.$ref;
    if (typeof ref === 'string') {
        const name = ref.replace('#/components/schemas/', '');
        if (!reachable.has(name)) {
            reachable.add(name);
            collect(allSchemas[name]);
        }
    }

    for (const value of Object.values(node)) {
        collect(value);
    }
};

collect(keptPaths);

const keptSchemas = {};
for (const name of [...reachable].sort()) {
    if (allSchemas[name]) {
        keptSchemas[name] = allSchemas[name];
    }
}

const filtered = {
    openapi: document.openapi,
    info: {
        ...document.info,
        title: 'HCMv3 Careers Portal API',
    },
    servers: document.servers,
    paths: keptPaths,
    components: {
        ...document.components,
        schemas: keptSchemas,
    },
};

writeFileSync(outputFile, JSON.stringify(filtered, null, 2), 'utf8');

console.log(
    `Careers OpenAPI document written to ${outputFile}: ` +
        `${Object.keys(keptPaths).length} paths, ${Object.keys(keptSchemas).length} schemas ` +
        `(filtered from ${Object.keys(document.paths ?? {}).length} paths, ${Object.keys(allSchemas).length} schemas).`
);
