import { Component, EventEmitter, HostListener, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

export interface DateRange {
  from: string; // yyyy-MM-dd
  to: string;
}

const DEFAULT_DAYS = 14;

/**
 * Reusable "Generate AI summary" control: a button plus a calendar date-range picker (default: last 14 days).
 * It owns only the UI. The host page supplies the summary/loading/error state and does the actual API call
 * when `generate` fires, so the same control can sit on any page that has timestamped data to summarize.
 */
@Component({
  selector: 'app-ai-summary',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './ai-summary.component.html',
  styleUrl: './ai-summary.component.css'
})
export class AiSummaryComponent {
  @Input() summary: string | null = null;
  @Input() period: string | null = null; // shown with the summary, e.g. "2026-09-27 to 2026-10-11"
  @Input() loading = false;
  @Input() error: string | null = null;
  @Input() hint = '';
  @Input() idPrefix = 'ai';
  @Output() generate = new EventEmitter<DateRange>();

  readonly presets = [
    { label: 'Last 2 weeks', days: 14 },
    { label: 'Last 3 months', days: 90 },
    { label: 'Last year', days: 365 }
  ];

  pickerOpen = false;
  from = this.isoDaysAgo(DEFAULT_DAYS);
  to = this.isoDaysAgo(0);
  readonly today = this.isoDaysAgo(0);

  get rangeValid(): boolean {
    return !!this.from && !!this.to && this.from <= this.to;
  }

  get rangeLabel(): string {
    const preset = this.presets.find(p => this.from === this.isoDaysAgo(p.days) && this.to === this.today);
    if (preset) return preset.label;
    return `${this.shortDate(this.from)} – ${this.shortDate(this.to)}`;
  }

  togglePicker(): void {
    this.pickerOpen = !this.pickerOpen;
  }

  @HostListener('document:keydown.escape')
  closePicker(): void {
    this.pickerOpen = false;
  }

  choosePreset(days: number): void {
    this.from = this.isoDaysAgo(days);
    this.to = this.today;
  }

  isPresetActive(days: number): boolean {
    return this.from === this.isoDaysAgo(days) && this.to === this.today;
  }

  run(): void {
    if (this.loading || !this.rangeValid) return;
    this.pickerOpen = false;
    this.generate.emit({ from: this.from, to: this.to });
  }

  // Local-calendar yyyy-MM-dd (not toISOString, which would shift the day for UTC offsets).
  private isoDaysAgo(days: number): string {
    const d = new Date();
    d.setDate(d.getDate() - days);
    const pad = (n: number) => String(n).padStart(2, '0');
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
  }

  private shortDate(iso: string): string {
    const [y, m, d] = iso.split('-').map(Number);
    return new Date(y, m - 1, d).toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' });
  }
}
