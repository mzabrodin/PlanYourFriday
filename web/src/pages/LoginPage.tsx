import type { CSSProperties } from 'react'
import { Navigate, useSearchParams } from 'react-router'
import { CalendarCheck, CalendarClock, Vote } from 'lucide-react'
import { api } from '../api'
import { useMe } from '../api/queries'
import { Loading } from '../components/States'

function safeReturnTo(value: string | null): string {
  return value && value.startsWith('/') && !value.startsWith('//') ? value : '/'
}

const STEPS = [
  {
    stage: 'planning',
    icon: CalendarClock,
    title: 'Everyone marks their free time',
    text: 'By hand, from Google Calendar, or with one tap on your usual hours.',
  },
  {
    stage: 'voting',
    icon: Vote,
    title: 'Friends vote on the best slots',
    text: 'Up to six top options, even when there is no perfect match.',
  },
  {
    stage: 'scheduled',
    icon: CalendarCheck,
    title: 'It lands in every calendar',
    text: 'Confirm the time and the event shows up for everyone.',
  },
] as const

// Free/busy heat for the preview grid: how many of the 4 friends are free in each slot.
const PREVIEW_DAYS = ['Wed', 'Thu', 'Fri', 'Sat']
const PREVIEW_HEAT = [
  [1, 2, 1, 0],
  [2, 3, 4, 2],
  [1, 3, 4, 3],
  [0, 2, 3, 1],
]

export function LoginPage() {
  const [params] = useSearchParams()
  const returnTo = safeReturnTo(params.get('returnTo'))
  const { data: me, isPending } = useMe()
  const joining = returnTo.startsWith('/join/')

  if (isPending) return <Loading />
  if (me) return <Navigate to={returnTo} replace />

  return (
    <div className="landing">
      <section className="landing-hero">
        <div className="landing-copy">
          <span className="landing-eyebrow">Group plans, minus the group chat chaos</span>
          <h1>
            Find a time that works <em>for everyone</em>
          </h1>
          <p className="lead">
            Create an event, invite friends, mark when you're free, and PlanYourFriday picks the slot where everyone can
            make it. The event lands straight in your Google Calendar.
          </p>
          {joining && <p className="notice">Sign in to join the event you were invited to.</p>}
          <div className="landing-cta">
            <button type="button" className="btn btn-primary btn-google" onClick={() => api.startLogin(returnTo)}>
              <span className="google-mark">
                <svg viewBox="0 0 48 48" width="18" height="18" aria-hidden="true">
                  <path
                    fill="#FFC107"
                    d="M43.6 20.5H42V20H24v8h11.3C33.7 32.7 29.2 36 24 36c-6.6 0-12-5.4-12-12s5.4-12 12-12c3.1 0 5.8 1.2 7.9 3.1l5.7-5.7C34 6.1 29.3 4 24 4 12.9 4 4 12.9 4 24s8.9 20 20 20 20-8.9 20-20c0-1.3-.1-2.4-.4-3.5z"
                  />
                  <path
                    fill="#FF3D00"
                    d="m6.3 14.7 6.6 4.8C14.7 15.1 19 12 24 12c3.1 0 5.8 1.2 7.9 3.1l5.7-5.7C34 6.1 29.3 4 24 4 16.3 4 9.7 8.3 6.3 14.7z"
                  />
                  <path
                    fill="#4CAF50"
                    d="M24 44c5.2 0 9.9-2 13.4-5.2l-6.2-5.2C29.2 35.1 26.7 36 24 36c-5.2 0-9.6-3.3-11.3-8l-6.5 5C9.5 39.6 16.2 44 24 44z"
                  />
                  <path
                    fill="#1976D2"
                    d="M43.6 20.5H42V20H24v8h11.3c-.8 2.2-2.2 4.2-4.1 5.6l6.2 5.2C37 39.2 44 34 44 24c0-1.3-.1-2.4-.4-3.5z"
                  />
                </svg>
              </span>
              Sign in with Google
            </button>
            <span className="landing-fineprint">Free · no new password · your calendar stays private</span>
          </div>
        </div>

        <div className="landing-preview" aria-hidden="true">
          <div className="preview-card preview-grid-card">
            <div className="preview-card-head">
              <span className="preview-dot stage-planning" />
              <strong>Board games night</strong>
              <span className="preview-meta">4 friends</span>
            </div>
            <div className="preview-grid">
              <span />
              {PREVIEW_DAYS.map((day) => (
                <span key={day} className="preview-day">
                  {day}
                </span>
              ))}
              {PREVIEW_HEAT.map((row, i) => (
                <div key={i} className="preview-row">
                  <span className="preview-hour">{17 + i}:00</span>
                  {row.map((free, j) => (
                    <span
                      key={j}
                      className={`preview-cell${free === 4 ? ' is-best' : ''}`}
                      style={{ '--free': free / 4 } as CSSProperties}
                    />
                  ))}
                </div>
              ))}
            </div>
          </div>

          <div className="preview-card preview-vote-card">
            <div className="preview-card-head">
              <span className="preview-dot stage-voting" />
              <strong>Vote</strong>
            </div>
            {[
              ['Fri 18:00', 92],
              ['Thu 19:00', 64],
              ['Sat 18:00', 38],
            ].map(([label, score]) => (
              <div key={label} className="preview-vote">
                <span>{label}</span>
                <span className="preview-bar">
                  <span style={{ width: `${score}%` }} />
                </span>
              </div>
            ))}
          </div>

          <div className="preview-card preview-event-card">
            <span className="preview-event-icon">
              <CalendarCheck size={18} />
            </span>
            <div>
              <strong>Fri, 18:00 – 21:00</strong>
              <span className="preview-meta">Added to 4 calendars</span>
            </div>
          </div>
        </div>
      </section>

      <ol className="landing-steps">
        {STEPS.map(({ stage, icon: Icon, title, text }, i) => (
          <li key={stage} className={`landing-step stage-${stage}`}>
            <span className="landing-step-icon">
              <Icon size={20} />
            </span>
            <span className="landing-step-num">Step {i + 1}</span>
            <h2>{title}</h2>
            <p>{text}</p>
          </li>
        ))}
      </ol>
    </div>
  )
}
