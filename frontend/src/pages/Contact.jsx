import { useState } from 'react'
import { Mail, MapPin, Phone } from 'lucide-react'
import { ApiError, sendContactMessage } from '../services/authApi'

const initialForm = { name: '', email: '', subject: '', message: '' }

const Contact = () => {
  const [form, setForm] = useState(initialForm)
  const [status, setStatus] = useState({ type: '', message: '' })
  const [isSubmitting, setIsSubmitting] = useState(false)

  const change = ({ target: { name, value } }) => {
    setForm((current) => ({ ...current, [name]: value }))
    setStatus({ type: '', message: '' })
  }

  const submit = async (event) => {
    event.preventDefault()
    setIsSubmitting(true)
    try {
      const result = await sendContactMessage(form)
      setStatus({ type: 'success', message: result.message })
      setForm(initialForm)
    } catch (error) {
      setStatus({
        type: 'error',
        message: error instanceof ApiError ? error.message : 'Your message could not be sent.',
      })
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <main className="bg-[#f6f9ff] py-20 px-6 min-h-[70vh]">
      <div className="mx-auto grid max-w-6xl gap-10 lg:grid-cols-[0.8fr_1.2fr]">
        <section>
          <p className="font-bold uppercase tracking-widest text-medzo-green">Contact Medzo</p>
          <h1 className="mt-3 text-4xl font-bold text-[#0a192f]">How can we help?</h1>
          <p className="mt-4 text-[#4a5568]">Send our pharmacy team a message and we will respond as soon as possible.</p>
          <div className="mt-10 space-y-5 text-[#4a5568]">
            <p className="flex items-center gap-3"><Phone className="text-medzo-blue" /> +94 11 234 5678</p>
            <p className="flex items-center gap-3"><Mail className="text-medzo-blue" /> support@medzo.lk</p>
            <p className="flex items-center gap-3"><MapPin className="text-medzo-blue" /> Colombo, Sri Lanka</p>
          </div>
        </section>

        <form onSubmit={submit} className="rounded-3xl bg-white p-8 shadow-sm space-y-5" noValidate>
          {status.message && <div role="status" className={`rounded-lg p-3 ${status.type === 'success' ? 'bg-green-50 text-green-700' : 'bg-red-50 text-red-700'}`}>{status.message}</div>}
          <div className="grid gap-5 sm:grid-cols-2">
            <label className="text-sm font-semibold">Name<input required name="name" value={form.name} onChange={change} className="mt-2 w-full rounded-lg border border-gray-200 p-3" /></label>
            <label className="text-sm font-semibold">Email<input required type="email" name="email" value={form.email} onChange={change} className="mt-2 w-full rounded-lg border border-gray-200 p-3" /></label>
          </div>
          <label className="block text-sm font-semibold">Subject<input required name="subject" value={form.subject} onChange={change} className="mt-2 w-full rounded-lg border border-gray-200 p-3" /></label>
          <label className="block text-sm font-semibold">Message<textarea required minLength={10} rows={6} name="message" value={form.message} onChange={change} className="mt-2 w-full resize-none rounded-lg border border-gray-200 p-3" /></label>
          <button type="submit" disabled={isSubmitting} className="gradient-btn rounded-lg px-7 py-3 font-bold text-white disabled:opacity-60">{isSubmitting ? 'Sending…' : 'Send Message'}</button>
        </form>
      </div>
    </main>
  )
}

export default Contact
