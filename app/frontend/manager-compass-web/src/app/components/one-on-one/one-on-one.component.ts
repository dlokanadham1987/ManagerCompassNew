import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DirectReport, PostNote, ScheduledMeeting } from '../../models/one-on-one.model';

const STORAGE_KEY = 'mc-one-on-ones-v2';
const NEW_REPORT_VALUE = '__new__';

@Component({
  selector: 'app-one-on-one',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './one-on-one.component.html'
})
export class OneOnOneComponent implements OnInit {
  reports: DirectReport[] = [];
  meetings: ScheduledMeeting[] = [];

  showScheduleModal = false;
  scheduleReportId = '';
  scheduleNewName = '';
  scheduleEmail = '';
  scheduleAt = '';
  scheduleDuration = 30;
  readonly newReportValue = NEW_REPORT_VALUE;

  aiSummaryLoadingId: string | null = null;

  postNotesOpenId: string | null = null;
  draftPostNoteText = '';
  draftFollowUp = '';
  draftActionText = '';
  draftActionItems: string[] = [];

  ngOnInit(): void {
    const state = this.load();
    this.reports = state.reports;
    this.meetings = state.meetings;
    this.save();
  }

  upcomingMeetings(): ScheduledMeeting[] {
    return this.meetings
      .filter(m => !m.completed)
      .sort((a, b) => a.startsAt.localeCompare(b.startsAt));
  }

  pastMeetings(): ScheduledMeeting[] {
    return this.meetings
      .filter(m => m.completed)
      .sort((a, b) => b.startsAt.localeCompare(a.startsAt));
  }

  dueSoonCount(): number {
    const seen = new Set<string>();
    let count = 0;
    for (const r of this.reports) {
      if (seen.has(r.id)) continue;
      seen.add(r.id);
      const days = this.daysUntilFollowUp(r.id);
      if (days !== null && days <= 7) count++;
    }
    return count;
  }

  openScheduleModal(): void {
    this.scheduleReportId = this.reports[0]?.id ?? NEW_REPORT_VALUE;
    this.scheduleNewName = '';
    this.scheduleEmail = '';
    this.scheduleAt = '';
    this.scheduleDuration = 30;
    this.showScheduleModal = true;
  }

  closeScheduleModal(): void {
    this.showScheduleModal = false;
  }

  submitSchedule(): void {
    if (!this.scheduleAt) return;

    let report: DirectReport | undefined;
    if (this.scheduleReportId === NEW_REPORT_VALUE) {
      const name = this.scheduleNewName.trim();
      if (!name) return;
      report = { id: this.makeId(), name, email: this.scheduleEmail.trim() };
      this.reports.push(report);
    } else {
      report = this.reports.find(r => r.id === this.scheduleReportId);
      if (!report) return;
      if (this.scheduleEmail.trim()) report.email = this.scheduleEmail.trim();
    }

    const meeting: ScheduledMeeting = {
      id: this.makeId(),
      reportId: report.id,
      reportName: report.name,
      startsAt: this.scheduleAt,
      durationMinutes: this.scheduleDuration,
      preNotes: this.suggestedPreNotes(report.id, report.name),
      aiSummary: null,
      postNotes: [],
      actionItems: [],
      nextFollowUpDate: null,
      completed: false
    };
    this.meetings.push(meeting);
    this.downloadIcs(meeting, report);
    this.save();
    this.closeScheduleModal();
  }

  // Mock only — no real activity source is wired up yet. Deliberately labeled illustrative in
  // the UI so it never reads as real data the way the rest of this app's real numbers do.
  generateAiSummary(m: ScheduledMeeting): void {
    if (m.aiSummary || this.aiSummaryLoadingId === m.id) return;
    this.aiSummaryLoadingId = m.id;
    setTimeout(() => {
      m.aiSummary =
        `Illustrative preview — not yet connected to a real activity feed. Once wired up, this will pull ` +
        `${m.reportName}'s last 2 weeks of activity (goals, tickets, engagement signals) and summarize it into ` +
        `2-3 talking points automatically. For now, use the pre notes below.`;
      this.aiSummaryLoadingId = null;
      this.save();
    }, 900);
  }

  // Opens the append panel for THIS meeting — any previously saved post notes stay visible
  // above it (see the template), so reopening always shows the running history, not a blank form.
  openPostNotes(m: ScheduledMeeting): void {
    this.postNotesOpenId = this.postNotesOpenId === m.id ? null : m.id;
    this.draftPostNoteText = '';
    this.draftFollowUp = m.nextFollowUpDate ?? '';
    this.draftActionText = '';
    this.draftActionItems = [];
  }

  closePostNotes(): void {
    this.postNotesOpenId = null;
  }

  addDraftAction(): void {
    const text = this.draftActionText.trim();
    if (!text) return;
    this.draftActionItems.push(text);
    this.draftActionText = '';
  }

  removeDraftAction(index: number): void {
    this.draftActionItems.splice(index, 1);
  }

  // Appends — never overwrites. Every save adds one more dated entry to the meeting's own
  // running post-notes log, so reopening this panel later always shows everything said so far.
  savePostNotes(m: ScheduledMeeting): void {
    const text = this.draftPostNoteText.trim();
    if (!text && this.draftActionItems.length === 0) return;

    if (text) {
      const note: PostNote = { id: this.makeId(), date: this.todayIso(), text };
      m.postNotes.push(note);
    }
    if (this.draftActionItems.length) {
      m.actionItems.push(...this.draftActionItems.map(t => ({ text: t, done: false })));
    }
    if (this.draftFollowUp) m.nextFollowUpDate = this.draftFollowUp;
    m.completed = true;

    this.save();
    this.closePostNotes();
  }

  toggleAction(m: ScheduledMeeting, index: number): void {
    m.actionItems[index].done = !m.actionItems[index].done;
    this.save();
  }

  private latestCompletedFor(reportId: string): ScheduledMeeting | undefined {
    return this.meetings
      .filter(m => m.reportId === reportId && m.completed)
      .sort((a, b) => b.startsAt.localeCompare(a.startsAt))[0];
  }

  private daysUntilFollowUp(reportId: string): number | null {
    const latest = this.latestCompletedFor(reportId)?.nextFollowUpDate;
    if (!latest) return null;
    const due = new Date(latest + 'T00:00:00');
    const today = new Date(this.todayIso() + 'T00:00:00');
    return Math.round((due.getTime() - today.getTime()) / 86400000);
  }

  // Real derivation from this manager's own logged history — not fabricated: pulls forward
  // open action items from the last completed meeting and flags an overdue follow-up if any.
  private suggestedPreNotes(reportId: string, reportName: string): string[] {
    const latest = this.latestCompletedFor(reportId);
    if (!latest) {
      return [`First 1:1 with ${reportName} — consider covering current priorities, blockers, and career goals.`];
    }
    const days = this.daysUntilFollowUp(reportId);
    const overdue = days !== null && days < 0 ? [`This follow-up is ${Math.abs(days)} day(s) overdue — check in on why.`] : [];
    const openItems = latest.actionItems.filter(a => !a.done).map(a => `Follow up: ${a.text}`);
    return [...overdue, ...openItems, "Ask what's top of mind for them this week."];
  }

  private downloadIcs(m: ScheduledMeeting, report: DirectReport): void {
    const start = new Date(m.startsAt);
    const end = new Date(start.getTime() + m.durationMinutes * 60000);
    const description = m.preNotes.length
      ? `Talking points:\\n${m.preNotes.map(t => `- ${t}`).join('\\n')}`
      : 'Regular 1:1 catch-up.';
    const fmt = (d: Date) => d.toISOString().replace(/[-:]/g, '').split('.')[0] + 'Z';

    const lines = [
      'BEGIN:VCALENDAR',
      'VERSION:2.0',
      'PRODID:-//Manager Compass//1:1 Scheduler//EN',
      'BEGIN:VEVENT',
      `UID:${m.id}@managercompass`,
      `DTSTAMP:${fmt(new Date())}`,
      `DTSTART:${fmt(start)}`,
      `DTEND:${fmt(end)}`,
      `SUMMARY:1:1 — ${report.name}`,
      `DESCRIPTION:${description}`,
    ];
    if (report.email.trim()) lines.push(`ATTENDEE;CN=${report.name}:mailto:${report.email.trim()}`);
    lines.push('END:VEVENT', 'END:VCALENDAR');

    const blob = new Blob([lines.join('\r\n')], { type: 'text/calendar' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `1-on-1-${report.name.replace(/\s+/g, '-')}.ics`;
    a.click();
    URL.revokeObjectURL(url);
  }

  private todayIso(): string {
    return new Date().toISOString().slice(0, 10);
  }

  private toLocalDateTimeInput(d: Date): string {
    const pad = (n: number) => String(n).padStart(2, '0');
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
  }

  private makeId(): string {
    return `${Date.now()}-${Math.random().toString(36).slice(2, 9)}`;
  }

  private load(): { reports: DirectReport[]; meetings: ScheduledMeeting[] } {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (raw) return JSON.parse(raw);
    } catch {
      // fall through to seed defaults
    }
    return this.seedDefaults();
  }

  // Sample-purpose starter data so the tab isn't empty on first visit — one upcoming meeting
  // per seeded team member, with illustrative (not sponsor-sourced) pre-notes as a stand-in
  // for what suggestedPreNotes() would generate once real history exists.
  private seedDefaults(): { reports: DirectReport[]; meetings: ScheduledMeeting[] } {
    const preNotesByName: Record<string, string[]> = {
      Saritha: [
        'Check in on current project workload and priorities.',
        'Discuss recent feedback on cross-team collaboration.',
        'Ask about career development interests for next quarter.'
      ],
      Rishika: [
        'Review progress on items from the last discussion.',
        'Talk through any blockers or support needed.',
        'Explore interest in upcoming stretch assignments.'
      ],
      Srija: [
        'Discuss recent wins and highlights.',
        'Check in on team dynamics and morale.',
        'Align on goals for the next sprint/cycle.'
      ]
    };

    const reports: DirectReport[] = Object.keys(preNotesByName).map(name => ({
      id: this.makeId(),
      name,
      email: ''
    }));

    const meetings: ScheduledMeeting[] = reports.map((r, i) => {
      const start = new Date();
      start.setDate(start.getDate() + 1);
      start.setHours(10 + i, 0, 0, 0);
      return {
        id: this.makeId(),
        reportId: r.id,
        reportName: r.name,
        startsAt: this.toLocalDateTimeInput(start),
        durationMinutes: 30,
        preNotes: preNotesByName[r.name],
        aiSummary: null,
        postNotes: [],
        actionItems: [],
        nextFollowUpDate: null,
        completed: false
      };
    });

    return { reports, meetings };
  }

  private save(): void {
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify({ reports: this.reports, meetings: this.meetings }));
    } catch {
      // ignore storage errors (private browsing, quota)
    }
  }
}
