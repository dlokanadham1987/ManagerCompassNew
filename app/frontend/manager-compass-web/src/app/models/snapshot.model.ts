export interface SnapshotKpi {
  label: string;
  value: string;
  subText: string;
  color: string;
}

export interface AttritionReason {
  label: string;
  percent: number;
  color: string;
}

export interface RequisitionStatusRow {
  status: string;
  count: number;
  color: string;
}

export interface Snapshot {
  scopeLabel: string;
  sourceNote: string;
  loadedAtUtc: string;
  kpis: SnapshotKpi[];
  levelCounts: number[];
  levelLabels: string[];
  attritionByReason: AttritionReason[];
  attritionByType: AttritionReason[];
  earlyTenureExitPct: number;
  headcountByRegion: AttritionReason[];
  requisitionStatus: RequisitionStatusRow[];
  engagementTrend: number[];
}
