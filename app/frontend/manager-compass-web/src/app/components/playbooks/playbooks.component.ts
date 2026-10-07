import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../services/api.service';
import { Topic } from '../../models/topic.model';
import { TopicIconComponent } from '../../shared/topic-icon.component';

interface TopicGroup {
  label: string;
  topics: Topic[];
}

const GROUP_ORDER = ['Hiring', 'People & Performance', 'Pay & Policy'];
const GROUP_MAP: Record<string, string> = {
  recruiting: 'Hiring',
  onboarding: 'Hiring',
  engagement: 'People & Performance',
  retention: 'People & Performance',
  performance: 'People & Performance',
  teamhealth: 'People & Performance',
  compensation: 'Pay & Policy',
  payroll: 'Pay & Policy',
  policy: 'Pay & Policy'
};

// Playbooks are a durable reference, not a one-off conversation — progress is saved
// per topic on this device so it survives switching tabs or coming back later.
const STORAGE_KEY = 'mc-playbook-checklist';

@Component({
  selector: 'app-playbooks',
  standalone: true,
  imports: [CommonModule, TopicIconComponent],
  templateUrl: './playbooks.component.html'
})
export class PlaybooksComponent implements OnInit {
  topicGroups: TopicGroup[] = [];
  selected: Topic | null = null;
  private checkedMap: Record<string, boolean[]> = this.loadChecked();

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.api.getTopics().subscribe(topics => {
      this.topicGroups = GROUP_ORDER.map(label => ({
        label,
        topics: topics.filter(t => GROUP_MAP[t.key] === label)
      })).filter(g => g.topics.length > 0);

      for (const t of topics) {
        const saved = this.checkedMap[t.key];
        if (!saved || saved.length !== t.checklist.length) {
          this.checkedMap[t.key] = new Array(t.checklist.length).fill(false);
        }
      }
    });
  }

  select(topic: Topic): void {
    this.selected = topic;
  }

  backToLibrary(): void {
    this.selected = null;
  }

  isChecked(topicKey: string, index: number): boolean {
    return !!this.checkedMap[topicKey]?.[index];
  }

  toggle(topicKey: string, index: number): void {
    const items = this.checkedMap[topicKey];
    if (!items) return;
    items[index] = !items[index];
    this.saveChecked();
  }

  doneCount(topic: Topic): number {
    return (this.checkedMap[topic.key] || []).filter(Boolean).length;
  }

  progressPct(topic: Topic): number {
    const total = topic.checklist.length;
    if (!total) return 0;
    return Math.round((this.doneCount(topic) / total) * 100);
  }

  private loadChecked(): Record<string, boolean[]> {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      return raw ? JSON.parse(raw) : {};
    } catch {
      return {};
    }
  }

  private saveChecked(): void {
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(this.checkedMap));
    } catch {
      // ignore storage errors (private browsing, quota)
    }
  }
}
