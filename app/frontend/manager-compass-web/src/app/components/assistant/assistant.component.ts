import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { delay } from 'rxjs/operators';
import { ApiService } from '../../services/api.service';
import { Topic, AskResponse } from '../../models/topic.model';
import { Snapshot } from '../../models/snapshot.model';
import { TopicIconComponent } from '../../shared/topic-icon.component';

interface LiveStat {
  value: string;
  sub?: string;
}

interface CurrentAnswer {
  question: string;
  response: AskResponse | null;
  loading: boolean;
}

interface TopicGroup {
  label: string;
  topics: Topic[];
}

interface PromptChip {
  text: string;
  color: string;
  key: string;
}

interface QuickRef {
  key: string;
  label: string;
  summary: string;
  points: string[];
  url: string;
}

const QUICK_REFS: QuickRef[] = [
  {
    key: 'launch',
    label: 'RealPage Launch',
    summary: 'The RealPage launch portal: a single starting point to reach company applications and resources.',
    points: [
      'Use it as your jumping-off page to find internal tools and sites',
      'Bookmark it for quick access to the systems you use most',
      'If a tool you expect is missing, ask your HRBP or IT'
    ],
    url: 'https://launch.realpage.com/'
  },
  {
    key: 'learning',
    label: 'RealPage Learning',
    summary: 'The learning platform for training courses and development for you and your team.',
    points: [
      'Find and assign required or recommended courses for your team',
      'Encourage direct reports to complete assigned learning on time',
      'Use it alongside development goals set in performance conversations'
    ],
    url: 'https://www.realpagelearning.com/v5/login/65/'
  },
  {
    key: 'dayforce',
    label: 'Dayforce (HCM)',
    summary: 'The HR, time and payroll system, signed in through RealPage single sign-on.',
    points: [
      'Review and approve your team\'s time and time-off requests',
      'Look up team information and personal details in HCM',
      'Payroll or pay questions about an individual go to HR, not through this assistant'
    ],
    url: 'https://sso.dayforcehcm.com/realpage'
  },
  {
    key: 'concur',
    label: 'Concur (Travel & Expense)',
    summary: 'Used to book business travel and to submit and approve expense reports.',
    points: [
      'Submit expense reports with receipts promptly after a trip or purchase',
      'Approve your direct reports\' expense reports as the approving manager',
      'Check the travel and expense policy before booking'
    ],
    url: 'https://www.concursolutions.com/'
  },
  {
    key: 'mediassist',
    label: 'Medi Assist (Health Benefits, India)',
    summary: 'The Medi Assist portal for health insurance: e-cards, claims and network hospitals.',
    points: [
      'Employees use it to download health e-cards and track claims',
      'Point team members here for health insurance claim questions',
      'Plan details and eligibility questions go to HR / the Benefits Summary'
    ],
    url: 'https://portal.mediassist.in/Home.aspx'
  }
];

const STARTER_KEYS = ['engagement', 'compensation', 'retention', 'recruiting', 'onboarding'];

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

@Component({
  selector: 'app-assistant',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, TopicIconComponent],
  templateUrl: './assistant.component.html'
})
export class AssistantComponent implements OnInit {
  topicGroups: TopicGroup[] = [];
  starterTopics: Topic[] = [];
  current: CurrentAnswer | null = null;
  activeKey: string | null = null;
  activeTopic: Topic | null = null;
  inputText = '';
  quickRefs = QUICK_REFS;
  quickRefOpen = false;
  activeRef: QuickRef | null = null;
  private snapshot: Snapshot | null = null;

  constructor(private api: ApiService) {}

  get promptsHeading(): string {
    return this.activeTopic ? `More on ${this.activeTopic.label}` : 'Try one of these';
  }

  get promptChips(): PromptChip[] {
    if (this.activeTopic?.examplePrompts.length) {
      return this.activeTopic.examplePrompts.map(text => ({
        text,
        color: this.activeTopic!.color,
        key: this.activeTopic!.key
      }));
    }
    return this.starterTopics.map(t => ({ text: t.prompt, color: t.color, key: t.key }));
  }

  toggleQuickRef(): void {
    this.quickRefOpen = !this.quickRefOpen;
  }

  openQuickRef(ref: QuickRef): void {
    this.activeRef = ref;
    this.activeKey = null;
    this.activeTopic = null;
    this.current = null;
  }

  askPromptChip(chip: PromptChip): void {
    this.activeRef = null;
    this.activeKey = chip.key;
    this.submitAsk(chip.text, chip.key);
  }

  ngOnInit(): void {
    this.api.getTopics().subscribe(topics => {
      this.topicGroups = GROUP_ORDER.map(label => ({
        label,
        topics: topics.filter(t => GROUP_MAP[t.key] === label)
      })).filter(g => g.topics.length > 0);

      this.starterTopics = STARTER_KEYS
        .map(key => topics.find(t => t.key === key))
        .filter((t): t is Topic => !!t);
    });

    this.api.getSnapshot().subscribe(s => (this.snapshot = s));
  }

  // Only topics with a genuinely real, already-validated aggregate in the People Snapshot get a
  // live stat here â€” e.g. Engagement has no source column in the sample dataset, so it's
  // deliberately left out rather than showing a fabricated number.
  liveStat(topicKey: string): LiveStat | null {
    const s = this.snapshot;
    if (!s) return null;

    switch (topicKey) {
      case 'recruiting':
        return {
          value: s.requisitionStatus.map(r => `${r.count} ${r.status.toLowerCase()}`).join(' Â· ')
        };
      case 'retention': {
        const exits = s.kpis.find(k => k.label.startsWith('Exits'));
        const topReason = s.attritionByReason[0];
        if (!exits) return null;
        return {
          value: `${exits.value} exits sampled (${exits.subText})`,
          sub: topReason ? `Top reason: ${topReason.label} â€” ${topReason.percent}%` : undefined
        };
      }
      case 'teamhealth': {
        const headcount = s.kpis.find(k => k.label === 'Active headcount');
        if (!headcount) return null;
        return { value: `${headcount.value} active headcount company-wide` };
      }
      default:
        return null;
    }
  }

  askTopic(topic: Topic): void {
    this.activeRef = null;
    this.activeKey = topic.key;
    this.submitAsk(topic.prompt, topic.key);
  }

  onSubmit(): void {
    const text = this.inputText.trim();
    if (!text) return;
    this.activeRef = null;
    this.activeKey = null;
    this.inputText = '';
    this.submitAsk(text);
  }

  private submitAsk(text: string, topicKey?: string): void {
    // Replaces whatever is currently shown â€” selecting a new topic or asking a new
    // question always swaps the middle panel's content rather than stacking a history.
    const entry: CurrentAnswer = { question: text, response: null, loading: true };
    this.current = entry;

    this.api
      .ask(text, topicKey)
      .pipe(delay(350))
      .subscribe(response => {
        if (this.current !== entry) return; // a newer request superseded this one
        entry.response = response;
        entry.loading = false;
        if (response.type === 'topic' && response.topic) {
          this.activeTopic = response.topic;
        } else {
          this.activeTopic = null;
        }
      });
  }
}
