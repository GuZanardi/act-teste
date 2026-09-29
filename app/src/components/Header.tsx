import React from 'react';
import { Wallet, CheckCircle2 } from 'lucide-react';

export const Header: React.FC = () => {
  return (
    <header className="bg-slate-900 border-b border-slate-800 text-white sticky top-0 z-30 shadow-md">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 h-16 flex items-center justify-between">
        <div className="flex items-center space-x-3">
          <div className="bg-sky-500/10 p-2 rounded-xl border border-sky-500/20 text-sky-400">
            <Wallet className="w-6 h-6" />
          </div>
          <div>
            <div className="flex items-center space-x-1.5 font-bold text-lg tracking-tight">
              <span>ACT Digital</span>
              <span className="text-sky-400">Financial</span>
            </div>
            <p className="text-xs text-slate-400 font-medium">Controle de Conta Empresarial</p>
          </div>
        </div>

        <div className="flex items-center space-x-2">
          <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold bg-emerald-500/10 text-emerald-400 border border-emerald-500/20">
            <CheckCircle2 className="w-3.5 h-3.5" />
            API Conectada (.NET 9)
          </span>
        </div>
      </div>
    </header>
  );
};
