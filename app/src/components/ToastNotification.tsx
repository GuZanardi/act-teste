import React, { useEffect } from 'react';
import { CheckCircle, AlertCircle, X } from 'lucide-react';
import { NotificationState } from '../types';

interface ToastNotificationProps {
  notification: NotificationState;
  onClose: () => void;
}

export const ToastNotification: React.FC<ToastNotificationProps> = ({
  notification,
  onClose,
}) => {
  useEffect(() => {
    if (notification.open) {
      const timer = setTimeout(() => {
        onClose();
      }, 5000);
      return () => clearTimeout(timer);
    }
  }, [notification.open, onClose]);

  if (!notification.open) return null;

  const isSuccess = notification.type === 'success';

  return (
    <div className="fixed bottom-5 right-5 z-50 animate-in slide-in-from-bottom-5 duration-300">
      <div
        className={`flex items-center gap-3 px-4 py-3 rounded-2xl shadow-xl border text-sm font-medium ${
          isSuccess
            ? 'bg-emerald-900/95 text-emerald-100 border-emerald-700/60 shadow-emerald-950/20'
            : 'bg-rose-900/95 text-rose-100 border-rose-700/60 shadow-rose-950/20'
        } backdrop-blur-md`}
      >
        {isSuccess ? (
          <CheckCircle className="w-5 h-5 text-emerald-400 flex-shrink-0" />
        ) : (
          <AlertCircle className="w-5 h-5 text-rose-400 flex-shrink-0" />
        )}
        <span className="max-w-xs">{notification.message}</span>
        <button
          type="button"
          onClick={onClose}
          className="ml-2 p-1 text-white/70 hover:text-white rounded-lg transition-colors cursor-pointer"
        >
          <X className="w-4 h-4" />
        </button>
      </div>
    </div>
  );
};
