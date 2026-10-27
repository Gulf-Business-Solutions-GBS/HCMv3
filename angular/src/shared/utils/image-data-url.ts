/**
 * Images travel to and from the server as plain base64 in `byte[]` DTO fields. The browser shows them only
 * as a data URL, and FileReader.readAsDataURL hands back one, so the two need converting at each edge.
 */

/** The base64 part of a data URL, for a `byte[]` field; plain base64 is returned unchanged. */
export function toBase64Payload(value: string | undefined | null): string | undefined {
    if (!value) {
        return undefined;
    }
    const marker = ';base64,';
    const index = value.indexOf(marker);
    return index >= 0 ? value.substring(index + marker.length) : value;
}

/** Something an `<img [src]>` can show: a data URL as it is, plain base64 wrapped as one. */
export function toImageSrc(value: string | undefined | null): string | undefined {
    if (!value) {
        return undefined;
    }
    return value.startsWith('data:') ? value : `data:image/png;base64,${value}`;
}
