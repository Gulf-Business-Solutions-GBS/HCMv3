import { Injectable } from '@angular/core';

/**
 * Rasterises an org chart to a PNG the user can save.
 *
 * Implemented against the browser's own SVG <foreignObject> support rather than a rasteriser package:
 * this repository carries both a yarn.lock and a package-lock.json, so adding a runtime dependency would
 * desync one of them. The technique is the same one those packages use -- clone the subtree, inline every
 * computed style (nothing outside the SVG is consulted when it renders), wrap it in a foreignObject, then
 * draw that SVG onto a canvas.
 *
 * The cards deliberately use inline SVG icons rather than an icon font, because a foreignObject cannot
 * reach a font file that is not embedded in it and icon glyphs would otherwise export as empty boxes.
 */
@Injectable({ providedIn: 'root' })
export class OrgChartExportService {
    /**
     * Properties copied onto every cloned node. Copying the whole computed style declaration works but
     * produces multi-megabyte markup on a chart of any size; this is the subset that actually affects how
     * a card renders.
     */
    private static readonly CopiedStyleProperties = [
        'box-sizing',
        'display',
        'position',
        'flex',
        'flex-direction',
        'flex-wrap',
        'align-items',
        'justify-content',
        'gap',
        'width',
        'height',
        'min-width',
        'min-height',
        'max-width',
        'margin',
        'padding',
        'border',
        'border-radius',
        'background',
        'background-color',
        'color',
        'font-family',
        'font-size',
        'font-weight',
        'line-height',
        'text-align',
        'text-overflow',
        'white-space',
        'overflow',
        'opacity',
        'fill',
        'stroke',
    ];

    async exportElementAsPng(element: HTMLElement, fileName: string): Promise<void> {
        const width = element.offsetWidth;
        const height = element.offsetHeight;

        if (!width || !height) {
            return;
        }

        const svgMarkup = this.buildSvgMarkup(element, width, height);
        const blob = await this.rasterise(svgMarkup, width, height);

        this.triggerDownload(blob, `${fileName}.png`);
    }

    private buildSvgMarkup(element: HTMLElement, width: number, height: number): string {
        const clone = element.cloneNode(true) as HTMLElement;
        this.inlineStyles(element, clone);

        // The live element carries the viewport's zoom/pan transform; the export is always at 100%.
        clone.style.transform = 'none';
        clone.style.width = `${width}px`;
        clone.style.height = `${height}px`;

        const serialized = new XMLSerializer().serializeToString(clone);

        return (
            `<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="${height}">` +
            `<foreignObject width="100%" height="100%">` +
            `<div xmlns="http://www.w3.org/1999/xhtml" style="background:#ffffff;width:${width}px;height:${height}px">` +
            serialized +
            `</div></foreignObject></svg>`
        );
    }

    /** Walks source and clone in lockstep, freezing each node's computed style onto the clone. */
    private inlineStyles(source: Element, clone: Element): void {
        const computed = window.getComputedStyle(source);
        const declarations: string[] = [];

        for (const property of OrgChartExportService.CopiedStyleProperties) {
            const value = computed.getPropertyValue(property);
            if (value) {
                declarations.push(`${property}:${value}`);
            }
        }

        (clone as HTMLElement).setAttribute('style', declarations.join(';'));

        const sourceChildren = Array.from(source.children);
        const cloneChildren = Array.from(clone.children);

        for (let index = 0; index < sourceChildren.length && index < cloneChildren.length; index++) {
            this.inlineStyles(sourceChildren[index], cloneChildren[index]);
        }
    }

    private rasterise(svgMarkup: string, width: number, height: number): Promise<Blob> {
        return new Promise<Blob>((resolve, reject) => {
            const image = new Image();

            image.onload = () => {
                // Render at device pixel ratio so the PNG is crisp on a high-DPI screen.
                const scale = window.devicePixelRatio || 1;
                const canvas = document.createElement('canvas');
                canvas.width = Math.ceil(width * scale);
                canvas.height = Math.ceil(height * scale);

                const context = canvas.getContext('2d');
                if (!context) {
                    reject(new Error('Canvas 2D context is not available.'));
                    return;
                }

                context.scale(scale, scale);
                context.fillStyle = '#ffffff';
                context.fillRect(0, 0, width, height);
                context.drawImage(image, 0, 0);

                canvas.toBlob((blob) => {
                    if (blob) {
                        resolve(blob);
                    } else {
                        reject(new Error('The chart could not be converted to an image.'));
                    }
                }, 'image/png');
            };

            image.onerror = () => reject(new Error('The chart could not be rendered for export.'));

            // A data URL keeps the image same-origin, so the canvas stays untainted and toBlob works.
            image.src = 'data:image/svg+xml;charset=utf-8,' + encodeURIComponent(svgMarkup);
        });
    }

    private triggerDownload(blob: Blob, fileName: string): void {
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
