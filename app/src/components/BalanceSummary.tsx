import React from 'react';
import { Landmark, ArrowUpRight, ArrowDownRight, PlusCircle, MinusCircle } from 'lucide-react';
import { BalanceResponse } from '../types';

interface BalanceSummaryProps {
  balanceData: BalanceResponse | null;
  loading: boolean;
  onOpenModal: (type: 'deposit' | 'withdraw') => void;
}

export const BalanceSummary: React.FC<BalanceSummaryProps> = ({
  balanceData,
  loading,
  onOpenModal,
}) => {
  const formatCurrency = (value: number | undefined) => {
    return new Intl.NumberFormat('pt-BR', {
      style: 'currency',
      currency: 'BRL',
    }).format(value ?? 0);
  };

  return (
    <div className="space-y-6 mb-8">
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        {/* Card Saldo Disponível */}
        <div className="bg-gradient-to-br from-slate-900 to-slate-800 text-white rounded-2xl p-6 shadow-xl border border-slate-700/50 flex flex-col justify-between relative overflow-hidden">
          <div className="flex items-center justify-between mb-4">
            <span className="text-xs font-bold uppercase tracking-wider text-slate-400">
              Saldo Disponível
            </span>
            <div className="p-2.5 bg-sky-500/20 text-sky-400 rounded-xl">
              <Landmark className="w-6 h-6" />
            </div>
          </div>
          <div>
            {loading ? (
              <div className="h-10 bg-slate-700/60 rounded-lg animate-pulse w-3/4 mb-2"></div>
            ) : (
              <div className="text-3xl sm:text-4xl font-extrabold text-white tracking-tight">
                {formatCurrency(balanceData?.balance)}
              </div>
            )}
            <p className="text-xs text-slate-400 mt-2 font-medium">
              {balanceData?.accountName || 'Conta Empresarial Principal'}
            </p>
          </div>
        </div>

        {/* Card Total Entradas */}
        <div className="bg-white rounded-2xl p-6 shadow-sm border border-slate-200/80 flex flex-col justify-between">
          <div className="flex items-center justify-between mb-4">
            <span className="text-xs font-bold uppercase tracking-wider text-slate-500">
              Total de Entradas
            </span>
            <div className="p-2.5 bg-emerald-50 text-emerald-600 rounded-xl">
              <ArrowUpRight className="w-6 h-6" />
            </div>
          </div>
          <div>
            {loading ? (
              <div className="h-9 bg-slate-100 rounded-lg animate-pulse w-2/3 mb-2"></div>
            ) : (
              <div className="text-2xl sm:text-3xl font-bold text-emerald-600 tracking-tight">
                {formatCurrency(balanceData?.totalCredits)}
              </div>
            )}
            <p className="text-xs text-slate-400 mt-2">Créditos e aportes acumulados</p>
          </div>
        </div>

        {/* Card Total Saídas */}
        <div className="bg-white rounded-2xl p-6 shadow-sm border border-slate-200/80 flex flex-col justify-between">
          <div className="flex items-center justify-between mb-4">
            <span className="text-xs font-bold uppercase tracking-wider text-slate-500">
              Total de Saídas
            </span>
            <div className="p-2.5 bg-rose-50 text-rose-600 rounded-xl">
              <ArrowDownRight className="w-6 h-6" />
            </div>
          </div>
          <div>
            {loading ? (
              <div className="h-9 bg-slate-100 rounded-lg animate-pulse w-2/3 mb-2"></div>
            ) : (
              <div className="text-2xl sm:text-3xl font-bold text-rose-600 tracking-tight">
                {formatCurrency(balanceData?.totalDebits)}
              </div>
            )}
            <p className="text-xs text-slate-400 mt-2">Débitos e pagamentos realizados</p>
          </div>
        </div>
      </div>

      {/* Botões de Ação */}
      <div className="flex flex-wrap gap-4 items-center">
        <button
          type="button"
          onClick={() => onOpenModal('deposit')}
          className="inline-flex items-center gap-2 px-5 py-3 rounded-xl bg-emerald-600 hover:bg-emerald-700 text-white font-semibold text-sm shadow-md shadow-emerald-600/20 transition-all duration-200 focus:outline-none focus:ring-2 focus:ring-emerald-500 focus:ring-offset-2 cursor-pointer"
        >
          <PlusCircle className="w-5 h-5" />
          Registrar Entrada
        </button>

        <button
          type="button"
          onClick={() => onOpenModal('withdraw')}
          className="inline-flex items-center gap-2 px-5 py-3 rounded-xl bg-rose-600 hover:bg-rose-700 text-white font-semibold text-sm shadow-md shadow-rose-600/20 transition-all duration-200 focus:outline-none focus:ring-2 focus:ring-rose-500 focus:ring-offset-2 cursor-pointer"
        >
          <MinusCircle className="w-5 h-5" />
          Registrar Saída
        </button>
      </div>
    </div>
  );
};
