import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../services/api.service';
import { Snapshot } from '../../models/snapshot.model';

@Component({
  selector: 'app-people-snapshot',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './people-snapshot.component.html'
})
export class PeopleSnapshotComponent implements OnInit {
  snapshot: Snapshot | null = null;

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.api.getSnapshot().subscribe(s => (this.snapshot = s));
  }

  barHeightPct(value: number, values: number[]): number {
    const max = Math.max(...values);
    return max ? Math.round((value / max) * 100) : 0;
  }

  loadedAtLabel(loadedAtUtc: string): string {
    return new Date(loadedAtUtc).toLocaleString(undefined, {
      dateStyle: 'medium',
      timeStyle: 'short'
    });
  }

  maxReqCount(rows: { count: number }[]): number {
    return Math.max(...rows.map(r => r.count), 1);
  }

  totalReqCount(rows: { count: number }[]): number {
    return rows.reduce((sum, r) => sum + r.count, 0);
  }
}
