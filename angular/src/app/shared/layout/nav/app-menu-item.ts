export class AppMenuItem {
    name = '';
    permissionName = '';
    icon = '';
    route = '';
    routeTemplates = [];
    items: AppMenuItem[];
    external: boolean;
    requiresAuthentication: boolean;
    featureDependency: any;
    parameters: {};

    constructor(
        name: string,
        permissionName: string,
        icon: string,
        route: string,
        routeTemplates?: string[],
        items?: AppMenuItem[],
        external?: boolean,
        parameters?: Object,
        featureDependency?: any,
        requiresAuthentication?: boolean
    ) {
        this.name = name;
        this.permissionName = permissionName;
        this.icon = icon;
        this.route = route;
        this.routeTemplates = routeTemplates;
        this.external = external;
        this.parameters = parameters;
        this.featureDependency = featureDependency;

        if (items === undefined) {
            this.items = [];
        } else {
            this.items = items;
        }

        if (this.permissionName) {
            this.requiresAuthentication = true;
        } else {
            this.requiresAuthentication = requiresAuthentication ? requiresAuthentication : false;
        }
    }

    /**
     * The catalog <c>Code</c> of the transaction this entry opens, for the self-service screens.
     *
     * <p>Set through <see cref="governedBy"/>. When present, the entry is hidden for a tenant that has
     * switched that transaction off -- otherwise disabling a type removes its home-page tile but leaves a
     * menu entry pointing at a screen that no longer applies, which reads as a broken link.</p>
     *
     * <p>Undefined on everything else, including the self-service entries that are not transactions at all
     * (My Requests, the inbox, notifications). Those are always shown.</p>
     */
    transactionType?: string;

    /**
     * Every catalog type this entry opens, for a screen that serves more than one -- the Documents screen
     * holds document, withdrawal and letter requests. The entry stays while ANY of them is switched on.
     */
    transactionTypes: string[] = [];

    /**
     * Marks this entry as governed by a catalog type, fluently, so the menu definition stays one readable
     * line per item rather than growing an eleventh positional constructor argument.
     */
    governedBy(transactionType: string, ...alsoServes: string[]): AppMenuItem {
        this.transactionType = transactionType;
        this.transactionTypes = [transactionType, ...alsoServes];

        return this;
    }

    hasFeatureDependency(): boolean {
        return this.featureDependency !== undefined;
    }

    featureDependencySatisfied(): boolean {
        if (this.featureDependency) {
            return this.featureDependency();
        }

        return false;
    }
}
