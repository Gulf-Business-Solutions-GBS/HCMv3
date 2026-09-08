/**
 * View models shared by all three org-chart screens (Company Structure Overview, Employee Org Chart,
 * Position Org Chart). Deliberately neutral about where the data came from: each screen maps its own
 * service-proxy DTO onto these, which is what keeps the three charts looking identical without coupling
 * the renderer to any one backend contract.
 */

/**
 * Icons available to a card metric. A closed set rather than a free-form CSS class on purpose: the cards
 * render these as inline SVG so that "Export as image" produces real icons -- an icon font cannot be
 * reached from inside the SVG the exporter builds, and would rasterise as empty boxes.
 */
export type OrgChartMetricIcon = 'person' | 'people';

/** One figure rendered as an icon + number on a card. A card shows at most two. */
export interface OrgChartMetric {
    icon: OrgChartMetricIcon;
    value: number;
    /** Tooltip text -- already localized by the caller. */
    tooltip: string;
}

/**
 * Which of the two card treatments to draw.
 *
 * `unit` is an organization box: a type-coloured bar down the leading edge and a coloured strip carrying
 * the child count. `person` is a human: a circular avatar in place of that bar, the job title under the
 * name, and no strip. Both render their figures as icon+number badges.
 *
 * Both share the same frame, typography, hover, focus treatment and export behaviour, which is the whole
 * point of them being one component.
 */
export type OrgChartCardVariant = 'unit' | 'person';

/** Which blank silhouette to draw when a person has no photo. */
export type OrgChartAvatarFallback = 'male' | 'female' | 'neutral';

export interface OrgChartAvatar {
    /**
     * Photo URL, or null to go straight to the silhouette. The card also falls back to the silhouette if
     * the image 404s, so a caller that guesses wrong degrades quietly rather than showing a broken image.
     */
    imageUrl: string | null;

    fallback: OrgChartAvatarFallback;
}

/** The small button on the trailing edge of a person card. */
export interface OrgChartCardAction {
    /** Tooltip text -- already localized by the caller. */
    tooltip: string;
}

export interface OrgChartCard {
    id: number;

    /** Small heading above the name, e.g. "Legal Entity", "Department", or a job title. */
    typeLabel: string | null;

    /** Business reference code shown under the heading. */
    code: string | null;

    /** The card's main label. */
    name: string;

    /**
     * Drives the card's accent colour. Any stable string works -- the Company Structure chart passes the
     * organization unit type code ("Level0".."Level9") so that every unit at the same level is coloured
     * the same on every chart.
     */
    accentKey: string | null;

    /** Up to two figures shown as badges along the bottom of the card. */
    metrics: OrgChartMetric[];

    /** Number shown in the card's coloured strip. Null hides the strip entirely. */
    stripCount: number | null;

    /** Tooltip for the strip figure -- already localized by the caller. */
    stripTooltip?: string;

    /** When false the card renders as static: no hover affordance, no click output. */
    navigable: boolean;

    /** Defaults to `unit` when omitted, so existing callers keep the organization-box treatment. */
    variant?: OrgChartCardVariant;

    /** Only drawn by the `person` variant. */
    avatar?: OrgChartAvatar;

    /** Only drawn by the `person` variant. Omit to hide the button. */
    action?: OrgChartCardAction;
}

/**
 * The ten organization levels each get a fixed accent colour, so a node keeps its colour no matter which
 * chart or which screen it appears on. Anything unrecognized falls back to the neutral accent.
 */
export const ORG_CHART_ACCENTS: Record<string, string> = {
    Level0: '#7239ea',
    Level1: '#7239ea',
    Level2: '#009ef7',
    Level3: '#50cd89',
    Level4: '#f1416c',
    Level5: '#ffc700',
    Level6: '#00a3ff',
    Level7: '#f1bc00',
    Level8: '#8950fc',
    Level9: '#1bc5bd',
};

export const ORG_CHART_DEFAULT_ACCENT = '#a1a5b7';

export function resolveOrgChartAccent(accentKey: string | null | undefined): string {
    if (!accentKey) {
        return ORG_CHART_DEFAULT_ACCENT;
    }

    return ORG_CHART_ACCENTS[accentKey] ?? ORG_CHART_DEFAULT_ACCENT;
}
