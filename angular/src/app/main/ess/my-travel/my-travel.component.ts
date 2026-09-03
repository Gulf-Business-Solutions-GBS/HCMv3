import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import {
    EntityDto,
    EssLookupServiceProxy,
    GetMyRequestsInput,
    MyTravelDto,
    MyTravelLegDto,
    MyTravelServiceProxy,
    RequestTravelCostInput,
    RequestTravelInput,
    RequestTravelLegInput,
    SimpleLookupDto,
    TravelTypeLookupDto,
} from '@shared/service-proxies/service-proxies';
import { DateTime } from 'luxon';
import { Observable } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { SubHeaderComponent } from '../../../shared/common/sub-header/sub-header.component';
import { WorkflowHistoryModalComponent } from '../../../shared/common/workflow-activity/workflow-history-modal.component';
import { MyRequestComponentBase } from '../my-request-component-base';

/** One expected cost on a leg, as the form holds it before it becomes a {@link RequestTravelCostInput}. */
interface TravelCostForm {
    expenseTypeId: number | null;
    currencyId: number | null;
    amount: number | null;
    comments: string;
}

/** One leg of the trip, as the form holds it. Dates and times are kept as the raw input strings. */
interface TravelLegForm {
    departureId: number | null;
    arrivalId: number | null;
    departureDate: string;
    departureTime: string;
    arrivalDate: string;
    arrivalTime: string;
    hotelId: number | null;
    travelAirlineId: number | null;
    nightStay: boolean;
    totalNights: number | null;
    isVisaRequired: boolean;
    visaComments: string;
    isHotelRequired: boolean;
    hotelComments: string;
    anyOtherExpense: boolean;
    otherComments: string;
    comments: string;
    costs: TravelCostForm[];
}

/**
 * Self-service business travel requests -- the request for the trip, not the claim for what it cost
 * afterwards.
 *
 * <p>A trip is a header with one or more legs, and each leg carries its own expected costs. The form nests
 * the same way the request does, because a cost only means anything against the leg it belongs to: "700 for
 * a flight" is unreadable without knowing which flight. The header total is never typed -- it is the sum of
 * the legs, which are themselves the sums of their cost lines, so the figure the approver sees cannot
 * disagree with the lines it came from.</p>
 *
 * <p>Amounts are Kuwaiti dinar: three decimal places throughout, entered in thousandths and displayed the
 * same way.</p>
 */
@Component({
    templateUrl: './my-travel.component.html',
    animations: [appModuleAnimation()],
    imports: [CommonModule, FormsModule, SubHeaderComponent, LocalizePipe, LuxonFormatPipe, WorkflowHistoryModalComponent],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyTravelComponent extends MyRequestComponentBase<MyTravelDto> implements OnInit {
    private readonly _service = inject(MyTravelServiceProxy);
    private readonly _lookups = inject(EssLookupServiceProxy);

    readonly travelTypes = signal<TravelTypeLookupDto[]>([]);
    readonly destinations = signal<SimpleLookupDto[]>([]);
    readonly expenseTypes = signal<SimpleLookupDto[]>([]);
    readonly currencies = signal<SimpleLookupDto[]>([]);
    readonly hotels = signal<SimpleLookupDto[]>([]);
    readonly airlines = signal<SimpleLookupDto[]>([]);

    /**
     * True when there are no destinations configured, which is the one remaining way a leg cannot be
     * filled in. It used to mean "these lookups are behind a travel-desk permission the employee does not
     * hold"; that is no longer possible, so an empty list now means only that HR has configured none.
     */
    readonly noDestinationsConfigured = computed(() => this.destinations().length === 0);

    /** The row whose legs are expanded in the list, if any. */
    readonly expandedRowId = signal<number | null>(null);

    header = { travelTypeId: null as number | null, startDate: '', endDate: '', travelPurpose: '', comments: '' };

    readonly legs = signal<TravelLegForm[]>([]);

    ngOnInit(): void {
        this._service.getTravelTypes().subscribe({
            next: (result) => this.travelTypes.set(result ?? []),
            error: () => this.travelTypes.set([]),
        });

        // All five come from the self-service lookup service, so none of them can fail for want of a
        // travel-desk permission any more. Hotel, airline and currency remain optional on a leg; a
        // destination is not, which is why only that one drives a notice on the form.
        this._lookups.getTravelDestinations().subscribe({
            next: (result) => this.destinations.set(result ?? []),
            error: () => this.destinations.set([]),
        });

        this._lookups.getExpenseTypes().subscribe({
            next: (result) => this.expenseTypes.set(result ?? []),
            error: () => this.expenseTypes.set([]),
        });

        this._lookups.getHotels().subscribe({
            next: (result) => this.hotels.set(result ?? []),
            error: () => this.hotels.set([]),
        });

        this._lookups.getTravelAirlines().subscribe({
            next: (result) => this.airlines.set(result ?? []),
            error: () => this.airlines.set([]),
        });

        this._lookups.getCurrencies().subscribe({
            next: (result) => this.currencies.set(result ?? []),
            error: () => this.currencies.set([]),
        });

        this.reload();
    }

    protected fetch(input: GetMyRequestsInput): Observable<{ items: MyTravelDto[] }> {
        return this._service.getMyTravels(input);
    }

    protected cancelRequest(input: EntityDto): Observable<void> {
        return this._service.cancelMyTravel(input);
    }

    protected resetForm(): void {
        this.header = { travelTypeId: null, startDate: '', endDate: '', travelPurpose: '', comments: '' };

        // A trip with no legs is not a request anybody can approve, so the form opens with one already
        // there rather than asking the employee to work out that they need to add it.
        this.legs.set([this.newLeg()]);
        this.expandedRowId.set(null);
    }

    addLeg(): void {
        this.legs.update((legs) => [...legs, this.newLeg()]);
    }

    removeLeg(index: number): void {
        this.legs.update((legs) => legs.filter((_, i) => i !== index));
    }

    addCost(legIndex: number): void {
        this.legs.update((legs) =>
            legs.map((leg, i) =>
                i === legIndex
                    ? { ...leg, costs: [...leg.costs, { expenseTypeId: null, currencyId: null, amount: null, comments: '' }] }
                    : leg
            )
        );
    }

    removeCost(legIndex: number, costIndex: number): void {
        this.legs.update((legs) =>
            legs.map((leg, i) => (i === legIndex ? { ...leg, costs: leg.costs.filter((_, c) => c !== costIndex) } : leg))
        );
    }

    /** The leg's expected cost: the sum of its lines, never typed in. */
    legTotal(leg: TravelLegForm): number {
        return this.round3(leg.costs.reduce((sum, cost) => sum + (cost.amount ?? 0), 0));
    }

    /** The trip's expected cost, summed the same way the server sums it. */
    formTotal(): number {
        return this.round3(this.legs().reduce((sum, leg) => sum + this.legTotal(leg), 0));
    }

    submit(): void {
        if (this.saving()) {
            return;
        }

        if (!this.header.travelTypeId) {
            this.message.warn(this.l('PleaseSelect'));
            return;
        }

        if (!this.header.startDate || !this.header.endDate) {
            this.message.warn(this.l('Required'));
            return;
        }

        if (this.header.endDate < this.header.startDate) {
            this.message.warn(this.l('EndDateCannotBeBeforeStartDate'));
            return;
        }

        const legs = this.legs();

        if (legs.length === 0) {
            this.message.warn(this.l('ATravelRequestMustHaveAtLeastOneLeg'));
            return;
        }

        // Every rule below is enforced by the server too. They are repeated here so the employee is told
        // which leg is wrong while the form is still in front of them.
        for (const leg of legs) {
            if (!leg.departureId || !leg.arrivalId) {
                this.message.warn(this.l('EachLegNeedsADepartureAndAnArrival'));
                return;
            }

            if (leg.departureId === leg.arrivalId) {
                this.message.warn(this.l('DepartureAndArrivalCannotBeTheSame'));
                return;
            }

            if (!leg.departureDate || !leg.departureTime || !leg.arrivalDate || !leg.arrivalTime) {
                this.message.warn(this.l('EachLegNeedsDepartureAndArrivalDatesAndTimes'));
                return;
            }

            if (`${leg.arrivalDate}T${leg.arrivalTime}` < `${leg.departureDate}T${leg.departureTime}`) {
                this.message.warn(this.l('ArrivalCannotBeBeforeDeparture'));
                return;
            }

            for (const cost of leg.costs) {
                if (!cost.expenseTypeId || cost.amount === null || cost.amount === undefined || cost.amount <= 0) {
                    this.message.warn(this.l('EachCostNeedsAnExpenseTypeAndAnAmount'));
                    return;
                }
            }
        }

        const input = new RequestTravelInput();
        input.travelTypeId = this.header.travelTypeId;
        input.startDate = DateTime.fromISO(this.header.startDate);
        input.endDate = DateTime.fromISO(this.header.endDate);
        input.travelPurpose = this.header.travelPurpose;
        input.comments = this.header.comments;
        input.legs = legs.map((leg) => this.toLegInput(leg));

        this.saving.set(true);

        this._service
            .requestTravel(input)
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe((result) => this.notifySubmitted(result.submittedForApproval));
    }

    toggleRow(row: MyTravelDto): void {
        this.expandedRowId.update((current) => (current === row.id ? null : row.id));
    }

    typeName(row: MyTravelDto): string {
        return this.pick(row.travelTypeNameEn, row.travelTypeNameAr);
    }

    lookupName(lookup: SimpleLookupDto): string {
        return this.pick(lookup.nameEn, lookup.nameAr);
    }

    travelTypeName(type: TravelTypeLookupDto): string {
        return this.pick(type.nameEn, type.nameAr);
    }

    currencyName(currency: SimpleLookupDto): string {
        return currency.code || this.pick(currency.nameEn, currency.nameAr);
    }

    legRoute(leg: MyTravelLegDto): string {
        const from = this.pick(leg.departureNameEn, leg.departureNameAr);
        const to = this.pick(leg.arrivalNameEn, leg.arrivalNameAr);
        return `${from} → ${to}`;
    }

    private newLeg(): TravelLegForm {
        return {
            departureId: null,
            arrivalId: null,
            departureDate: '',
            departureTime: '',
            arrivalDate: '',
            arrivalTime: '',
            hotelId: null,
            travelAirlineId: null,
            nightStay: false,
            totalNights: 0,
            isVisaRequired: false,
            visaComments: '',
            isHotelRequired: false,
            hotelComments: '',
            anyOtherExpense: false,
            otherComments: '',
            comments: '',
            costs: [],
        };
    }

    private toLegInput(leg: TravelLegForm): RequestTravelLegInput {
        const input = new RequestTravelLegInput();

        input.departureId = leg.departureId as number;
        input.arrivalId = leg.arrivalId as number;
        input.departureDate = DateTime.fromISO(leg.departureDate);

        // The time fields are their own columns on the server but arrive as full timestamps, so each is
        // anchored to the date it belongs to rather than to today.
        input.departureTime = DateTime.fromISO(`${leg.departureDate}T${leg.departureTime}`);
        input.arrivalDate = DateTime.fromISO(leg.arrivalDate);
        input.arrivalTime = DateTime.fromISO(`${leg.arrivalDate}T${leg.arrivalTime}`);
        input.hotelId = leg.hotelId ?? undefined;
        input.travelAirlineId = leg.travelAirlineId ?? undefined;
        input.nightStay = leg.nightStay;
        input.totalNights = leg.totalNights ?? 0;
        input.isVisaRequired = leg.isVisaRequired;
        input.visaComments = leg.visaComments;
        input.isHotelRequired = leg.isHotelRequired;
        input.hotelComments = leg.hotelComments;
        input.anyOtherExpense = leg.anyOtherExpense;
        input.otherComments = leg.otherComments;
        input.comments = leg.comments;

        input.costs = leg.costs.map((cost) => {
            const costInput = new RequestTravelCostInput();
            costInput.expenseTypeId = cost.expenseTypeId as number;
            costInput.currencyId = cost.currencyId ?? undefined;

            // Dinar is three decimal places; rounding here keeps what was typed and what is stored the same.
            costInput.amount = this.round3(cost.amount ?? 0);
            costInput.comments = cost.comments;
            return costInput;
        });

        return input;
    }

    private round3(value: number): number {
        return Math.round((value + Number.EPSILON) * 1000) / 1000;
    }
}
