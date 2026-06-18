import {
  AfterViewInit,
  Component,
  ElementRef,
  OnDestroy,
  computed,
  inject,
  signal,
  viewChild
} from '@angular/core';
import * as maplibregl from 'maplibre-gl';

import { Candidate, DestinationScore, MemberPin, Objective, PlanResult } from './models';
import { PlanService } from './plan.service';

type Mode = 'member' | 'candidate';

const POLAND_CENTER: [number, number] = [19.2, 52.0];
const MAP_STYLE = 'https://demotiles.maplibre.org/style.json';

@Component({
  selector: 'app-root',
  imports: [],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements AfterViewInit, OnDestroy {
  private readonly planService = inject(PlanService);
  private readonly mapContainer = viewChild.required<ElementRef<HTMLDivElement>>('map');

  private map?: maplibregl.Map;
  private readonly memberMarkers = new Map<string, maplibregl.Marker>();
  private readonly candidateMarkers = new Map<string, maplibregl.Marker>();

  protected readonly mode = signal<Mode>('member');
  protected readonly members = signal<MemberPin[]>([]);
  protected readonly candidates = signal<Candidate[]>([]);
  protected readonly objective = signal<Objective>('Minimax');
  protected readonly result = signal<PlanResult | null>(null);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly objectives: readonly Objective[] = ['Minimax', 'Sum', 'Spread', 'Weighted'];

  protected readonly bestId = computed(() => this.result()?.ranked[0]?.candidate.id ?? null);
  protected readonly canPlan = computed(
    () => this.members().length > 0 && this.candidates().length > 0 && !this.loading()
  );

  ngAfterViewInit(): void {
    this.map = new maplibregl.Map({
      container: this.mapContainer().nativeElement,
      style: MAP_STYLE,
      center: POLAND_CENTER,
      zoom: 5.3
    });

    this.map.addControl(new maplibregl.NavigationControl(), 'top-right');
    this.map.on('click', (e) => this.onMapClick(e.lngLat.lng, e.lngLat.lat));
  }

  ngOnDestroy(): void {
    this.map?.remove();
  }

  protected setMode(mode: Mode): void {
    this.mode.set(mode);
  }

  protected setObjective(value: string): void {
    this.objective.set(value as Objective);
    this.result.set(null);
  }

  private onMapClick(lng: number, lat: number): void {
    if (this.mode() === 'member') {
      this.addMember(lng, lat);
    } else {
      this.addCandidate(lng, lat);
    }
    this.result.set(null);
  }

  private addMember(lng: number, lat: number): void {
    const id = crypto.randomUUID();
    const label = `Member ${this.members().length + 1}`;
    const member: MemberPin = { id, label, lat, lng, weight: 1 };
    this.members.update((list) => [...list, member]);

    const marker = new maplibregl.Marker({ color: '#3b82f6' })
      .setLngLat([lng, lat])
      .addTo(this.map!);
    this.memberMarkers.set(id, marker);
  }

  private addCandidate(lng: number, lat: number): void {
    const id = crypto.randomUUID();
    const name = `Candidate ${this.candidates().length + 1}`;
    const candidate: Candidate = { id, name, lat, lng };
    this.candidates.update((list) => [...list, candidate]);
    this.renderCandidateMarkers();
  }

  protected removeMember(id: string): void {
    this.memberMarkers.get(id)?.remove();
    this.memberMarkers.delete(id);
    this.members.update((list) => list.filter((m) => m.id !== id));
    this.result.set(null);
  }

  protected removeCandidate(id: string): void {
    this.candidates.update((list) => list.filter((c) => c.id !== id));
    this.result.set(null);
    this.renderCandidateMarkers();
  }

  protected clearAll(): void {
    this.memberMarkers.forEach((m) => m.remove());
    this.candidateMarkers.forEach((m) => m.remove());
    this.memberMarkers.clear();
    this.candidateMarkers.clear();
    this.members.set([]);
    this.candidates.set([]);
    this.result.set(null);
    this.error.set(null);
  }

  private renderCandidateMarkers(): void {
    this.candidateMarkers.forEach((m) => m.remove());
    this.candidateMarkers.clear();
    const best = this.bestId();
    for (const c of this.candidates()) {
      const color = c.id === best ? '#f5b301' : '#94a3b8';
      const marker = new maplibregl.Marker({ color })
        .setLngLat([c.lng, c.lat])
        .addTo(this.map!);
      this.candidateMarkers.set(c.id, marker);
    }
  }

  protected async plan(): Promise<void> {
    if (!this.canPlan()) {
      return;
    }

    this.loading.set(true);
    this.error.set(null);
    try {
      const result = await this.planService.rank({
        members: this.members(),
        candidates: this.candidates(),
        objective: this.objective()
      });
      this.result.set(result);
      this.renderCandidateMarkers();
    } catch (err: unknown) {
      this.error.set(err instanceof Error ? err.message : 'Planning failed.');
    } finally {
      this.loading.set(false);
    }
  }

  protected minutes(seconds: number | null): string {
    if (seconds === null) {
      return '—';
    }
    return `${Math.round(seconds / 60)} min`;
  }

  protected scoreLabel(score: DestinationScore): string {
    switch (this.objective()) {
      case 'Minimax':
        return `worst-case ${this.minutes(score.maxSeconds)}`;
      case 'Sum':
        return `total ${this.minutes(score.sumSeconds)}`;
      case 'Spread':
        return `spread ${this.minutes(score.spreadSeconds)}`;
      case 'Weighted':
        return `weighted ${Math.round(score.score / 60)} min`;
    }
  }
}
