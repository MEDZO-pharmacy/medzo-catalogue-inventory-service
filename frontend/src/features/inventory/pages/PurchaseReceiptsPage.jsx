import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import PurchaseReceiptTable from '../components/PurchaseReceiptTable'
import { getPurchaseReceipts } from '../api/inventoryApi'

export default function PurchaseReceiptsPage() {
  const [data, setData] = useState({ items: [] })
  const [status, setStatus] = useState('loading')
  const [error, setError] = useState('')

  const load = () => {
    setStatus('loading')
    setError('')
    getPurchaseReceipts()
      .then(result => { setData(result); setStatus('ready') })
      .catch(requestError => { setError(requestError.message || 'Purchase updates could not be loaded.'); setStatus('error') })
  }

  useEffect(() => {
    getPurchaseReceipts()
      .then(result => { setData(result); setStatus('ready') })
      .catch(requestError => { setError(requestError.message || 'Purchase updates could not be loaded.'); setStatus('error') })
  }, [])

  return <main className="min-h-screen bg-[#f4f8ff] px-6 py-10">
    <div className="mx-auto max-w-7xl">
      <div className="mb-6 flex flex-wrap items-center justify-between gap-4">
        <div>
          <Link to="/inventory" className="mb-3 inline-block font-semibold text-medzo-blue">Back to inventory</Link>
          <h1 className="text-3xl font-bold text-[#0a192f]">Purchase stock updates</h1>
          <p className="mt-2 text-[#4a5568]">Purchase receipts applied automatically from the Purchasing/Supplier service.</p>
        </div>
        <button type="button" onClick={load} disabled={status === 'loading'} className="rounded-lg border border-medzo-blue px-5 py-3 font-semibold text-medzo-blue disabled:opacity-60">Refresh</button>
      </div>
      {status === 'loading' && <p className="rounded-2xl bg-white p-10 text-center text-[#6b7280]">Loading purchase updates…</p>}
      {status === 'error' && <p role="alert" className="rounded-lg bg-red-50 p-4 text-red-700">{error}</p>}
      {status === 'ready' && <PurchaseReceiptTable items={data.items} />}
    </div>
  </main>
}
