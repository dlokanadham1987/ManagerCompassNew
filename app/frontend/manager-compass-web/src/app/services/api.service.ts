import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Topic, AskResponse } from '../models/topic.model';
import { Snapshot } from '../models/snapshot.model';
import { LeadershipCard } from '../models/leadership.model';
import { AdoptionMetrics } from '../models/adoption.model';
import { DatasetInfo, PagedColumnResult, IdentifierGridResult } from '../models/data-explorer.model';

/**
 * Single seam between the Angular app and its data source. Talks to the .NET API's
 * MockGuidanceDataService today; nothing here changes when that service is swapped
 * for a real SharePoint/Power BI-backed implementation.
 */
@Injectable({ providedIn: 'root' })
export class ApiService {
  private base = '/api';

  constructor(private http: HttpClient) {}

  getTopics(): Observable<Topic[]> {
    return this.http.get<Topic[]>(`${this.base}/topics`);
  }

  getTopic(key: string): Observable<Topic> {
    return this.http.get<Topic>(`${this.base}/topics/${key}`);
  }

  getSnapshot(): Observable<Snapshot> {
    return this.http.get<Snapshot>(`${this.base}/snapshot`);
  }

  getLeadershipCard(): Observable<LeadershipCard> {
    return this.http.get<LeadershipCard>(`${this.base}/leadership`);
  }

  getAdoptionMetrics(): Observable<AdoptionMetrics> {
    return this.http.get<AdoptionMetrics>(`${this.base}/adoption`);
  }

  getDatasets(): Observable<DatasetInfo[]> {
    return this.http.get<DatasetInfo[]>(`${this.base}/dataexplorer/datasets`);
  }

  getDatasetColumns(dataset: string): Observable<string[]> {
    return this.http.get<string[]>(`${this.base}/dataexplorer/${dataset}/columns`);
  }

  getAttributeColumns(dataset: string): Observable<string[]> {
    return this.http.get<string[]>(`${this.base}/dataexplorer/${dataset}/attribute-columns`);
  }

  getIdentifierGrid(dataset: string, page: number, pageSize = 25, columnFilters: Record<string, string> = {}): Observable<IdentifierGridResult> {
    const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
    for (const [col, value] of Object.entries(columnFilters)) {
      if (value.trim()) params.set(col, value.trim());
    }
    return this.http.get<IdentifierGridResult>(`${this.base}/dataexplorer/${dataset}/identifiers?${params.toString()}`);
  }

  getColumnPage(dataset: string, column: string, page: number, pageSize = 25, search = ''): Observable<PagedColumnResult> {
    let params = `page=${page}&pageSize=${pageSize}`;
    if (search.trim()) params += `&search=${encodeURIComponent(search.trim())}`;
    return this.http.get<PagedColumnResult>(`${this.base}/dataexplorer/${dataset}/${encodeURIComponent(column)}?${params}`);
  }

  ask(text: string, topicKey?: string): Observable<AskResponse> {
    return this.http.post<AskResponse>(`${this.base}/assistant/ask`, { text, topicKey });
  }
}
