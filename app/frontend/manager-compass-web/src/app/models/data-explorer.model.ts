export interface DatasetInfo {
  key: string;
  label: string;
  fileName: string;
  rowCount: number;
}

export interface PagedColumnResult {
  dataset: string;
  column: string;
  totalRows: number;
  page: number;
  pageSize: number;
  totalPages: number;
  values: string[];
}

export interface IdentifierGridResult {
  dataset: string;
  columns: string[];
  totalRows: number;
  page: number;
  pageSize: number;
  totalPages: number;
  rows: string[][];
}
