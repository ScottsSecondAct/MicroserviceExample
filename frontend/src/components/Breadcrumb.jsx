import { Link } from 'react-router-dom'
import { ChevronRight } from 'lucide-react'

export default function Breadcrumb({ items }) {
  return (
    <nav className="flex items-center flex-wrap gap-1 mb-3 text-sm" aria-label="Breadcrumb">
      {items.map((item, index) => {
        const isLast = index === items.length - 1
        return (
          <span key={index} className="flex items-center gap-1">
            {!isLast ? (
              <>
                <Link to={item.to} className="text-gray-500 hover:text-blue-600 no-underline hover:underline">
                  {item.label}
                </Link>
                <ChevronRight size={14} className="text-gray-400" />
              </>
            ) : (
              <span className="text-gray-700 font-medium">{item.label}</span>
            )}
          </span>
        )
      })}
    </nav>
  )
}
