import { useState } from 'react'
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom'
import { ArrowLeft, User, Lock, Eye, EyeOff } from 'lucide-react'
import { ApiError } from '../services/authApi'
import { useAuth } from '../auth/AuthContext'
import { getRoleHome } from '../auth/roleRouting'

const Login = () => {
  const navigate = useNavigate()
  const location = useLocation()
  const { login, isAuthenticated, isLoading, user } = useAuth()
  const [showPassword, setShowPassword] = useState(false)
  const [formData, setFormData] = useState({ identifier: '', password: '' })
  const [errors, setErrors] = useState({})
  const [submitError, setSubmitError] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  const handleChange = ({ target: { name, value } }) => {
    setFormData((current) => ({ ...current, [name]: value }))
    setErrors((current) => ({ ...current, [name]: '' }))
    setSubmitError('')
  }

  const handleSubmit = async (event) => {
    event.preventDefault()
    const nextErrors = {}

    if (formData.identifier.trim().length < 3) nextErrors.identifier = 'Enter your Staff ID, username, or email.'
    if (formData.password.length < 6) nextErrors.password = 'Password must be at least 6 characters.'

    if (Object.keys(nextErrors).length > 0) {
      setErrors(nextErrors)
      return
    }

    setIsSubmitting(true)
    setSubmitError('')

    try {
      const session = await login({ identifier: formData.identifier.trim(), password: formData.password })
      navigate(location.state?.from?.pathname || getRoleHome(session.user), { replace: true })
    } catch (error) {
      if (error instanceof ApiError) {
        setErrors(error.errors)
        setSubmitError(error.message)
      } else {
        setSubmitError('An unexpected error occurred. Please try again.')
      }
    } finally {
      setIsSubmitting(false)
    }
  }

  if (!isLoading && isAuthenticated) return <Navigate to={getRoleHome(user)} replace />

  return (
    <div className="min-h-screen bg-[#f8f9fb] flex flex-col font-sans">
      <header className="w-full flex justify-between items-center px-12 py-6">
        <Link to="/" className="flex items-center gap-2 text-medzo-blue font-bold text-2xl">
          <img src="/hospital-icon1.svg" alt="Medzo Logo" className="w-6 h-6" />
          <span>Medzo</span>
        </Link>
        <Link to="/" className="flex items-center gap-2 text-[#4a5568] hover:text-[#0a192f] font-semibold transition-colors">
          <ArrowLeft size={18} /> Back to Home
        </Link>
      </header>

      <main className="flex-1 flex items-center justify-center p-6">
        <div className="bg-white rounded-[2rem] shadow-[0_8px_30px_rgb(0,0,0,0.04)] w-full max-w-5xl min-h-[600px] flex items-center justify-center p-12">
          <div className="w-full max-w-[500px]">
            <div className="text-center mb-10">
              <h1 className="text-3xl font-bold text-[#0a192f] mb-2">Welcome</h1>
              <p className="text-[#6b7280]">Please sign in to access your Medzo account.</p>
            </div>

            {submitError && <div role="alert" className="mb-6 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">{submitError}</div>}

            <form className="space-y-6" onSubmit={handleSubmit} noValidate>
              <div className="space-y-2">
                <label htmlFor="login-identifier" className="block text-sm font-bold text-[#0a192f]">Staff ID, Username, or Email</label>
                <div className="relative">
                  <span className="absolute inset-y-0 left-0 pl-4 flex items-center pointer-events-none"><User size={20} className="text-[#a0aec0]" /></span>
                  <input id="login-identifier" type="text" name="identifier" value={formData.identifier} onChange={handleChange} autoComplete="username" aria-invalid={Boolean(errors.identifier)} placeholder="Staff ID, username, or email" className="w-full pl-12 pr-4 py-3.5 bg-[#f8fafc] border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-medzo-blue focus:border-transparent transition-all placeholder:text-[#a0aec0]" />
                </div>
                {errors.identifier && <p className="text-red-500 text-xs">{errors.identifier}</p>}
              </div>

              <div className="space-y-2">
                <label htmlFor="login-password" className="block text-sm font-bold text-[#0a192f]">Password</label>
                <div className="relative">
                  <span className="absolute inset-y-0 left-0 pl-4 flex items-center pointer-events-none"><Lock size={20} className="text-[#a0aec0]" /></span>
                  <input id="login-password" type={showPassword ? 'text' : 'password'} name="password" value={formData.password} onChange={handleChange} autoComplete="current-password" aria-invalid={Boolean(errors.password)} placeholder="Enter your password" className="w-full pl-12 pr-12 py-3.5 bg-[#f8fafc] border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-medzo-blue focus:border-transparent transition-all placeholder:text-[#a0aec0]" />
                  <button type="button" onClick={() => setShowPassword((current) => !current)} aria-label="Toggle password visibility" className="absolute inset-y-0 right-0 pr-4 flex items-center text-[#a0aec0] hover:text-[#4a5568]">
                    {showPassword ? <Eye size={20} /> : <EyeOff size={20} />}
                  </button>
                </div>
                {errors.password && <p className="text-red-500 text-xs">{errors.password}</p>}
              </div>

              <button type="submit" disabled={isSubmitting} className="w-full gradient-btn text-white py-4 rounded-lg font-bold hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-60 transition-opacity">
                {isSubmitting ? 'Signing in…' : 'Login to Account'}
              </button>

              <p className="text-center text-sm text-[#6b7280]">
                Need an account?{' '}<Link to="/signup" className="font-bold text-medzo-blue hover:text-blue-700">Sign up</Link>
              </p>
            </form>
          </div>
        </div>
      </main>
    </div>
  )
}

export default Login
