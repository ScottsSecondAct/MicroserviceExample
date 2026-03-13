import { useState } from 'react'
import { Check, ChevronsUpDown } from 'lucide-react'
import { Command } from 'cmdk'
import { Popover, PopoverContent, PopoverTrigger } from './popover'
import { Button } from './button'
import { cn } from '../../lib/utils'

/**
 * Combobox — searchable select built on cmdk.
 *
 * Props:
 *   options   : [{ value: string, label: string }]
 *   value     : string (selected value)
 *   onChange  : (value: string) => void
 *   placeholder : string   — trigger button text when nothing selected
 *   searchPlaceholder : string
 *   emptyText : string
 *   className : string
 */
export function Combobox({
  options = [],
  value,
  onChange,
  placeholder = 'Select…',
  searchPlaceholder = 'Search…',
  emptyText = 'No results found.',
  className,
}) {
  const [open, setOpen] = useState(false)

  const selected = options.find((opt) => opt.value === value)

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <Button
          variant="outline"
          role="combobox"
          aria-expanded={open}
          className={cn('w-full justify-between font-normal', className)}
        >
          {selected ? selected.label : placeholder}
          <ChevronsUpDown className="ml-2 h-4 w-4 shrink-0 opacity-50" />
        </Button>
      </PopoverTrigger>
      <PopoverContent className="w-full p-0" align="start">
        <Command>
          <Command.Input
            placeholder={searchPlaceholder}
            className="h-9 w-full border-0 bg-transparent px-3 py-2 text-sm outline-none placeholder:text-muted-foreground"
          />
          <Command.Empty className="py-6 text-center text-sm text-muted-foreground">
            {emptyText}
          </Command.Empty>
          <Command.List className="max-h-60 overflow-y-auto p-1">
            <Command.Group>
              {options.map((opt) => (
                <Command.Item
                  key={opt.value}
                  value={opt.value}
                  onSelect={(currentValue) => {
                    onChange(currentValue === value ? '' : currentValue)
                    setOpen(false)
                  }}
                  className="relative flex cursor-default select-none items-center rounded-sm px-2 py-1.5 text-sm outline-none hover:bg-accent hover:text-accent-foreground data-[selected=true]:bg-accent data-[selected=true]:text-accent-foreground"
                >
                  <Check
                    className={cn(
                      'mr-2 h-4 w-4',
                      value === opt.value ? 'opacity-100' : 'opacity-0'
                    )}
                  />
                  {opt.label}
                </Command.Item>
              ))}
            </Command.Group>
          </Command.List>
        </Command>
      </PopoverContent>
    </Popover>
  )
}
