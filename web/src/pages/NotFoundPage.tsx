import { Link } from 'react-router'

export function NotFoundPage() {
  return (
    <div className="state">
      <h1>Page not found</h1>
      <Link to="/" className="btn">
        Back to my events
      </Link>
    </div>
  )
}
