import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../services/api.service';
import { DatasetInfo, IdentifierGridResult } from '../../models/data-explorer.model';

const GRID_TITLES: Record<string, string> = {
  active_fte: 'Employee Directory',
  exits: 'Former Employee Directory',
  contractors: 'Contractor & Intern Directory',
  requisitions: 'Requisition Directory'
};

@Component({
  selector: 'app-data-explorer',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './data-explorer.component.html'
})
export class DataExplorerComponent implements OnInit {
  datasets: DatasetInfo[] = [];
  selectedDataset = '';

  // A real multi-column, multi-row grid — safe because these columns weren't independently
  // shuffled (they're the same synthetic ID repeated a few ways).
  idGrid: IdentifierGridResult | null = null;
  idPage = 1;
  idPageSize = 25;
  idLoading = false;
  idError: string | null = null;
  columnFilters: Record<string, string> = {};
  private filterDebounce: ReturnType<typeof setTimeout> | null = null;

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.api.getDatasets().subscribe(list => {
      this.datasets = list;
      if (list.length) this.onDatasetChange(list[0].key);
    });
  }

  get gridTitle(): string {
    return GRID_TITLES[this.selectedDataset] ?? 'Record Directory';
  }

  onDatasetChange(key: string): void {
    this.selectedDataset = key;
    this.idPage = 1;
    this.idGrid = null;
    this.columnFilters = {};
    this.loadIdentifiers();
  }

  goToIdPage(p: number): void {
    if (p < 1 || (this.idGrid && p > this.idGrid.totalPages)) return;
    this.idPage = p;
    this.loadIdentifiers();
  }

  onColumnFilterChange(column: string, value: string): void {
    this.columnFilters = { ...this.columnFilters, [column]: value };
    this.idPage = 1;
    if (this.filterDebounce) clearTimeout(this.filterDebounce);
    this.filterDebounce = setTimeout(() => this.loadIdentifiers(), 250);
  }

  clearFilters(): void {
    this.columnFilters = {};
    this.idPage = 1;
    this.loadIdentifiers();
  }

  get hasActiveFilters(): boolean {
    return Object.values(this.columnFilters).some(v => v && v.trim());
  }

  private loadIdentifiers(): void {
    this.idLoading = true;
    this.idError = null;
    this.api.getIdentifierGrid(this.selectedDataset, this.idPage, this.idPageSize, this.columnFilters).subscribe({
      next: res => { this.idGrid = res; this.idLoading = false; },
      error: () => { this.idError = 'Could not load identifier data — the sample dataset may not be available on this machine.'; this.idLoading = false; }
    });
  }

  idRowIndex(i: number): number {
    return (this.idPage - 1) * this.idPageSize + i + 1;
  }
}
