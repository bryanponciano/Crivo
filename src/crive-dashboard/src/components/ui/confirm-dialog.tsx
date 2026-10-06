import * as React from "react"
import { createPortal } from "react-dom"
import { AlertTriangle } from "lucide-react"
import { Button } from "./button"

interface ConfirmDialogProps {
  open: boolean
  onConfirm: () => void
  onCancel: () => void
  title?: string
  description?: string
  confirmText?: string
  cancelText?: string
  variant?: "danger" | "warning"
  loading?: boolean
}

export function ConfirmDialog({
  open,
  onConfirm,
  onCancel,
  title = "Confirmar ação",
  description = "Tem certeza que deseja continuar? Esta ação não pode ser desfeita.",
  confirmText = "Excluir",
  cancelText = "Cancelar",
  variant = "danger",
  loading = false,
}: ConfirmDialogProps) {
  if (!open) return null

  return createPortal(
    <div className="fixed inset-0 z-[100] flex items-center justify-center">
      {/* Overlay */}
      <div className="fixed inset-0 bg-black/50 backdrop-blur-sm" onClick={onCancel} />

      {/* Modal */}
      <div className="relative z-[101] w-full max-w-md mx-4 bg-white rounded-xl shadow-2xl border border-gray-200 overflow-hidden animate-in fade-in zoom-in-95 duration-200">
        {/* Header com ícone */}
        <div className="flex flex-col items-center pt-6 px-6">
          <div className={`flex items-center justify-center w-14 h-14 rounded-full mb-4 ${
            variant === "danger" ? "bg-red-100" : "bg-yellow-100"
          }`}>
            <AlertTriangle className={`h-7 w-7 ${
              variant === "danger" ? "text-red-600" : "text-yellow-600"
            }`} />
          </div>
          <h3 className="text-lg font-semibold text-gray-900 text-center">{title}</h3>
          <p className="mt-2 text-sm text-gray-500 text-center leading-relaxed">{description}</p>
        </div>

        {/* Botões */}
        <div className="flex gap-3 p-6 mt-2">
          <Button
            variant="outline"
            className="flex-1 h-11 text-gray-700 border-gray-300 hover:bg-gray-50"
            onClick={onCancel}
            disabled={loading}
          >
            {cancelText}
          </Button>
          <Button
            className={`flex-1 h-11 text-white ${
              variant === "danger"
                ? "bg-red-600 hover:bg-red-700"
                : "bg-yellow-600 hover:bg-yellow-700"
            }`}
            onClick={onConfirm}
            disabled={loading}
          >
            {loading ? "Excluindo..." : confirmText}
          </Button>
        </div>
      </div>
    </div>,
    document.body
  )
}
