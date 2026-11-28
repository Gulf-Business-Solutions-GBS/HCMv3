import { AdjustmentPaymentType, AdjustmentType } from '@shared/service-proxies/service-proxies';

/** Localization key of an AdjustmentType value (legacy enum member name). */
export function adjustmentTypeKey(value: AdjustmentType): string {
    return AdjustmentType[value] ?? '';
}

/** Localization key of an AdjustmentPaymentType value (legacy enum member name). */
export function adjustmentPaymentTypeKey(value: AdjustmentPaymentType): string {
    return AdjustmentPaymentType[value] ?? '';
}
