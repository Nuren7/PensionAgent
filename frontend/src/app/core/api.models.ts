export interface PensionSummary {
  userId: string;
  fullName: string;
  currentAge: number;
  totalCapital: number;
  monthlyContribution: number;
  accounts: PensionAccount[];
  insurancePolicies: InsurancePolicy[];
  providers: ProviderStatus[];
}

export interface PensionAccount {
  accountType: string;
  providerName: string;
  balance: number;
  monthlyContribution: number;
  updatedAt: string;
}

export interface InsurancePolicy {
  policyType: string;
  providerName: string;
  coverageAmount: number;
  isActive: boolean;
  updatedAt: string;
}

export interface ProviderStatus {
  provider: string;
  status: string;
  message: string;
  durationMs: number;
}

export interface SimulationRequest {
  userId: string;
  retirementAge: number;
  extraMonthlySaving: number;
}

export interface SimulationResult {
  userId: string;
  retirementAge: number;
  currentCapital: number;
  currentMonthlyContribution: number;
  extraMonthlySaving: number;
  expectedAnnualReturn: number;
  yearsToRetirement: number;
  projectedCapital: number;
  estimatedMonthlyPayout: number;
}

export interface ConversationMessage {
  role: 'user' | 'assistant';
  content: string;
}

export interface ToolTrace {
  toolName: string;
  argumentsJson: string;
  arguments?: unknown;
  output: unknown;
}

export interface AdvisorResponse {
  reply: string;
  toolTraces: ToolTrace[];
}

export interface AdvisorMessage {
  role: 'user' | 'assistant';
  content: string;
  toolTraces?: ToolTrace[];
}
