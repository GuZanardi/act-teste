import axios from "axios";
import {
  BalanceResponse,
  CreateTransactionRequest,
  PagedResult,
  Transaction,
  TransactionFilter,
} from "../types";

const API_BASE_URL = process.env.REACT_APP_API_URL || "http://localhost:5001";

const client = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    "Content-Type": "application/json",
  },
});

export const financialApi = {
  // Consultar o saldo disponível
  getBalance: async (accountId?: string): Promise<BalanceResponse> => {
    const params = accountId ? { accountId } : {};
    const response = await client.get<BalanceResponse>("/api/account/balance", {
      params,
    });
    return response.data;
  },

  // Consultar o extrato / histórico de transações
  getTransactions: async (
    filter: TransactionFilter = {},
  ): Promise<PagedResult<Transaction>> => {
    const response = await client.get<PagedResult<Transaction>>(
      "/api/account/transactions",
      { params: filter },
    );
    return response.data;
  },

  // Registrar entrada (Crédito)
  deposit: async (
    data: CreateTransactionRequest,
    accountId?: string,
  ): Promise<Transaction> => {
    const params = accountId ? { accountId } : {};
    const response = await client.post<Transaction>(
      "/api/transactions/deposit",
      data,
      { params },
    );
    return response.data;
  },

  // Registrar saída (Débito)
  withdraw: async (
    data: CreateTransactionRequest,
    accountId?: string,
  ): Promise<Transaction> => {
    const params = accountId ? { accountId } : {};
    const response = await client.post<Transaction>(
      "/api/transactions/withdraw",
      data,
      { params },
    );
    return response.data;
  },
};

export default client;
