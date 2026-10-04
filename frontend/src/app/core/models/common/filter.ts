import { FilterOperator, TemporalPartType } from '../../enums/filter';

export { FilterOperator, TemporalPartType } from '../../enums/filter';

export interface FilterCriterionRequest {
  fieldName: string;
  operator: FilterOperator;
  values: string[];
  dateTimeFilterOptions?: {
    offsetMinutes: number;
    temporalPartType: TemporalPartType;
  };
}

export interface SortFieldRequest {
  fieldName: string;
  isAscending: boolean;
}

/** Request contract shared by APIs that accept the backend generic filter. */
export interface FilterRequest {
  currentPage: number;
  itemsPerPage: number;
  searchGlobalText?: string;
  groupPattern?: string;
  filterCriteria: FilterCriterionRequest[];
  sortFields: SortFieldRequest[];
}

/** Response contract shared by APIs that return the backend generic filter. */
export interface FilterResult<TItem> {
  items: TItem[];
  currentPage: number;
  itemsPerPage: number;
  totalItems: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}
