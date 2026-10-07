import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../services/api.service';
import { AdoptionMetrics, TopicAdoptionStat } from '../../models/adoption.model';
import { TopicIconComponent } from '../../shared/topic-icon.component';

type KpiKey = 'activeManagers' | 'questions' | 'checklist' | 'escalations' | 'medianTime';

@Component({
  selector: 'app-adoption',
  standalone: true,
  imports: [CommonModule, TopicIconComponent],
  templateUrl: './adoption.component.html'
})
export class AdoptionComponent implements OnInit {
  metrics: AdoptionMetrics | null = null;
  expandedKpi: KpiKey | null = null;

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.api.getAdoptionMetrics().subscribe(m => (this.metrics = m));
  }

  toggleKpi(key: KpiKey): void {
    this.expandedKpi = this.expandedKpi === key ? null : key;
  }

  adoptionPct(m: AdoptionMetrics): number {
    return Math.round((m.activeManagers / m.eligibleManagers) * 100);
  }

  maxCount(m: AdoptionMetrics): number {
    return Math.max(...m.topTopics.map(t => t.count), 1);
  }

  barHeightPct(value: number, values: number[]): number {
    const max = Math.max(...values);
    return Math.round((value / max) * 100);
  }

  private withData(m: AdoptionMetrics): TopicAdoptionStat[] {
    return m.topicBreakdown.filter(t => t.count > 0);
  }

  topicsByCount(m: AdoptionMetrics): TopicAdoptionStat[] {
    return [...this.withData(m)].sort((a, b) => b.count - a.count);
  }

  topicsByChecklist(m: AdoptionMetrics): TopicAdoptionStat[] {
    return [...this.withData(m)].sort((a, b) => (b.checklistCompletionRatePct ?? 0) - (a.checklistCompletionRatePct ?? 0));
  }

  topicsByEscalation(m: AdoptionMetrics): TopicAdoptionStat[] {
    return [...this.withData(m)].sort((a, b) => (b.escalationRatePct ?? 0) - (a.escalationRatePct ?? 0));
  }

  topicsByMedian(m: AdoptionMetrics): TopicAdoptionStat[] {
    return [...this.withData(m)].sort((a, b) => (a.medianSeconds ?? 0) - (b.medianSeconds ?? 0));
  }
}
