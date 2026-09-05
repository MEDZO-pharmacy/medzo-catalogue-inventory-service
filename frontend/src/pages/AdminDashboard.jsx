import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { ArrowLeft, CheckCircle2, LogOut, Pencil, ShieldCheck, UserPlus, X } from 'lucide-react'
import { useAuth } from '../auth/AuthContext'
import {
  ApiError,
  approveStaffId,
  createUser,
  getDashboard,
  getStaffInvitations,
  setUserStatus,
  updateManagedUser,
} from '../services/authApi'

const emptyUser = {
  staffId: '', username: '', email: '', firstName: '', lastName: '',
  password: '', confirmPassword: '', role: 'Pharmacist', confirmPotentialDuplicate: false,
}
const emptyApproval = { staffId: '', role: 'Pharmacist' }
const rolePrefix = { Pharmacist: 'P', InventoryManager: 'I' }

const AdminDashboard = () => {
  const { user, logout } = useAuth()
  const [dashboard, setDashboard] = useState({ users: [], modules: [], totalUsers: 0 })
  const [invitations, setInvitations] = useState([])
  const [userForm, setUserForm] = useState(emptyUser)
  const [approvalForm, setApprovalForm] = useState(emptyApproval)
  const [userMessage, setUserMessage] = useState({ type: '', text: '' })
  const [approvalMessage, setApprovalMessage] = useState({ type: '', text: '' })
  const [duplicateWarning, setDuplicateWarning] = useState(null)
  const [editingUser, setEditingUser] = useState(null)
  const [pendingDeactivation, setPendingDeactivation] = useState(null)
  const [editMessage, setEditMessage] = useState({ type: '', text: '' })
  const [busy, setBusy] = useState(false)

  const reload = useCallback(async () => {
    const [nextDashboard, nextInvitations] = await Promise.all([
      getDashboard('admin'), getStaffInvitations(),
    ])
    setDashboard(nextDashboard)
    setInvitations(nextInvitations)
  }, [])

  useEffect(() => { reload().catch(() => setUserMessage({ type: 'error', text: 'Admin data could not be loaded.' })) }, [reload])

  const changeUser = ({ target: { name, value } }) => {
    setUserForm((current) => ({ ...current, [name]: value, confirmPotentialDuplicate: false }))
    setDuplicateWarning(null)
    setUserMessage({ type: '', text: '' })
  }

  const submitUser = async (event, confirmDuplicate = false) => {
    event?.preventDefault()
    setBusy(true)
    setUserMessage({ type: '', text: '' })
    try {
      await createUser({ ...userForm, confirmPotentialDuplicate: confirmDuplicate })
      setUserMessage({ type: 'success', text: 'Staff account created successfully.' })
      setUserForm(emptyUser)
      setDuplicateWarning(null)
      await reload()
    } catch (error) {
      if (error instanceof ApiError && error.data?.code === 'potential_duplicate') {
        setDuplicateWarning(error.data)
        setUserMessage({ type: 'warning', text: error.data.message })
      } else {
        setUserMessage({ type: 'error', text: error instanceof ApiError ? error.message : 'Account could not be created.' })
      }
    } finally {
      setBusy(false)
    }
  }

  const submitApproval = async (event) => {
    event.preventDefault()
    setBusy(true)
    try {
      await approveStaffId(approvalForm)
      setApprovalMessage({ type: 'success', text: 'Staff ID approved for signup.' })
      setApprovalForm(emptyApproval)
      await reload()
    } catch (error) {
      setApprovalMessage({ type: 'error', text: error instanceof ApiError ? error.message : 'Staff ID could not be approved.' })
    } finally {
      setBusy(false)
    }
  }

  const confirmDeactivation = async () => {
    if (!pendingDeactivation) return
    const account = pendingDeactivation
    setBusy(true)
    setEditMessage({ type: '', text: '' })
    try {
      const deactivated = await setUserStatus(account.id, false)
      setDashboard((current) => ({
        ...current,
        users: current.users.map((item) => item.id === account.id ? deactivated : item),
      }))
      setInvitations((current) => {
        const existing = current.find((item) => item.staffId === account.staffId)
        if (existing) {
          return current.map((item) => item.staffId === account.staffId
            ? { ...item, isClaimed: true }
            : item)
        }
        return [{
          id: `reserved-${account.staffId}`,
          staffId: account.staffId,
          role: account.roles[0],
          isClaimed: true,
        }, ...current]
      })
      setEditMessage({ type: 'success', text: 'Staff account successfully deactivated.' })
      setPendingDeactivation(null)

      try {
        setInvitations(await getStaffInvitations())
      } catch {
        setApprovalMessage({ type: 'error', text: 'The account was deleted, but claimed Staff IDs could not be refreshed.' })
      }
    } catch (error) {
      const message = error instanceof ApiError && error.status === 503
        ? 'Deactivation could not be completed because the database connection failed. No account changes were made.'
        : error instanceof ApiError ? error.message : 'Account could not be deactivated.'
      setEditMessage({ type: 'error', text: message })
    } finally {
      setBusy(false)
    }
  }

  const startEditing = (staff) => {
    setEditingUser({
      id: staff.id,
      username: staff.username,
      staffId: staff.staffId || '',
      email: staff.email,
      firstName: staff.firstName,
      lastName: staff.lastName,
      role: staff.roles[0] || 'Pharmacist',
    })
    setEditMessage({ type: '', text: '' })
  }

  const changeEdit = ({ target: { name, value } }) => {
    setEditingUser((current) => {
      if (name !== 'role') return { ...current, [name]: value }
      const prefix = rolePrefix[value]
      const staffId = current.staffId ? `${prefix}${current.staffId.slice(1)}` : prefix
      return { ...current, role: value, staffId }
    })
    setEditMessage({ type: '', text: '' })
  }

  const submitEdit = async (event) => {
    event.preventDefault()
    setBusy(true)
    setEditMessage({ type: '', text: '' })
    try {
      const updated = await updateManagedUser(editingUser.id, editingUser)
      setDashboard((current) => ({
        ...current,
        users: current.users.map((item) => item.id === updated.id ? updated : item),
      }))
      setEditMessage({ type: 'success', text: 'Staff account updated successfully.' })
      setEditingUser(null)
    } catch (error) {
      setEditMessage({ type: 'error', text: error instanceof ApiError ? error.message : 'Account could not be updated.' })
    } finally {
      setBusy(false)
    }
  }

  const input = (name, label, type = 'text') => (
    <label className="text-sm font-semibold text-[#0a192f]">{label}
      <input required type={type} name={name} value={userForm[name]} onChange={changeUser} className="mt-1 w-full rounded-lg border border-slate-200 p-3 font-normal" />
    </label>
  )

  return (
    <main className="min-h-screen bg-[#f4f8ff] px-6 py-10">
      <div className="mx-auto max-w-7xl">
        <header className="flex flex-col gap-5 rounded-3xl bg-[#0a192f] p-8 text-white sm:flex-row sm:items-center sm:justify-between">
          <div><div className="mb-3 flex items-center gap-2 text-medzo-green"><ShieldCheck /> Secure staff administration</div><h1 className="text-3xl font-bold">Administrator Dashboard</h1><p className="mt-2 text-slate-300">Signed in as {user.firstName || user.username} · {user.staffId}</p></div>
          <div className="flex flex-wrap gap-3">
            <Link to="/" className="flex items-center justify-center gap-2 rounded-lg bg-white/10 px-5 py-3 font-semibold hover:bg-white/20"><ArrowLeft size={18} /> Back to home</Link>
            <button onClick={logout} className="flex items-center justify-center gap-2 rounded-lg bg-white/10 px-5 py-3 font-semibold hover:bg-white/20"><LogOut size={18} /> Logout</button>
          </div>
        </header>

        <section className="mt-6 w-full">
          <article className="flex flex-col gap-3 rounded-2xl bg-white px-8 py-5 shadow-sm sm:flex-row sm:items-center sm:justify-between">
            <div>
              <p className="text-sm font-semibold uppercase tracking-wide text-medzo-blue">Signed in as</p>
              <h2 className="mt-1 text-xl font-bold text-[#0a192f]">{user.firstName || user.username} {user.lastName}</h2>
            </div>
            <div className="text-left text-sm text-[#4a5568] sm:text-right">
              <p>Staff ID: {user.staffId}</p>
              <p>Role: {user.roles.join(', ')}</p>
            </div>
          </article>
        </section>

        <div className="mt-8 grid gap-8 xl:grid-cols-2">
          <form onSubmit={submitUser} className="rounded-2xl bg-white p-7 shadow-sm">
            <h2 className="flex items-center gap-2 text-xl font-bold"><UserPlus className="text-medzo-blue" /> Create staff account</h2>
            <p className="mt-2 text-sm text-slate-500">The selected role must match the Staff ID prefix.</p>
            {userMessage.text && <p role="alert" className={`mt-4 rounded-lg p-3 ${userMessage.type === 'success' ? 'bg-green-50 text-green-700' : userMessage.type === 'warning' ? 'bg-amber-50 text-amber-800' : 'bg-red-50 text-red-700'}`}>{userMessage.text}</p>}
            <div className="mt-5 grid gap-4 sm:grid-cols-2">
              {input('firstName', 'First name')}{input('lastName', 'Last name')}
              {input('username', 'Username')}{input('email', 'Email', 'email')}
              {input('staffId', `Staff ID (${rolePrefix[userForm.role]} prefix)`)}
              <label className="text-sm font-semibold">Role<select name="role" value={userForm.role} onChange={changeUser} className="mt-1 w-full rounded-lg border border-slate-200 p-3 font-normal"><option>Pharmacist</option><option value="InventoryManager">Inventory Manager</option></select></label>
              {input('password', 'Temporary password', 'password')}{input('confirmPassword', 'Confirm password', 'password')}
            </div>
            {duplicateWarning && <div className="mt-4 rounded-lg border border-amber-300 bg-amber-50 p-4 text-sm text-amber-900"><p>{duplicateWarning.duplicates?.map((item) => `${item.firstName} ${item.lastName} (${item.staffId || item.username})`).join(', ')}</p><button type="button" disabled={busy} onClick={() => submitUser(null, true)} className="mt-3 rounded-lg bg-amber-700 px-4 py-2 font-bold text-white">Create anyway</button></div>}
            <button disabled={busy} className="gradient-btn mt-5 rounded-lg px-6 py-3 font-bold text-white disabled:opacity-60">Create account</button>
          </form>

          <form onSubmit={submitApproval} className="rounded-2xl bg-white p-7 shadow-sm">
            <h2 className="text-xl font-bold">Pre-approve Staff ID for signup</h2>
            <p className="mt-2 text-sm text-slate-500">Only approved, unclaimed IDs can use public signup.</p>
            {approvalMessage.text && <p role="status" className={`mt-4 rounded-lg p-3 ${approvalMessage.type === 'success' ? 'bg-green-50 text-green-700' : 'bg-red-50 text-red-700'}`}>{approvalMessage.text}</p>}
            <div className="mt-5 grid gap-4 sm:grid-cols-2">
              <label className="text-sm font-semibold">Staff ID<input required value={approvalForm.staffId} onChange={(e) => setApprovalForm((current) => ({ ...current, staffId: e.target.value }))} className="mt-1 w-full rounded-lg border border-slate-200 p-3 font-normal" placeholder={`${rolePrefix[approvalForm.role]}1001`} /></label>
              <label className="text-sm font-semibold">Role<select value={approvalForm.role} onChange={(e) => setApprovalForm((current) => ({ ...current, role: e.target.value }))} className="mt-1 w-full rounded-lg border border-slate-200 p-3 font-normal"><option>Pharmacist</option><option value="InventoryManager">Inventory Manager</option></select></label>
            </div>
            <button disabled={busy} className="gradient-btn mt-5 rounded-lg px-6 py-3 font-bold text-white disabled:opacity-60">Approve Staff ID</button>
            <div className="mt-6 max-h-52 overflow-auto space-y-2">{invitations.map((item) => <div key={item.id} className="flex justify-between rounded-lg bg-slate-50 p-3 text-sm"><span className="font-bold">{item.staffId} · {item.role}</span><span className={item.isClaimed ? 'text-slate-500' : 'text-green-700'}>{item.isClaimed ? 'Claimed' : 'Available'}</span></div>)}</div>
          </form>
        </div>

        {editMessage.text && <p role="status" className={`mt-6 rounded-lg p-3 ${editMessage.type === 'success' ? 'bg-green-50 text-green-700' : 'bg-red-50 text-red-700'}`}>{editMessage.text}</p>}

        {editingUser && <form onSubmit={submitEdit} className="mt-8 rounded-2xl border border-blue-100 bg-white p-7 shadow-sm">
          <div className="flex items-center justify-between">
            <h2 className="flex items-center gap-2 text-xl font-bold"><Pencil size={20} /> Edit staff account</h2>
            <button type="button" aria-label="Cancel editing" onClick={() => setEditingUser(null)}><X /></button>
          </div>
          <div className="mt-5 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {['firstName', 'lastName', 'username', 'email', 'staffId'].map((name) => <label key={name} className="text-sm font-semibold">{{ firstName: 'First name', lastName: 'Last name', username: 'Username', email: 'Email', staffId: 'Staff ID' }[name]}<input required name={name} type={name === 'email' ? 'email' : 'text'} value={editingUser[name]} onChange={changeEdit} className="mt-1 w-full rounded-lg border border-slate-200 p-3 font-normal" /></label>)}
            <label className="text-sm font-semibold">Role<select name="role" value={editingUser.role} onChange={changeEdit} className="mt-1 w-full rounded-lg border border-slate-200 p-3 font-normal"><option>Pharmacist</option><option value="InventoryManager">Inventory Manager</option></select></label>
          </div>
          <div className="mt-5 flex gap-3">
            <button disabled={busy} className="gradient-btn rounded-lg px-6 py-3 font-bold text-white disabled:opacity-60">Save changes</button>
            <button type="button" onClick={() => setEditingUser(null)} className="rounded-lg border px-6 py-3 font-semibold">Cancel</button>
          </div>
        </form>}

        <section className="mt-8 overflow-hidden rounded-2xl bg-white shadow-sm">
          <div className="flex items-center justify-between p-6"><h2 className="text-xl font-bold">Staff accounts</h2><span className="rounded-full bg-blue-50 px-4 py-2 font-bold text-medzo-blue">{dashboard.totalUsers} users</span></div>
          <div className="overflow-x-auto"><table className="w-full text-left"><thead className="bg-slate-50 text-sm text-slate-500"><tr><th className="p-4">User ID</th><th className="p-4">Staff ID</th><th className="p-4">Name</th><th className="p-4">Role</th><th className="p-4">Status</th><th className="p-4">Actions</th></tr></thead><tbody>{dashboard.users.map((staff) => { const isAdmin = staff.roles.includes('Admin'); const isDeactivated = !staff.isActive; return <tr key={staff.id} className="border-t border-slate-100"><td className="p-4 font-semibold">{staff.userCode}</td><td className="p-4 font-semibold">{staff.staffId || 'Legacy user'}</td><td className="p-4">{staff.firstName || staff.username} {staff.lastName}</td><td className="p-4">{staff.roles.join(', ')}</td><td className="p-4">{isDeactivated ? <span className="font-semibold text-red-700">Deactivated</span> : <span className="inline-flex items-center gap-1 text-green-700"><CheckCircle2 size={16} /> Active</span>}</td><td className="p-4"><div className="flex gap-2"><button disabled={busy || isAdmin || isDeactivated} onClick={() => startEditing(staff)} className="rounded-lg border px-3 py-2 text-sm font-semibold disabled:cursor-not-allowed disabled:opacity-40">Edit</button><button disabled={busy || staff.id === user.id || isAdmin || isDeactivated} onClick={() => setPendingDeactivation(staff)} className="rounded-lg border border-red-200 px-3 py-2 text-sm font-semibold text-red-700 disabled:cursor-not-allowed disabled:opacity-40">Deactivate</button></div></td></tr> })}</tbody></table></div>
        </section>
      </div>

      {pendingDeactivation && <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/60 p-4" role="dialog" aria-modal="true" aria-labelledby="deactivation-title">
        <div className="w-full max-w-md rounded-2xl bg-white p-7 shadow-2xl">
          <h2 id="deactivation-title" className="text-xl font-bold text-[#0a192f]">Confirm account deactivation</h2>
          <p className="mt-3 text-slate-600">Deactivate <strong>{pendingDeactivation.firstName || pendingDeactivation.username} {pendingDeactivation.lastName}</strong> ({pendingDeactivation.staffId})? They will immediately lose access, their status will remain visible as Deactivated, and this Staff ID can never be approved or used again.</p>
          <div className="mt-6 flex justify-end gap-3">
            <button type="button" disabled={busy} onClick={() => setPendingDeactivation(null)} className="rounded-lg border px-5 py-2.5 font-semibold disabled:opacity-60">Cancel</button>
            <button type="button" disabled={busy} onClick={confirmDeactivation} className="rounded-lg bg-red-700 px-5 py-2.5 font-semibold text-white disabled:opacity-60">Confirm deactivation</button>
          </div>
        </div>
      </div>}
    </main>
  )
}

export default AdminDashboard
