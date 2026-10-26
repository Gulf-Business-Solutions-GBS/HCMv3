import { Injectable } from '@angular/core';

/**
 * Saves a dashboard tile, or a whole dashboard, as a JPEG or a PDF exactly as it looks on screen, charts included.
 *
 * No packages: this repository carries both a yarn.lock and a package-lock.json, so a new runtime dependency
 * would desync one of them (the same reason OrgChartExportService is hand-rolled). The picture is made the way
 * that service makes it -- clone the element, freeze each node's computed style onto the clone, wrap it in an SVG
 * <foreignObject> and draw that onto a canvas. The PDF is a single page holding that JPEG, written by hand: a PDF
 * embeds a JPEG as-is (DCTDecode), so the whole file is five small objects.
 *
 * Anything marked `.ld-no-export` (the download and close buttons) is left out of the picture.
 */
@Injectable({ providedIn: 'root' })
export class DashboardSnapshotService {
    /**
     * Properties frozen onto every cloned node. The HTML ones lay out the cards; the SVG ones (fill, stroke,
     * text-anchor...) are what ngx-charts styles through CSS, without which the bars and labels come out black.
     */
    private static readonly CopiedStyleProperties = [
        'box-sizing',
        'display',
        'position',
        'top',
        'left',
        'right',
        'bottom',
        'flex',
        'flex-direction',
        'flex-wrap',
        'flex-grow',
        'flex-shrink',
        'flex-basis',
        'align-items',
        'align-self',
        'justify-content',
        'gap',
        'row-gap',
        'column-gap',
        'grid-template-columns',
        'grid-column',
        'width',
        'height',
        'min-width',
        'min-height',
        'max-width',
        'max-height',
        'margin',
        'padding',
        'border',
        'border-top',
        'border-bottom',
        'border-left',
        'border-right',
        'border-radius',
        'background',
        'background-color',
        'box-shadow',
        'color',
        'opacity',
        'visibility',
        'overflow',
        'font-family',
        'font-size',
        'font-weight',
        'font-style',
        'line-height',
        'letter-spacing',
        'text-align',
        'text-transform',
        'text-overflow',
        'white-space',
        'vertical-align',
        'table-layout',
        'border-collapse',
        'fill',
        'fill-opacity',
        'stroke',
        'stroke-width',
        'stroke-opacity',
        'stroke-dasharray',
        'text-anchor',
        'dominant-baseline',
        'alignment-baseline',
        'shape-rendering',
    ];

    async saveAsJpeg(element: HTMLElement, fileName: string): Promise<void> {
        const shot = await this.capture(element);
        this.download(new Blob([shot.jpeg], { type: 'image/jpeg' }), `${fileName}.jpg`);
    }

    async saveAsPdf(element: HTMLElement, fileName: string): Promise<void> {
        const shot = await this.capture(element);
        this.download(this.buildPdf(shot), `${fileName}.pdf`);
    }

    // ------------------------------------------------------------ capture

    private async capture(element: HTMLElement): Promise<Snapshot> {
        const width = Math.ceil(element.offsetWidth);
        const height = Math.ceil(element.offsetHeight);
        if (!width || !height) {
            throw new Error('Nothing to capture: the element has no size.');
        }

        const background = this.backgroundOf(element);
        const markup = this.buildSvgMarkup(element, width, height, background);

        // Twice the CSS size, so text stays sharp when the PDF is zoomed or the JPEG printed.
        const scale = 2;
        const canvas = await this.rasterise(markup, width, height, scale, background);
        const blob = await new Promise<Blob>((resolve, reject) =>
            canvas.toBlob(
                (b) => (b ? resolve(b) : reject(new Error('The picture could not be created.'))),
                'image/jpeg',
                0.92
            )
        );

        return {
            jpeg: new Uint8Array(await blob.arrayBuffer()),
            pixelWidth: canvas.width,
            pixelHeight: canvas.height,
            cssWidth: width,
            cssHeight: height,
        };
    }

    /** The first non-transparent background up the tree, so a dark theme exports dark and a light one light. */
    private backgroundOf(element: HTMLElement): string {
        for (let node: HTMLElement | null = element; node; node = node.parentElement) {
            const colour = window.getComputedStyle(node).backgroundColor;
            if (colour && colour !== 'transparent' && !colour.startsWith('rgba(0, 0, 0, 0)')) {
                return colour;
            }
        }
        return '#ffffff';
    }

    private buildSvgMarkup(element: HTMLElement, width: number, height: number, background: string): string {
        const clone = element.cloneNode(true) as HTMLElement;
        this.inlineStyles(element, clone);

        // Removed only after inlining: inlineStyles walks source and clone in lockstep by child index.
        clone.querySelectorAll('.ld-no-export').forEach((node) => node.remove());

        clone.style.width = `${width}px`;
        clone.style.height = `${height}px`;
        clone.style.margin = '0';
        clone.style.transform = 'none';
        clone.style.animation = 'none';

        const serialized = new XMLSerializer().serializeToString(clone);
        return (
            `<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="${height}">` +
            `<foreignObject width="100%" height="100%">` +
            `<div xmlns="http://www.w3.org/1999/xhtml" style="background:${background};width:${width}px;height:${height}px">` +
            serialized +
            `</div></foreignObject></svg>`
        );
    }

    /** Walks source and clone in lockstep, freezing each node's computed style onto the clone. */
    private inlineStyles(source: Element, clone: Element): void {
        const computed = window.getComputedStyle(source);
        const declarations: string[] = [];

        for (const property of DashboardSnapshotService.CopiedStyleProperties) {
            const value = computed.getPropertyValue(property);
            if (value) {
                declarations.push(`${property}:${value}`);
            }
        }

        // Entrance animations are finished on screen but would restart, invisible, inside the picture.
        declarations.push('animation:none', 'transition:none');
        clone.setAttribute('style', declarations.join(';'));

        const sourceChildren = Array.from(source.children);
        const cloneChildren = Array.from(clone.children);
        for (let i = 0; i < sourceChildren.length && i < cloneChildren.length; i++) {
            this.inlineStyles(sourceChildren[i], cloneChildren[i]);
        }
    }

    private rasterise(
        markup: string,
        width: number,
        height: number,
        scale: number,
        background: string
    ): Promise<HTMLCanvasElement> {
        return new Promise((resolve, reject) => {
            const image = new Image();

            image.onload = () => {
                const canvas = document.createElement('canvas');
                canvas.width = Math.ceil(width * scale);
                canvas.height = Math.ceil(height * scale);

                const context = canvas.getContext('2d');
                if (!context) {
                    reject(new Error('Canvas 2D context is not available.'));
                    return;
                }

                context.scale(scale, scale);
                // JPEG has no transparency: paint the page colour first or empty areas turn black.
                context.fillStyle = background;
                context.fillRect(0, 0, width, height);
                context.drawImage(image, 0, 0);
                resolve(canvas);
            };

            image.onerror = () => reject(new Error('The dashboard could not be rendered for export.'));

            // A data URL keeps the image same-origin, so the canvas stays untainted and toBlob works.
            image.src = 'data:image/svg+xml;charset=utf-8,' + encodeURIComponent(markup);
        });
    }

    // ------------------------------------------------------------ PDF

    /**
     * A one-page PDF showing the JPEG at its on-screen size (1 CSS pixel = 0.75 pt, i.e. 96 dpi). Objects:
     * 1 catalog, 2 page tree, 3 page, 4 the image (JPEG bytes as-is), 5 the drawing instructions.
     */
    private buildPdf(shot: Snapshot): Blob {
        const pageWidth = +(shot.cssWidth * 0.75).toFixed(2);
        const pageHeight = +(shot.cssHeight * 0.75).toFixed(2);
        const drawing = `q ${pageWidth} 0 0 ${pageHeight} 0 0 cm /Im0 Do Q`;

        const encoder = new TextEncoder();
        const parts: Uint8Array<ArrayBuffer>[] = [];
        const offsets: number[] = [];
        let length = 0;
        const push = (part: string | Uint8Array<ArrayBuffer>) => {
            const bytes = typeof part === 'string' ? encoder.encode(part) : part;
            parts.push(bytes);
            length += bytes.length;
        };
        const object = (id: number, body: string) => {
            offsets[id] = length;
            push(`${id} 0 obj\n${body}\nendobj\n`);
        };

        push('%PDF-1.4\n%âãÏÓ\n');
        object(1, '<< /Type /Catalog /Pages 2 0 R >>');
        object(2, '<< /Type /Pages /Kids [3 0 R] /Count 1 >>');
        object(
            3,
            `<< /Type /Page /Parent 2 0 R /MediaBox [0 0 ${pageWidth} ${pageHeight}] ` +
                '/Resources << /XObject << /Im0 4 0 R >> >> /Contents 5 0 R >>'
        );

        offsets[4] = length;
        push(
            '4 0 obj\n' +
                `<< /Type /XObject /Subtype /Image /Width ${shot.pixelWidth} /Height ${shot.pixelHeight} ` +
                `/ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode /Length ${shot.jpeg.length} >>\nstream\n`
        );
        push(shot.jpeg);
        push('\nendstream\nendobj\n');

        object(5, `<< /Length ${drawing.length} >>\nstream\n${drawing}\nendstream`);

        const xrefAt = length;
        let xref = 'xref\n0 6\n0000000000 65535 f \n';
        for (let id = 1; id <= 5; id++) {
            xref += `${String(offsets[id]).padStart(10, '0')} 00000 n \n`;
        }
        push(xref);
        push(`trailer\n<< /Size 6 /Root 1 0 R >>\nstartxref\n${xrefAt}\n%%EOF\n`);

        return new Blob(parts, { type: 'application/pdf' });
    }

    private download(blob: Blob, fileName: string): void {
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = fileName;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(url);
    }
}

interface Snapshot {
    jpeg: Uint8Array<ArrayBuffer>;
    pixelWidth: number;
    pixelHeight: number;
    cssWidth: number;
    cssHeight: number;
}
