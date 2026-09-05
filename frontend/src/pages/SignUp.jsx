import { useState } from 'react'
import { Link, Navigate, useNavigate } from 'react-router-dom'
import { ArrowLeft, User, Mail, Lock, Eye, EyeOff } from 'lucide-react'
import { ApiError } from '../services/authApi'
import { useAuth } from '../auth/AuthContext'
import { getRoleHome } from '../auth/roleRouting'

const initialForm = {
  firstName: '',
  lastName: '',
  username: '',
  staffId: '',
  email: '',
  password: '',
  confirmPassword: '',
}

const SignUp = () => {
  const navigate = useNavigate()
  const { register, isAuthenticated, isLoading, user } = useAuth()
  const [showPassword, setShowPassword] = useState(false)
  const [showConfirmPassword, setShowConfirmPassword] = useState(false)
  const [formData, setFormData] = useState(initialForm)
  const [errors, setErrors] = useState({})
  const [submitError, setSubmitError] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  const handleChange = ({ target: { name, value } }) => {
    setFormData((current) => ({ ...current, [name]: value }))
    setErrors((current) => ({ ...current, [name]: '' }))
    setSubmitError('')
  }

  const validate = () => {
    const nextErrors = {}
    if (!formData.firstName.trim()) nextErrors.firstName = 'First name is required.'
    if (!formData.lastName.trim()) nextErrors.lastName = 'Last name is required.'
    if (formData.username.trim().length < 3) nextErrors.username = 'Username must be at least 3 characters.'
    if (!/^[PI][A-Za-z0-9]{3,19}$/i.test(formData.staffId.trim())) nextErrors.staffId = 'Staff ID must start with P or I and contain 4-20 letters or numbers.'
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.email)) nextErrors.email = 'Enter a valid email address.'
    if (formData.password.length < 8) nextErrors.password = 'Password must be at least 8 characters.'
    else if (!/[A-Z]/.test(formData.password) || !/[a-z]/.test(formData.password) || !/[0-9]/.test(formData.password) || !/[^a-zA-Z0-9]/.test(formData.password)) {
      nextErrors.password = 'Use uppercase, lowercase, a number, and a special character.'
    }
    if (formData.confirmPassword !== formData.password) nextErrors.confirmPassword = 'Passwords do not match.'
    return nextErrors
  }

  const handleSubmit = async (event) => {
    event.preventDefault()
    const nextErrors = validate()
    if (Object.keys(nextErrors).length > 0) {
      setErrors(nextErrors)
      return
    }

    setIsSubmitting(true)
    setSubmitError('')

    try {
      const session = await register({
        ...formData,
        firstName: formData.firstName.trim(),
        lastName: formData.lastName.trim(),
        username: formData.username.trim(),
        staffId: formData.staffId.trim(),
        email: formData.email.trim(),
      })
      navigate(getRoleHome(session.user), { replace: true })
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

  const textInput = (name, label, placeholder, type = 'text', Icon = User, autoComplete = undefined) => (
    <div className="space-y-2">
      <label htmlFor={`signup-${name}`} className="block text-sm font-bold text-[#0a192f]">{label}</label>
      <div className="relative">
        <span className="absolute inset-y-0 left-0 pl-4 flex items-center pointer-events-none"><Icon size={20} className="text-[#a0aec0]" /></span>
        <input id={`signup-${name}`} type={type} name={name} value={formData[name]} onChange={handleChange} autoComplete={autoComplete} aria-invalid={Boolean(errors[name])} placeholder={placeholder} className="w-full pl-12 pr-4 py-3.5 bg-[#f8fafc] border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-medzo-blue focus:border-transparent transition-all placeholder:text-[#a0aec0]" />
      </div>
      {errors[name] && <p className="text-red-500 text-xs">{errors[name]}</p>}
    </div>
  )

  const passwordInput = (name, label, visible, setVisible, autoComplete) => (
    <div className="space-y-2">
      <label htmlFor={`signup-${name}`} className="block text-sm font-bold text-[#0a192f]">{label}</label>
      <div className="relative">
        <span className="absolute inset-y-0 left-0 pl-4 flex items-center pointer-events-none"><Lock size={20} className="text-[#a0aec0]" /></span>
        <input id={`signup-${name}`} type={visible ? 'text' : 'password'} name={name} value={formData[name]} onChange={handleChange} autoComplete={autoComplete} aria-invalid={Boolean(errors[name])} placeholder={label} className="w-full pl-12 pr-12 py-3.5 bg-[#f8fafc] border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-medzo-blue focus:border-transparent transition-all placeholder:text-[#a0aec0]" />
        <button type="button" onClick={() => setVisible((current) => !current)} aria-label={`Toggle ${label.toLowerCase()} visibility`} className="absolute inset-y-0 right-0 pr-4 flex items-center text-[#a0aec0] hover:text-[#4a5568]">
          {visible ? <Eye size={20} /> : <EyeOff size={20} />}
        </button>
      </div>
      {errors[name] && <p className="text-red-500 text-xs">{errors[name]}</p>}
    </div>
  )

  if (!isLoading && isAuthenticated) return <Navigate to={getRoleHome(user)} replace />

  return (
    <div className="min-h-screen bg-[#f8f9fb] flex flex-col font-sans">
      <header className="w-full flex justify-between items-center px-12 py-6">
        <Link to="/" className="flex items-center gap-2 text-medzo-blue font-bold text-2xl">
          <img src="/hospital-icon1.svg" alt="Medzo Logo" className="w-6 h-6" />
          <span>Medzo</span>
        </Link>
        <Link to="/" className="flex items-center gap-2 text-[#4a5568] hover:text-[#0a192f] font-semibold transition-colors"><ArrowLeft size={18} /> Back to Home</Link>
      </header>

      <main className="flex-1 flex items-center justify-center p-6">
        <div className="bg-white rounded-[2rem] shadow-[0_8px_30px_rgb(0,0,0,0.04)] w-full max-w-5xl flex items-center justify-center p-12">
          <div className="w-full max-w-[560px]">
            <div className="text-center mb-8">
              <h1 className="text-3xl font-bold text-[#0a192f] mb-2">Create Account</h1>
              <p className="text-[#6b7280]">Fill in the details below to register your Medzo account.</p>
            </div>

            {submitError && <div role="alert" className="mb-6 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">{submitError}</div>}

            <form className="space-y-5" onSubmit={handleSubmit} noValidate>
              <div className="grid gap-5 sm:grid-cols-2">
                {textInput('firstName', 'First Name', 'Enter first name', 'text', User, 'given-name')}
                {textInput('lastName', 'Last Name', 'Enter last name', 'text', User, 'family-name')}
              </div>
              {textInput('username', 'Username', 'Choose a username', 'text', User, 'username')}
              {textInput('staffId', 'Staff ID', 'P1001 or I1001', 'text', User, 'off')}
              {textInput('email', 'Email Address', 'name@example.com', 'email', Mail, 'email')}
              {passwordInput('password', 'Password', showPassword, setShowPassword, 'new-password')}
              {passwordInput('confirmPassword', 'Confirm Password', showConfirmPassword, setShowConfirmPassword, 'new-password')}

              <button type="submit" disabled={isSubmitting} className="w-full gradient-btn text-white py-4 rounded-lg font-bold hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-60 transition-opacity">
                {isSubmitting ? 'Creating account…' : 'Sign Up'}
              </button>

              <p className="text-center text-sm text-[#6b7280]">
                Already have an account?{' '}<Link to="/login" className="font-bold text-medzo-blue hover:text-blue-700">Login here</Link>
              </p>
            </form>
          </div>
        </div>
      </main>
    </div>
  )
}

export default SignUp
