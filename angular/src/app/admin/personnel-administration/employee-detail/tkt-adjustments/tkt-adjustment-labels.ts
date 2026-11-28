import { AdjustmentType, TicketPaymentType } from '@shared/service-proxies/service-proxies';

/** Adjustment Type options in legacy enum order (value + localization key). */
export const TKT_ADJUSTMENT_TYPES = [
    { value: AdjustmentType.Encashment, name: 'Encashment' },
    { value: AdjustmentType.Adjust, name: 'Adjust' },
    { value: AdjustmentType.Fractional, name: 'Fractional' },
    { value: AdjustmentType.NoImpactOnQuota, name: 'NoImpactOnQuota' },
];

/** Ticket Payment Type options in legacy enum order (value + localization key). */
export const TKT_PAYMENT_TYPES = [
    { value: TicketPaymentType.PayWithPayroll, name: 'PayWithPayroll' },
    { value: TicketPaymentType.PayInAdvance, name: 'PayInAdvance' },
];

/** Localization key for a stored Adjustment Type value. */
export function tktAdjustmentTypeKey(value: AdjustmentType): string {
    return TKT_ADJUSTMENT_TYPES.find((o) => o.value === value)?.name ?? '';
}

/** Localization key for a stored Ticket Payment Type value. */
export function tktPaymentTypeKey(value: TicketPaymentType): string {
    return TKT_PAYMENT_TYPES.find((o) => o.value === value)?.name ?? '';
}
