import * as React from "react"
import { cn } from "@/lib/utils"

export const DropdownMenu = ({ children }: { children: React.ReactNode }) => {
  return <div className="relative inline-block text-left">{children}</div>
}

export const DropdownMenuTrigger = ({ children, asChild }: { children: React.ReactNode, asChild?: boolean }) => {
  const context = React.useContext(DropdownContext)
  if (asChild && React.isValidElement(children)) {
    return React.cloneElement(children as React.ReactElement, {
      onClick: (e: any) => {
        children.props.onClick?.(e)
        context.setOpen(!context.open)
      }
    })
  }
  return <div onClick={() => context.setOpen(!context.open)}>{children}</div>
}

export const DropdownMenuContent = ({ children, className, align = "end" }: { children: React.ReactNode, className?: string, align?: "start" | "end" }) => {
  const context = React.useContext(DropdownContext)
  
  if (!context.open) return null

  return (
    <div className={cn("absolute z-50 mt-2 min-w-[8rem] rounded-md border bg-popover p-1 text-popover-foreground shadow-md data-[state=open]:animate-in data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=open]:fade-in-0 data-[state=closed]:zoom-out-95 data-[state=open]:zoom-in-95 data-[side=bottom]:slide-in-from-top-2 data-[side=left]:slide-in-from-right-2 data-[side=right]:slide-in-from-left-2 data-[side=top]:slide-in-from-bottom-2", align === "end" ? "right-0" : "left-0", className)}>
      {children}
    </div>
  )
}

export const DropdownMenuItem = ({ children, className, onClick, ...props }: React.HTMLAttributes<HTMLDivElement>) => {
  const context = React.useContext(DropdownContext)
  return (
    <div
      className={cn("relative flex cursor-default select-none items-center rounded-sm px-2 py-1.5 text-sm outline-none transition-colors hover:bg-accent hover:text-accent-foreground data-[disabled]:pointer-events-none data-[disabled]:opacity-50 cursor-pointer", className)}
      onClick={(e) => {
        onClick?.(e)
        context.setOpen(false)
      }}
      {...props}
    >
      {children}
    </div>
  )
}

const DropdownContext = React.createContext<{ open: boolean, setOpen: (open: boolean) => void }>({ open: false, setOpen: () => {} })

export const DropdownMenuRoot = ({ children }: { children: React.ReactNode }) => {
  const [open, setOpen] = React.useState(false)
  const ref = React.useRef<HTMLDivElement>(null)

  React.useEffect(() => {
    const clickOutside = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) {
        setOpen(false)
      }
    }
    document.addEventListener("mousedown", clickOutside)
    return () => document.removeEventListener("mousedown", clickOutside)
  }, [])

  return (
    <DropdownContext.Provider value={{ open, setOpen }}>
      <div ref={ref} className="relative inline-block text-left">{children}</div>
    </DropdownContext.Provider>
  )
}

// Re-export DropdownMenu as Root for simplicity
export { DropdownMenuRoot as DropdownMenuProvider }
