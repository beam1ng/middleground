// Shared API models — mirror the .NET backend DTOs (camelCase JSON).

export type Objective = 'Minimax' | 'Sum' | 'Spread' | 'Weighted';

export interface MemberPin {
  id: string;
  label?: string;
  lat: number;
  lng: number;
  weight: number;
}

export interface Candidate {
  id: string;
  name: string;
  lat: number;
  lng: number;
}

export interface PlanRequest {
  members: MemberPin[];
  candidates: Candidate[];
  objective: Objective;
}

export interface MemberTravel {
  memberId: string;
  label?: string;
  durationSeconds: number | null;
}

export interface DestinationScore {
  candidate: Candidate;
  score: number;
  allReachable: boolean;
  maxSeconds: number | null;
  sumSeconds: number | null;
  spreadSeconds: number | null;
  memberTravels: MemberTravel[];
}

export interface PlanResult {
  objective: Objective;
  ranked: DestinationScore[];
}

export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  error?: string;
}
