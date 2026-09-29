import React, { useState, useEffect, useCallback } from 'react';
import { Header } from './components/Header';
import { BalanceSummary } from './components/BalanceSummary';
import { TransactionModal } from './components/TransactionModal';
import { TransactionFilterComponent } from './components/TransactionFilter';
import { TransactionTable } from './components/TransactionTable';
import { ToastNotification } from './components/ToastNotification';
import { financialApi } from './services/api';
import { BalanceResponse, CreateTransactionRequest, NotificationState, Transaction, TransactionFilter } from './types';

export const App: React.FC = () => {
  const [balanceData, setBalanceData] = useState<BalanceResponse | null>(null);
  const [balanceLoading, setBalanceLoading] = useState<boolean>(true);

  const [transactions, setTransactions] = useState<Transaction[]>([]);
  const [transactionsLoading, setTransactionsLoading] = useState<boolean>(true);
  const [totalItems, setTotalItems] = useState<number>(0);
  const [totalPages, setTotalPages] = useState<number>(1);

  const [filters, setFilters] = useState<TransactionFilter>({
    startDate: undefined,
    endDate: undefined,
    type: undefined,
    page: 1,
    pageSize: 10,
  });

  const [modalOpen, setModalOpen] = useState<boolean>(false);
  const [modalType, setModalType] = useState<'deposit' | 'withdraw'>('deposit');
  const [submitting, setSubmitting] = useState<boolean>(false);

  const [notification, setNotification] = useState<NotificationState>({
    open: false,
    message: '',
    type: 'success',
  });

  // Carregar Saldo
  const fetchBalance = useCallback(async () => {
    try {
      setBalanceLoading(true);
      const data = await financialApi.getBalance();
      setBalanceData(data);
    } catch (err: any) {
      console.error('Erro ao buscar saldo:', err);
      setNotification({
        open: true,
        message: 'Não foi possível conectar à API de saldo.',
        type: 'error',
      });
    } finally {
      setBalanceLoading(false);
    }
  }, []);

  // Carregar Transações
  const fetchTransactions = useCallback(async () => {
    try {
      setTransactionsLoading(true);
      const data = await financialApi.getTransactions(filters);
      setTransactions(data.items || []);
      setTotalItems(data.totalItems || 0);
      setTotalPages(data.totalPages || 1);
    } catch (err: any) {
      console.error('Erro ao buscar transações:', err);
    } finally {
      setTransactionsLoading(false);
    }
  }, [filters]);

  useEffect(() => {
    fetchBalance();
  }, [fetchBalance]);

  useEffect(() => {
    fetchTransactions();
  }, [fetchTransactions]);

  const handleOpenModal = (type: 'deposit' | 'withdraw') => {
    setModalType(type);
    setModalOpen(true);
  };

  const handleTransactionSubmit = async (data: CreateTransactionRequest) => {
    setSubmitting(true);
    try {
      if (modalType === 'deposit') {
        await financialApi.deposit(data);
        setNotification({
          open: true,
          message: `Entrada de R$ ${data.amount.toFixed(2)} confirmada!`,
          type: 'success',
        });
      } else {
        await financialApi.withdraw(data);
        setNotification({
          open: true,
          message: `Saída de R$ ${data.amount.toFixed(2)} debitada com sucesso!`,
          type: 'success',
        });
      }

      await fetchBalance();
      await fetchTransactions();
    } finally {
      setSubmitting(false);
    }
  };

  const handleFilterChange = (key: keyof TransactionFilter, value: any) => {
    setFilters((prev) => ({
      ...prev,
      [key]: value,
      page: 1,
    }));
  };

  const handleResetFilters = () => {
    setFilters({
      startDate: undefined,
      endDate: undefined,
      type: undefined,
      page: 1,
      pageSize: 10,
    });
  };

  const handlePageChange = (newPage: number) => {
    setFilters((prev) => ({
      ...prev,
      page: newPage,
    }));
  };

  const handlePageSizeChange = (newPageSize: number) => {
    setFilters((prev) => ({
      ...prev,
      pageSize: newPageSize,
      page: 1,
    }));
  };

  return (
    <div className="min-h-screen bg-slate-50 flex flex-col">
      <Header />

      <main className="flex-1 max-w-7xl w-full mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <BalanceSummary
          balanceData={balanceData}
          loading={balanceLoading}
          onOpenModal={handleOpenModal}
        />

        <TransactionFilterComponent
          filters={filters}
          onFilterChange={handleFilterChange}
          onReset={handleResetFilters}
        />

        <TransactionTable
          transactions={transactions}
          totalItems={totalItems}
          totalPages={totalPages}
          page={filters.page || 1}
          pageSize={filters.pageSize || 10}
          loading={transactionsLoading}
          onPageChange={handlePageChange}
          onPageSizeChange={handlePageSizeChange}
        />

        <TransactionModal
          isOpen={modalOpen}
          type={modalType}
          onClose={() => setModalOpen(false)}
          onSubmit={handleTransactionSubmit}
          loading={submitting}
        />

        <ToastNotification
          notification={notification}
          onClose={() => setNotification((prev) => ({ ...prev, open: false }))}
        />
      </main>
    </div>
  );
};

export default App;
