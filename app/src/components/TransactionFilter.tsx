import React from 'react';
import { Filter, RotateCcw } from 'lucide-react';
import { TransactionFilter as ITransactionFilter } from '../types';

interface TransactionFilterProps {
  filters: ITransactionFilter;
  onFilterChange: (key: keyof ITransactionFilter, value: any) => void;
  onReset: () => void;
}

export const TransactionFilterComponent: React.FC<TransactionFilterProps> = ({
  filters,
  onFilterChange,
  onReset,
}) => {
  return (
    <div className="bg-white rounded-2xl p-4 sm:p-5 border border-slate-200/80 shadow-sm mb-6">
      <div className="flex flex-wrap items-center gap-4">
        <div className="flex items-center space-x-2 text-slate-700 font-semibold text-sm mr-2">
          <Filter className="w-4 h-4 text-slate-500" />
          <span>Filtros do Extrato</span>
        </div>

        {/* Tipo */}
        <div className="flex flex-col">
          <label htmlFor="filter-type" className="text-[11px] font-semibold uppercase text-slate-400 mb-1">
            Tipo
          </label>
          <select
            id="filter-type"
            value={filters.type || ''}
            onChange={(e) => onFilterChange('type', e.target.value || undefined)}
            className="px-3 py-1.5 bg-slate-50 border border-slate-200 rounded-lg text-xs font-medium text-slate-700 focus:bg-white focus:outline-none focus:ring-2 focus:ring-sky-500"
          >
            <option value="">Todos os Tipos</option>
            <option value="Credit">Entradas (+)</option>
            <option value="Debit">Saídas (-)</option>
          </select>
        </div>

        {/* Data Início */}
        <div className="flex flex-col">
          <label htmlFor="filter-start-date" className="text-[11px] font-semibold uppercase text-slate-400 mb-1">
            Data Início
          </label>
          <input
            id="filter-start-date"
            type="date"
            value={filters.startDate ? filters.startDate.split('T')[0] : ''}
            onChange={(e) =>
              onFilterChange('startDate', e.target.value ? new Date(e.target.value).toISOString() : undefined)
            }
            className="px-3 py-1.5 bg-slate-50 border border-slate-200 rounded-lg text-xs font-medium text-slate-700 focus:bg-white focus:outline-none focus:ring-2 focus:ring-sky-500"
          />
        </div>

        {/* Data Fim */}
        <div className="flex flex-col">
          <label htmlFor="filter-end-date" className="text-[11px] font-semibold uppercase text-slate-400 mb-1">
            Data Fim
          </label>
          <input
            id="filter-end-date"
            type="date"
            value={filters.endDate ? filters.endDate.split('T')[0] : ''}
            onChange={(e) =>
              onFilterChange('endDate', e.target.value ? new Date(e.target.value + 'T23:59:59').toISOString() : undefined)
            }
            className="px-3 py-1.5 bg-slate-50 border border-slate-200 rounded-lg text-xs font-medium text-slate-700 focus:bg-white focus:outline-none focus:ring-2 focus:ring-sky-500"
          />
        </div>

        {/* Botão Limpar */}
        <div className="sm:ml-auto self-end">
          <button
            type="button"
            onClick={onReset}
            className="inline-flex items-center gap-1.5 px-3 py-2 text-xs font-semibold text-slate-600 hover:text-slate-900 bg-slate-100 hover:bg-slate-200 rounded-lg transition-colors cursor-pointer"
          >
            <RotateCcw className="w-3.5 h-3.5" />
            Limpar Filtros
          </button>
        </div>
      </div>
    </div>
  );
};
