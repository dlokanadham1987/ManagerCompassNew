import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { ApiService } from '../../services/api.service';
import { AiSummaryComponent, DateRange } from '../../shared/ai-summary/ai-summary.component';
import { DirectReport, OneOnOneState, PostNote, ScheduledMeeting } from '../../models/one-on-one.model';

const STORAGE_KEY = 'mc-one-on-ones-v2';
const NEW_REPORT_VALUE = '__new__';

@Component({
  selector: 'app-one-on-one',
  standalone: true,
  imports: [CommonModule, FormsModule, AiSummaryComponent],
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

  offline = false; // true when the API couldn't be reached, so data is only kept in this browser
  saveError: string | null = null;

  aiSummaryLoadingId: string | null = null;
  aiSummaryError: { id: string; message: string } | null = null;

  constructor(private api: ApiService) {}

  postNotesOpenId: string | null = null;
  draftPostNoteText = '';
  draftFollowUp = '';
  draftActionText = '';
  draftActionItems: string[] = [];

  // Source of truth is the server file (App_Data/one-on-ones.json). localStorage is only a fallback copy: it is
  // imported once if the server file is still empty, and used if the API can't be reached.
  ngOnInit(): void {
    this.api.getOneOnOneState().subscribe({
      next: server => {
        if (server.reports?.length || server.meetings?.length) {
          this.apply(server);
          return;
        }
        // First run on the server: bring over this browser's existing notes, else start from sample data.
        const local = this.loadLocal();
        this.apply(local ?? this.seedWithSampleNotes());
        this.save();
      },
      error: () => {
        this.offline = true;
        this.apply(this.loadLocal() ?? this.seedWithSampleNotes());
      }
    });
  }

  private apply(state: OneOnOneState): void {
    this.reports = state.reports ?? [];
    this.meetings = state.meetings ?? [];
  }

  private seedWithSampleNotes(): OneOnOneState {
    const state = this.seedDefaults();
    this.apply(state);
    this.addSampleNotes();
    return { reports: this.reports, meetings: this.meetings };
  }

  // Sample history (2 dated post notes per seeded person, within the last 2 weeks) so the AI summary has
  // something to read on a fresh install.
  private addSampleNotes(): void {
    const samples: Record<string, { daysAgo: number; note: string; action: string; done: boolean }[]> = {
      Saritha: [
        { daysAgo: 9, note: 'Reviewed current project workload. Comfortable with priorities but flagged the release testing timeline as tight.', action: 'Share the testing timeline concern with the team lead', done: true },
        { daysAgo: 3, note: 'Talked about cross-team collaboration; discussed pairing with the platform team on the integration work.', action: 'Introduce Saritha to the platform team contact', done: false }
      ],
      Rishika: [
        { daysAgo: 10, note: 'Walked through last sprint items. Two tasks carried over because the requirements were unclear.', action: 'Clarify requirements for the reporting task', done: true },
        { daysAgo: 4, note: 'Interested in a stretch assignment on the dashboard work; wants more exposure to customer-facing demos.', action: 'Find a demo opportunity for next month', done: false }
      ],
      Srija: [
        { daysAgo: 8, note: 'Celebrated the successful demo. Team morale is good, with some concern about meeting-heavy days.', action: 'Review recurring meetings to protect focus time', done: true },
        { daysAgo: 2, note: 'Aligned on goals for the next sprint. She would like to mentor a new joiner.', action: 'Pair Srija with the new joiner', done: false }
      ]
    };

    for (const [name, entries] of Object.entries(samples)) {
      let report = this.reports.find(r => r.name === name);
      if (!report) {
        report = { id: this.makeId(), name, email: '' };
        this.reports.push(report);
      }
      for (const e of entries) {
        const when = new Date();
        when.setDate(when.getDate() - e.daysAgo);
        when.setHours(10, 0, 0, 0);
        this.meetings.push({
          id: this.makeId(),
          reportId: report.id,
          reportName: name,
          startsAt: this.toLocalDateTimeInput(when),
          durationMinutes: 30,
          preNotes: [],
          aiSummary: null,
          postNotes: [{ id: this.makeId(), date: when.toISOString().slice(0, 10), text: e.note }],
          actionItems: [{ text: e.action, done: e.done }],
          nextFollowUpDate: null,
          completed: true
        });
      }
    }
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

  // Sends this person's notes (all their meetings — the API applies the chosen date range by timestamp and adds
  // Team Notes comments) to the backend, which asks the AI model for the summary. The API key never reaches the browser.
  generateAiSummary(m: ScheduledMeeting, range: DateRange): void {
    if (this.aiSummaryLoadingId === m.id) return;
    this.aiSummaryLoadingId = m.id;
    this.aiSummaryError = null;

    const theirMeetings = this.meetings.filter(x => x.reportId === m.reportId);
    this.api.summarizeOneOnOne({
      reportName: m.reportName,
      from: range.from,
      to: range.to,
      postNotes: theirMeetings.flatMap(x => x.postNotes.map(n => ({ date: n.date, text: n.text }))),
      actionItems: theirMeetings.flatMap(x =>
        x.actionItems.map(a => ({ text: a.text, done: a.done, meetingDate: x.startsAt })))
    }).subscribe({
      next: res => {
        m.aiSummary = res.summary;
        m.aiSummaryPeriod = `${res.periodStart} to ${res.periodEnd}`;
        this.aiSummaryError = null;
        this.aiSummaryLoadingId = null;
        this.save();
      },
      error: (err: HttpErrorResponse) => {
        this.aiSummaryError = {
          id: m.id,
          message: typeof err.error === 'string' && err.error ? err.error : 'Could not generate the summary. Is the backend running?'
        };
        this.aiSummaryLoadingId = null;
      }
    });
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

  private loadLocal(): OneOnOneState | null {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (raw) return JSON.parse(raw);
    } catch {
      // unreadable — treat as no local copy
    }
    return null;
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
    const state: OneOnOneState = { reports: this.reports, meetings: this.meetings };
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(state)); // fallback copy
    } catch {
      // ignore storage errors (private browsing, quota)
    }
    if (this.offline) return;
    this.api.saveOneOnOneState(state).subscribe({
      error: () => (this.saveError = 'Could not save to the server. Your changes are kept in this browser only.')
    });
  }
}
