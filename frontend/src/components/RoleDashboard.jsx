import { Link } from 'react-router-dom'
import { ArrowLeft, LogOut, ShieldCheck } from 'lucide-react'
import { useAuth } from '../auth/AuthContext'

const RoleDashboard = ({ title, description }) => {
  const { user, logout } = useAuth()

  return (
    <main className="min-h-screen bg-[#f4f8ff] px-6 py-10">
      <div className="mx-auto max-w-7xl">
        <header className="flex flex-col gap-5 rounded-3xl bg-[#0a192f] p-8 text-white sm:flex-row sm:items-center sm:justify-between">
          <div>
            <div className="mb-3 flex items-center gap-2 text-medzo-green"><ShieldCheck /> Secure staff workspace</div>
            <h1 className="text-3xl font-bold">{title}</h1>
            <p className="mt-2 text-slate-300">{description}</p>
          </div>
          <div className="flex flex-wrap gap-3">
            <Link to="/" className="flex items-center justify-center gap-2 rounded-lg bg-white/10 px-5 py-3 font-semibold hover:bg-white/20"><ArrowLeft size={18} /> Back to home</Link>
            <button onClick={logout} className="flex items-center justify-center gap-2 rounded-lg bg-white/10 px-5 py-3 font-semibold hover:bg-white/20"><LogOut size={18} /> Logout</button>
          </div>
        </header>

        <section className="mt-6 w-full">
          <article className="flex flex-col gap-3 rounded-2xl bg-white px-8 py-5 shadow-sm sm:flex-row sm:items-center sm:justify-between">
            <div>
              <p className="text-sm font-semibold uppercase tracking-wide text-medzo-blue">Signed in as</p>
              <h2 className="mt-1 text-xl font-bold text-[#0a192f]">{user.firstName} {user.lastName}</h2>
            </div>
            <div className="text-left text-sm text-[#4a5568] sm:text-right">
              <p>Staff ID: {user.staffId}</p>
              <p>Role: {user.roles.join(', ')}</p>
            </div>
          </article>
        </section>
      </div>
    </main>
  )
}

export default RoleDashboard
