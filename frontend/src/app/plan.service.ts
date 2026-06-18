import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

import { ApiResponse, PlanRequest, PlanResult } from './models';

// MVP: backend runs locally. Move to an environment file before any deploy.
const API_BASE = 'http://localhost:5130';

@Injectable({ providedIn: 'root' })
export class PlanService {
  private readonly http = inject(HttpClient);

  async rank(request: PlanRequest): Promise<PlanResult> {
    const response = await firstValueFrom(
      this.http.post<ApiResponse<PlanResult>>(`${API_BASE}/api/plan`, request)
    );

    if (!response.success || !response.data) {
      throw new Error(response.error ?? 'Planning failed.');
    }

    return response.data;
  }
}
