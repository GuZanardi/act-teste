export interface BalanceResponse {
  accountId: string;
  accountName: string;
  balance: number;
  totalCredits: number;
  totalDebits: number;
  totalTransactions: number;
  lastUpdated: string | null;
}

export interface Transaction {
  id: string;
  accountId: string;
  type: string;
  typeValue: number;
  amount: number;
  description: string;
  balanceAfter: number;
  createdAt: string;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

export interface TransactionFilter {
  startDate?: string | null;
  endDate?: string | null;
  type?: string;
  page?: number;
  pageSize?: number;
}

export interface CreateTransactionRequest {
  amount: number;
  description?: string;
}

export interface NotificationState {
  open: boolean;
  message: string;
  type: 'success' | 'error';
}
