export interface TopicUsage {
  key: string;
  label: string;
  color: string;
  count: number;
}

export interface TopicAdoptionStat {
  key: string;
  label: string;
  color: string;
  count: number;
  escalationRatePct: number | null;
  checklistCompletionRatePct: number | null;
  medianSeconds: number | null;
}

export interface AdoptionMetrics {
  activeManagers: number;
  eligibleManagers: number;
  questionsAnsweredThisWeek: number;
  checklistCompletionRate: number;
  escalationsRoutedThisWeek: number;
  medianSecondsPerQuestion: number;
  weeklyActiveTrend: number[];
  topTopics: TopicUsage[];
  topicBreakdown: TopicAdoptionStat[];
  productionMeasurementPlan: string[];
}
