import * as React from "react"
import { cn } from "@/lib/utils"

const TabsContext = React.createContext<{
  value: string
  onValueChange: (value: string) => void
}>({ value: "", onValueChange: () => {} })

export function Tabs({ defaultValue, value, onValueChange, children, className }: { defaultValue?: string, value?: string, onValueChange?: (val: string) => void, children: React.ReactNode, className?: string }) {
  const [internalValue, setInternalValue] = React.useState(value || defaultValue || "")
  
  React.useEffect(() => {
    if (value !== undefined) setInternalValue(value)
  }, [value])
  
  const handleValueChange = (val: string) => {
    setInternalValue(val)
    onValueChange?.(val)
  }

  return (
    <TabsContext.Provider value={{ value: internalValue, onValueChange: handleValueChange }}>
      <div className={className}>{children}</div>
    </TabsContext.Provider>
  )
}

export function TabsList({ className, ...props }: React.HTMLAttributes<HTMLDivElement>) {
  return (
    <div
      className={cn("inline-flex h-10 items-center justify-center rounded-md bg-muted p-1 text-muted-foreground", className)}
      {...props}
    />
  )
}

export function TabsTrigger({ value, className, children, ...props }: React.ButtonHTMLAttributes<HTMLButtonElement> & { value: string }) {
  const { value: selectedValue, onValueChange } = React.useContext(TabsContext)
  const isSelected = selectedValue === value
  
  return (
    <button
      type="button"
      role="tab"
      aria-selected={isSelected}
      onClick={() => onValueChange(value)}
      className={cn(
        "inline-flex items-center justify-center whitespace-nowrap rounded-sm px-3 py-1.5 text-sm font-medium ring-offset-background transition-all focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-50",
        isSelected ? "bg-background text-foreground shadow-sm" : "hover:bg-background/50 hover:text-foreground",
        className
      )}
      {...props}
    >
      {children}
    </button>
  )
}

export function TabsContent({ value, className, children, ...props }: React.HTMLAttributes<HTMLDivElement> & { value: string }) {
  const { value: selectedValue } = React.useContext(TabsContext)
  
  if (selectedValue !== value) return null
  
  return (
    <div
      role="tabpanel"
      className={cn("mt-2 ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2", className)}
      {...props}
    >
      {children}
    </div>
  )
}
