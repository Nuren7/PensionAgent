import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  AdvisorResponse,
  ConversationMessage,
  PensionSummary,
  SimulationRequest,
  SimulationResult,
} from './api.models';

const demoUserId = 'user-seb-001';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = this.resolveApiBaseUrl(environment.apiUrl);

  getPortfolio(userId = demoUserId): Observable<PensionSummary> {
    return this.http.get<PensionSummary>(
      `${this.apiBaseUrl}/portfolio/${encodeURIComponent(userId)}`,
    );
  }

  simulate(request: SimulationRequest): Observable<SimulationResult> {
    return this.http.post<SimulationResult>(`${this.apiBaseUrl}/simulate`, request);
  }

  chat(
    userId: string,
    message: string,
    conversationHistory: ConversationMessage[],
  ): Observable<AdvisorResponse> {
    return this.http.post<AdvisorResponse>(`${this.apiBaseUrl}/advisor/chat`, {
      userId,
      message,
      conversationHistory,
    });
  }

  private resolveApiBaseUrl(apiUrl: string): string {
    const normalizedUrl = apiUrl.replace(/\/+$/, '');
    return normalizedUrl.endsWith('/api') ? normalizedUrl : `${normalizedUrl}/api`;
  }
}
