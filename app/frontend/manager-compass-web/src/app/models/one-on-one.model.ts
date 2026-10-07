export interface ActionItem {
  text: string;
  done: boolean;
}

export interface DirectReport {
  id: string;
  name: string;
  email: string;
}

export interface PostNote {
  id: string;
  date: string; // yyyy-MM-dd, when this note was added
  text: string;
}

export interface ScheduledMeeting {
  id: string;
  reportId: string;
  reportName: string;
  startsAt: string; // datetime-local value, from the schedule form
  durationMinutes: number;
  preNotes: string[]; // talking points prepared before the conversation
  aiSummary: string | null; // mock/illustrative preview only — see aiSummaryLoading in the component
  postNotes: PostNote[]; // appended over time — every save adds a new dated entry, never overwrites
  actionItems: ActionItem[]; // accumulates as post-notes are appended
  nextFollowUpDate: string | null; // most recently set value
  completed: boolean;
}
