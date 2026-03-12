import { Link } from 'react-router-dom'

export default function Breadcrumb({ items }) {
  return (
    <nav className="breadcrumb" aria-label="Breadcrumb">
      {items.map((item, index) => {
        const isLast = index === items.length - 1
        return (
          <span key={index} className="breadcrumb-item">
            {!isLast ? (
              <>
                <Link to={item.to} className="breadcrumb-link">{item.label}</Link>
                <span className="breadcrumb-sep" aria-hidden="true">›</span>
              </>
            ) : (
              <span className="breadcrumb-current">{item.label}</span>
            )}
          </span>
        )
      })}
    </nav>
  )
}
