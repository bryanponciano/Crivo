import * as React from "react"
import { createPortal } from "react-dom"
import { cn } from "@/lib/utils"

export type ToastProps = {
  id: string;
  title?: string;
  description?: string;
  variant?: "default" | "destructive" | "success";
}

let toastCount = 0;
let subscribers: ((toasts: ToastProps[]) => void)[] = [];
let toasts: ToastProps[] = [];

export const toast = (props: Omit<ToastProps, "id">) => {
  const id = (++toastCount).toString();
  const newToast = { ...props, id };
  toasts = [...toasts, newToast];
  subscribers.forEach(sub => sub(toasts));
  
  setTimeout(() => {
    toasts = toasts.filter(t => t.id !== id);
    subscribers.forEach(sub => sub(toasts));
  }, 3000);
}

export function Toaster() {
  const [currentToasts, setCurrentToasts] = React.useState<ToastProps[]>([]);
  
  React.useEffect(() => {
    subscribers.push(setCurrentToasts);
    return () => {
      subscribers = subscribers.filter(s => s !== setCurrentToasts);
    }
  }, []);
  
  if (typeof document === 'undefined') return null;
  
  return createPortal(
    <div className="fixed top-4 right-4 z-[100] flex flex-col gap-2">
      {currentToasts.map(t => (
        <div
          key={t.id}
          className={cn(
            "pointer-events-auto flex w-full max-w-sm flex-col gap-1 rounded-lg border p-4 shadow-lg transition-all",
            t.variant === "destructive" ? "border-destructive bg-destructive text-destructive-foreground" :
            t.variant === "success" ? "border-green-500 bg-green-500 text-white" :
            "border bg-background text-foreground"
          )}
        >
          {t.title && <div className="text-sm font-semibold">{t.title}</div>}
          {t.description && <div className="text-sm opacity-90">{t.description}</div>}
        </div>
      ))}
    </div>,
    document.body
  );
}
