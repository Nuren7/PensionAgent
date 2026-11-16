import { DecimalPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { EMPTY, Subject, catchError, debounceTime, distinctUntilChanged, switchMap } from 'rxjs';
import { ApiService } from './core/api.service';
import {
  AdvisorMessage,
  ConversationMessage,
  PensionAccount,
  PensionSummary,
  SimulationRequest,
  SimulationResult,
} from './core/api.models';

const demoUserId = 'user-seb-001';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [DecimalPipe, FormsModule],
  templateUrl: './app.html',
})
export class App implements OnInit {
  private readonly api = inject(ApiService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly simulationRequests = new Subject<SimulationRequest>();
  private conversationHistory: ConversationMessage[] = [];

  readonly userId = signal(demoUserId);
  readonly portfolio = signal<PensionSummary | null>(null);
  readonly projection = signal<SimulationResult | null>(null);
  readonly isLoading = signal(true);
  readonly isSimulating = signal(false);
  readonly isChatBusy = signal(false);
  readonly portfolioError = signal('');
  readonly simulationError = signal('');
  readonly chatError = signal('');
  readonly retirementAge = signal(65);
  readonly extraMonthlySaving = signal(0);
  readonly question = signal('');
  readonly messages = signal<AdvisorMessage[]>([]);
  readonly expandedTraces = signal<Set<number>>(new Set());

  readonly publicCapital = computed(() =>
    this.accountsOfType('State').reduce((total, account) => total + account.balance, 0),
  );
  readonly occupationalCapital = computed(() =>
    this.accountsOfType('Occupational').reduce((total, account) => total + account.balance, 0),
  );
  readonly missingSicknessInsurance = computed(
    () => !this.hasActivePolicy(['sickness', 'sjukförsäkring']),
  );
  readonly missingSurvivorProtection = computed(
    () => !this.hasActivePolicy(['survivor', 'survivorprotection', 'efterlevandeskydd']),
  );
  readonly coverageGaps = computed(() => {
    const gaps: string[] = [];
    if (this.missingSicknessInsurance()) {
      gaps.push('Sickness insurance');
    }
    if (this.missingSurvivorProtection()) {
      gaps.push('Survivor protection');
    }
    return gaps;
  });

  ngOnInit(): void {
    this.simulationRequests
      .pipe(
        debounceTime(280),
        distinctUntilChanged(
          (previous, current) =>
            previous.userId === current.userId &&
            previous.retirementAge === current.retirementAge &&
            previous.extraMonthlySaving === current.extraMonthlySaving,
        ),
        switchMap((request) =>
          this.api.simulate(request).pipe(
            catchError((error: unknown) => {
              this.simulationError.set(this.getErrorMessage(error));
              this.isSimulating.set(false);
              return EMPTY;
            }),
          ),
        ),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((result) => {
        this.projection.set(result);
        this.simulationError.set('');
        this.isSimulating.set(false);
      });

    this.loadPortfolio();
  }

  loadPortfolio(): void {
    this.isLoading.set(true);
    this.portfolioError.set('');
    this.api
      .getPortfolio(this.userId())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (portfolio) => {
          this.portfolio.set(portfolio);
          this.userId.set(portfolio.userId);
          this.isLoading.set(false);
          this.requestProjection();
        },
        error: (error: unknown) => {
          this.portfolioError.set(this.getErrorMessage(error));
          this.isLoading.set(false);
        },
      });
  }

  setRetirementAge(event: Event): void {
    const input = event.target;
    if (!(input instanceof HTMLInputElement)) {
      return;
    }

    this.retirementAge.set(Number(input.value));
    this.requestProjection();
  }

  setExtraMonthlySaving(event: Event): void {
    const input = event.target;
    if (!(input instanceof HTMLInputElement)) {
      return;
    }

    this.extraMonthlySaving.set(Number(input.value));
    this.requestProjection();
  }

  useSuggestion(suggestion: string): void {
    this.question.set(suggestion);
  }

  askAdvisor(): void {
    const message = this.question().trim();
    if (!message || this.isChatBusy()) {
      return;
    }

    const history = [...this.conversationHistory];
    this.messages.update((messages) => [...messages, { role: 'user', content: message }]);
    this.question.set('');
    this.chatError.set('');
    this.isChatBusy.set(true);

    this.api
      .chat(this.userId(), message, history)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.conversationHistory = [
            ...history,
            { role: 'user', content: message },
            { role: 'assistant', content: response.reply },
          ];
          this.messages.update((messages) => [
            ...messages,
            {
              role: 'assistant',
              content: response.reply,
              toolTraces: response.toolTraces,
            },
          ]);
          this.isChatBusy.set(false);
        },
        error: (error: unknown) => {
          this.chatError.set(this.getErrorMessage(error));
          this.isChatBusy.set(false);
        },
      });
  }

  toggleTrace(messageIndex: number): void {
    this.expandedTraces.update((expanded) => {
      const next = new Set(expanded);
      if (next.has(messageIndex)) {
        next.delete(messageIndex);
      } else {
        next.add(messageIndex);
      }
      return next;
    });
  }

  formatTraceValue(value: unknown): string {
    return JSON.stringify(value, null, 2);
  }

  private requestProjection(): void {
    this.isSimulating.set(true);
    this.simulationRequests.next({
      userId: this.userId(),
      retirementAge: this.retirementAge(),
      extraMonthlySaving: this.extraMonthlySaving(),
    });
  }

  private accountsOfType(accountType: string): PensionAccount[] {
    return (this.portfolio()?.accounts ?? []).filter(
      (account) => account.accountType.toLowerCase() === accountType.toLowerCase(),
    );
  }

  private hasActivePolicy(policyTypes: string[]): boolean {
    const expectedTypes = policyTypes.map((type) => type.toLowerCase());
    return (this.portfolio()?.insurancePolicies ?? []).some(
      (policy) => policy.isActive && expectedTypes.includes(policy.policyType.toLowerCase()),
    );
  }

  private getErrorMessage(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      if (error.status === 0) {
        return 'The API could not be reached. Check that the backend is running and try again.';
      }

      const serverMessage =
        typeof error.error?.detail === 'string'
          ? error.error.detail
          : typeof error.error?.error === 'string'
            ? error.error.error
            : '';
      return serverMessage || `The request could not be completed (HTTP ${error.status}).`;
    }

    return 'An unexpected error occurred. Please try again.';
  }
}
