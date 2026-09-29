import React from 'react';
import { ArrowUpRight, ArrowDownRight, FileText, ChevronLeft, ChevronRight } from 'lucide-react';
import { Transaction } from '../types';

interface TransactionTableProps {
  transactions: Transaction[];
  totalItems: number;
  totalPages: number;
  page: number;
  pageSize: number;
  loading: boolean;
  onPageChange: (newPage: number) => void;
  onPageSizeChange: (newPageSize: number) => void;
}

export const TransactionTable: React.FC<TransactionTableProps> = ({
  transactions,
  totalItems,
  totalPages,
  page,
  pageSize,
  loading,
  onPageChange,
  onPageSizeChange,
}) => {
  const formatCurrency = (value: number) => {
    return new Intl.NumberFormat('pt-BR', {
      style: 'currency',
      currency: 'BRL',
    }).format(value);
  };

  const formatDate = (dateStr: string) => {
    try {
      const d = new Date(dateStr);
      return new Intl.DateTimeFormat('pt-BR', {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
        second: '2-digit',
      }).format(d);
    } catch {
      return dateStr;
    }
  };

  return (
    <div className="bg-white rounded-2xl border border-slate-200/80 shadow-sm overflow-hidden">
      <div className="p-5 border-b border-slate-100 flex items-center justify-between">
        <div className="flex items-center space-x-2">
          <FileText className="w-5 h-5 text-slate-700" />
          <h2 className="text-base font-bold text-slate-900">Extrato de Movimentações</h2>
        </div>
        <span className="text-xs font-semibold px-2.5 py-1 bg-slate-100 text-slate-600 rounded-full">
          {totalItems} movimentação(ões)
        </span>
      </div>

      <div className="overflow-x-auto">
        <table className="w-full text-left border-collapse">
          <thead>
            <tr className="bg-slate-50/75 border-b border-slate-100 text-[11px] font-bold uppercase tracking-wider text-slate-500">
              <th className="py-3.5 px-5">Data / Hora</th>
              <th className="py-3.5 px-5">Tipo</th>
              <th className="py-3.5 px-5">Descrição</th>
              <th className="py-3.5 px-5 text-right">Valor</th>
              <th className="py-3.5 px-5 text-right">Saldo Resultante</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100 text-sm">
            {loading ? (
              Array.from({ length: 4 }).map((_, index) => (
                <tr key={index} className="animate-pulse">
                  <td className="py-4 px-5"><div className="h-4 bg-slate-100 rounded w-32"></div></td>
                  <td className="py-4 px-5"><div className="h-6 bg-slate-100 rounded-full w-20"></div></td>
                  <td className="py-4 px-5"><div className="h-4 bg-slate-100 rounded w-48"></div></td>
                  <td className="py-4 px-5 text-right"><div className="h-4 bg-slate-100 rounded w-24 ml-auto"></div></td>
                  <td className="py-4 px-5 text-right"><div className="h-4 bg-slate-100 rounded w-24 ml-auto"></div></td>
                </tr>
              ))
            ) : transactions.length > 0 ? (
              transactions.map((tx) => {
                const isCredit = tx.type === 'Entrada' || tx.typeValue === 1;
                return (
                  <tr key={tx.id} className="hover:bg-slate-50/80 transition-colors">
                    <td className="py-4 px-5 text-slate-500 font-medium text-xs whitespace-nowrap">
                      {formatDate(tx.createdAt)}
                    </td>
                    <td className="py-4 px-5 whitespace-nowrap">
                      <span
                        className={`inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-bold ${
                          isCredit
                            ? 'bg-emerald-50 text-emerald-700 border border-emerald-200/60'
                            : 'bg-rose-50 text-rose-700 border border-rose-200/60'
                        }`}
                      >
                        {isCredit ? (
                          <ArrowUpRight className="w-3.5 h-3.5 text-emerald-600" />
                        ) : (
                          <ArrowDownRight className="w-3.5 h-3.5 text-rose-600" />
                        )}
                        {tx.type}
                      </span>
                    </td>
                    <td className="py-4 px-5 text-slate-800 font-medium max-w-xs truncate">
                      {tx.description || '-'}
                    </td>
                    <td
                      className={`py-4 px-5 text-right font-bold whitespace-nowrap ${
                        isCredit ? 'text-emerald-600' : 'text-rose-600'
                      }`}
                    >
                      {isCredit ? '+ ' : '- '}
                      {formatCurrency(tx.amount)}
                    </td>
                    <td className="py-4 px-5 text-right font-semibold text-slate-700 whitespace-nowrap">
                      {formatCurrency(tx.balanceAfter)}
                    </td>
                  </tr>
                );
              })
            ) : (
              <tr>
                <td colSpan={5} className="py-12 text-center text-slate-400 font-medium text-sm">
                  Nenhuma movimentação encontrada.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {/* Paginação */}
      <div className="p-4 border-t border-slate-100 flex flex-wrap items-center justify-between gap-4 text-xs text-slate-600">
        <div className="flex items-center space-x-2">
          <span>Itens por página:</span>
          <select
            value={pageSize}
            onChange={(e) => onPageSizeChange(Number(e.target.value))}
            className="px-2 py-1 bg-slate-50 border border-slate-200 rounded-md text-xs font-medium focus:outline-none focus:ring-2 focus:ring-sky-500"
          >
            <option value={5}>5</option>
            <option value={10}>10</option>
            <option value={20}>20</option>
            <option value={50}>50</option>
          </select>
        </div>

        <div className="flex items-center space-x-3">
          <span>
            Página <strong className="text-slate-800">{page}</strong> de{' '}
            <strong className="text-slate-800">{totalPages || 1}</strong>
          </span>

          <div className="inline-flex rounded-lg border border-slate-200 overflow-hidden">
            <button
              type="button"
              onClick={() => onPageChange(page - 1)}
              disabled={page <= 1 || loading}
              className="p-1.5 bg-white hover:bg-slate-50 text-slate-600 disabled:opacity-40 disabled:hover:bg-white transition-colors cursor-pointer"
            >
              <ChevronLeft className="w-4 h-4" />
            </button>
            <div className="w-[1px] bg-slate-200"></div>
            <button
              type="button"
              onClick={() => onPageChange(page + 1)}
              disabled={page >= totalPages || loading}
              className="p-1.5 bg-white hover:bg-slate-50 text-slate-600 disabled:opacity-40 disabled:hover:bg-white transition-colors cursor-pointer"
            >
              <ChevronRight className="w-4 h-4" />
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};
