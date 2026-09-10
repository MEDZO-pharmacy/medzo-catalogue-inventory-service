import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import SaleIssueTable from '../components/SaleIssueTable'
import { getSaleIssues } from '../api/inventoryApi'

export default function SaleIssuesPage() {
  const [data, setData] = useState({ items: [] })
  const [status, setStatus] = useState('loading')
  const [error, setError] = useState('')

  const load = () => {
    setStatus('loading')
    setError('')
    getSaleIssues()
      .then(result => { setData(result); setStatus('ready') })
      .catch(requestError => { setError(requestError.message || 'Sale updates could not be loaded.'); setStatus('error') })
  }

  useEffect(() => {
    getSaleIssues()
      .then(result => { setData(result); setStatus('ready') })
      .catch(requestError => { setError(requestError.message || 'Sale updates could not be loaded.'); setStatus('error') })
  }, [])

  return <main className="min-h-screen bg-[#f4f8ff] px-4 py-8 sm:px-6 sm:py-10">
    <div className="mx-auto max-w-7xl">
      <div className="mb-6 flex flex-wrap items-center justify-between gap-4">
        <div>
          <Link to="/inventory" className="mb-3 inline-block font-semibold text-medzo-blue">Back to inventory</Link>
          <h1 className="text-2xl font-bold text-[#0a192f] sm:text-3xl">Sale stock updates</h1>
          <p className="mt-2 text-[#4a5568]">Stock deducted automatically from the earliest-expiring sellable batches.</p>
        </div>
        <button type="button" onClick={load} disabled={status === 'loading'} className="w-full rounded-lg border border-medzo-blue px-5 py-3 font-semibold text-medzo-blue transition-colors hover:bg-blue-50 disabled:opacity-60 sm:w-auto">Refresh</button>
      </div>
      {status === 'loading' && <p role="status" className="rounded-2xl bg-white p-10 text-center text-[#6b7280]">Loading sale updates…</p>}
      {status === 'error' && <p role="alert" className="rounded-lg bg-red-50 p-4 text-red-700">{error}</p>}
      {status === 'ready' && <SaleIssueTable items={data.items} />}
    </div>
  </main>
}
