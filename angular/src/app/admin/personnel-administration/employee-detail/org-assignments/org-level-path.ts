import { OrgAssignmentOrgUnitDto } from '@shared/service-proxies/service-proxies';

/**
 * The legacy OrgLevel1..OrgLevel7 values of an assignment, worked out from the one organization unit HCMv3 stores:
 * that unit and each ancestor, keyed by their level (from their Organization Unit Type).
 */
export function orgLevelPath(
    unitId: number | undefined,
    unitById: ReadonlyMap<number, OrgAssignmentOrgUnitDto>
): Map<number, OrgAssignmentOrgUnitDto> {
    const path = new Map<number, OrgAssignmentOrgUnitDto>();
    let unit = unitId !== undefined && unitId !== null ? unitById.get(unitId) : undefined;
    while (unit) {
        if (!path.has(unit.level)) {
            path.set(unit.level, unit);
        }
        unit = unit.parentId !== undefined && unit.parentId !== null ? unitById.get(unit.parentId) : undefined;
    }
    return path;
}

/** True when `unit` is `ancestorId` or sits anywhere below it. */
export function isUnderOrgUnit(
    unit: OrgAssignmentOrgUnitDto,
    ancestorId: number,
    unitById: ReadonlyMap<number, OrgAssignmentOrgUnitDto>
): boolean {
    let current: OrgAssignmentOrgUnitDto | undefined = unit;
    while (current) {
        if (current.id === ancestorId) {
            return true;
        }
        current = current.parentId !== undefined && current.parentId !== null ? unitById.get(current.parentId) : undefined;
    }
    return false;
}
