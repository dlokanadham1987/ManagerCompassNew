import { SnapshotKpi } from './snapshot.model';

export interface LeadershipItem {
  text: string;
  source?: string;
  color: string;
}

export interface LeadershipCard {
  teamName: string;
  manager: string;
  reviewedWith: string;
  period: string;
  cardNumber: string;
  pulseLabel: string;
  pulseColor: string;
  pulseNote: string;
  stats: SnapshotKpi[];
  risksFlagged: LeadershipItem[];
  actionsTaken: LeadershipItem[];
  nextSteps: LeadershipItem[];
  evidenceNote: string;
  humanReviewNote: string;
}
