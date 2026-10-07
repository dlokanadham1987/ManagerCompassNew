export interface Topic {
  key: string;
  label: string;
  blurb: string;
  color: string;
  contact: string;
  prompt: string;
  answer: string;
  sources: string[];
  checklist: string[];
  escalation: string;
  keywords: string[];
  examplePrompts: string[];
}

export interface AskResponse {
  type: 'topic' | 'guardrail' | 'clarify';
  topic?: Topic;
  message?: string;
}
