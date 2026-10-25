import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  input,
  output,
  signal,
} from '@angular/core';
import { TpButton, TpInput, type TpInputValue } from '@team-platform/ui';

export interface ExactBirthDateFilter {
  mode: 'exact';
  date: number | null;
  month: number | null;
  year: number | null;
  displayValue: string;
}

export interface BirthDateAgeFilter {
  mode: 'age';
  fromAge: number | null;
  toAge: number | null;
  displayValue: string;
}

export type BirthDateFilterValue = ExactBirthDateFilter | BirthDateAgeFilter;

@Component({
  selector: 'app-birth-date-filter-menu',
  imports: [TpButton, TpInput],
  templateUrl: './birth-date-filter-menu.html',
  styleUrl: './birth-date-filter-menu.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BirthDateFilterMenu {
  filter = input<BirthDateFilterValue | null>(null);
  resetKey = input(0);

  apply = output<BirthDateFilterValue>();
  clear = output<void>();

  protected readonly day = signal<number | null>(null);
  protected readonly month = signal<number | null>(null);
  protected readonly year = signal<number | null>(null);
  protected readonly fromAge = signal<number | null>(null);
  protected readonly toAge = signal<number | null>(null);

  protected readonly exactHasValue = computed(
    () => this.day() !== null || this.month() !== null || this.year() !== null,
  );
  protected readonly ageHasValue = computed(() => this.fromAge() !== null || this.toAge() !== null);
  protected readonly hasAnyValue = computed(() => this.exactHasValue() || this.ageHasValue());
  protected readonly exactDisabled = computed(() => this.ageHasValue());
  protected readonly ageDisabled = computed(() => this.exactHasValue());
  protected readonly ageRangeInvalid = computed(() => {
    const fromAge = this.fromAge();
    const toAge = this.toAge();

    return (
      fromAge !== null &&
      toAge !== null &&
      this.isInRange(fromAge, 1, 200) &&
      this.isInRange(toAge, 1, 200) &&
      fromAge > toAge
    );
  });
  protected readonly canApply = computed(() => {
    if (this.exactHasValue()) {
      return (
        this.isInRange(this.day(), 1, 31) &&
        this.isInRange(this.month(), 1, 12) &&
        this.isAtLeast(this.year(), 1900)
      );
    }

    if (this.ageHasValue()) {
      return (
        this.isInRange(this.fromAge(), 1, 200) &&
        this.isInRange(this.toAge(), 1, 200) &&
        !this.ageRangeInvalid()
      );
    }

    return false;
  });

  private readonly resetDraftEffect = effect(() => {
    const filter = this.filter();
    this.resetKey();

    this.day.set(filter?.mode === 'exact' ? filter.date : null);
    this.month.set(filter?.mode === 'exact' ? filter.month : null);
    this.year.set(filter?.mode === 'exact' ? filter.year : null);
    this.fromAge.set(filter?.mode === 'age' ? filter.fromAge : null);
    this.toAge.set(filter?.mode === 'age' ? filter.toAge : null);
  });

  protected setDay(value: TpInputValue): void {
    this.day.set(this.toNumber(value));
  }

  protected setMonth(value: TpInputValue): void {
    this.month.set(this.toNumber(value));
  }

  protected setYear(value: TpInputValue): void {
    this.year.set(this.toNumber(value));
  }

  protected setFromAge(value: TpInputValue): void {
    this.fromAge.set(this.toNumber(value));
  }

  protected setToAge(value: TpInputValue): void {
    this.toAge.set(this.toNumber(value));
  }

  protected applyFilter(): void {
    if (!this.canApply()) return;

    if (this.exactHasValue()) {
      const parts = [
        this.day() === null ? null : `D = ${this.day()}`,
        this.month() === null ? null : `M = ${this.month()}`,
        this.year() === null ? null : `Y = ${this.year()}`,
      ].filter((part): part is string => part !== null);

      this.apply.emit({
        mode: 'exact',
        date: this.day(),
        month: this.month(),
        year: this.year(),
        displayValue: parts.join(', '),
      });
      return;
    }

    if (this.ageHasValue()) {
      const fromAge = this.fromAge();
      const toAge = this.toAge();
      const displayValue =
        fromAge !== null && toAge !== null
          ? `Age: ${fromAge} - ${toAge}`
          : fromAge !== null
            ? `Age >= ${fromAge}`
            : `Age <= ${toAge}`;

      this.apply.emit({
        mode: 'age',
        fromAge,
        toAge,
        displayValue,
      });
    }
  }

  private isInRange(value: number | null, min: number, max: number): boolean {
    return value === null || (Number.isInteger(value) && value >= min && value <= max);
  }

  private isAtLeast(value: number | null, min: number): boolean {
    return value === null || (Number.isInteger(value) && value >= min);
  }

  private toNumber(value: TpInputValue): number | null {
    if (value === null || value === '') return null;
    const numberValue = typeof value === 'number' ? value : Number(value);
    return Number.isFinite(numberValue) ? numberValue : null;
  }
}
