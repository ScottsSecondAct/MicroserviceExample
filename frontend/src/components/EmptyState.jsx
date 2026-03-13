import { Button } from './ui/button.jsx'

export function EmptyState({ icon, heading, description, action }) {
  return (
    <div className="flex flex-col items-center justify-center py-16 px-6 text-center">
      <div className="flex items-center justify-center w-16 h-16 rounded-full bg-gray-100 text-gray-400 mb-4">
        {icon}
      </div>
      <h3 className="text-lg font-semibold text-gray-900 mb-1">{heading}</h3>
      {description && (
        <p className="text-sm text-gray-500 max-w-sm mb-5">{description}</p>
      )}
      {action && (
        <Button onClick={action.onClick}>{action.label}</Button>
      )}
    </div>
  )
}
