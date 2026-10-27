import { DateTime, DurationLike } from 'luxon';

export class ChainableDateTime {
    constructor(public readonly value: DateTime) {}

    static now(): ChainableDateTime {
        return new ChainableDateTime(DateTime.local());
    }

    static from(date: DateTime | Date): ChainableDateTime {
        return new ChainableDateTime(date instanceof Date ? DateTime.fromJSDate(date) : date);
    }

    plus(values: DurationLike): ChainableDateTime {
        return new ChainableDateTime(this.value.plus(values));
    }

    minus(values: DurationLike): ChainableDateTime {
        return new ChainableDateTime(this.value.minus(values));
    }

    plusDays(days: number): ChainableDateTime {
        return this.plus({ days });
    }

    minusDays(days: number): ChainableDateTime {
        return this.minus({ days });
    }

    plusSeconds(seconds: number): ChainableDateTime {
        return this.plus({ seconds });
    }

    startOf(unit: 'day' | 'week' | 'month'): ChainableDateTime {
        return new ChainableDateTime(this.value.startOf(unit));
    }

    endOf(unit: 'day' | 'week' | 'month'): ChainableDateTime {
        return new ChainableDateTime(this.value.endOf(unit));
    }

    toUTC(): ChainableDateTime {
        return new ChainableDateTime(this.value.toUTC());
    }

    toJSDate(): Date {
        return this.value.toJSDate();
    }

    toFormat(fmt: string): string {
        return this.value.toFormat(fmt);
    }

    toRelative(): string | null {
        return this.value.toRelative();
    }

    toDateTime(): DateTime {
        return this.value;
    }
}
