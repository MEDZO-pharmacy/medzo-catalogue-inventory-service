import { afterEach, describe, expect, it, vi } from 'vitest'
import { cleanup, render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import App from '../App'

const session = {
  token: 'access-token',
  expiresAt: new Date(Date.now() + 3_600_000).toISOString(),
  user: {
    id: 'b4de84fc-3907-4f36-9803-c82912d9cb72',
    username: 'medzo.user',
    staffId: 'P1001',
    email: 'user@example.com',
    firstName: 'Medzo',
    lastName: 'User',
    roles: ['Pharmacist'],
  },
}

const jsonResponse = (body, status = 200) => new Response(JSON.stringify(body), {
  status,
  headers: { 'Content-Type': 'application/json' },
})

afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
  window.history.replaceState({}, '', '/')
})

describe('authentication UI', () => {
  it('redirects a signed-out user away from a protected route', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(
      jsonResponse({ message: 'Authentication session is unavailable.' }, 401),
    ))
    window.history.replaceState({}, '', '/pharmacist')

    render(<App />)

    expect(await screen.findByRole('heading', { name: 'Welcome' })).toBeInTheDocument()
    expect(window.location.pathname).toBe('/login')
  })

  it('allows a signed-out guest to view the product catalogue', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(
      jsonResponse({ message: 'Authentication session is unavailable.' }, 401),
    ))
    window.history.replaceState({}, '', '/products')

    render(<App />)

    expect(await screen.findByRole('heading', { name: 'Products Page' })).toBeInTheDocument()
    expect(window.location.pathname).toBe('/products')
  })

  it.each([
    ['Pharmacist', '/pharmacist', 'Pharmacist Dashboard'],
    ['InventoryManager', '/inventory', 'Inventory Manager Dashboard'],
  ])('shows a back-to-home arrow on the %s dashboard', async (role, path, heading) => {
    const roleSession = { ...session, user: { ...session.user, roles: [role] } }
    vi.stubGlobal('fetch', vi.fn(async (url) => {
      if (url.endsWith('/auth/refresh')) return jsonResponse(roleSession)
      if (url.endsWith(`/dashboard/${path.slice(1)}`)) return jsonResponse({ modules: [] })
      throw new Error(`Unexpected request: ${url}`)
    }))
    window.history.replaceState({}, '', path)

    render(<App />)

    expect(await screen.findByRole('heading', { name: heading })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Back to home' })).toHaveAttribute('href', '/')
    expect(screen.getByText('Signed in as')).toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: 'Available modules' })).not.toBeInTheDocument()
  })

  it('restores the cookie session, updates the header, and logs out', async () => {
    const fetchMock = vi.fn(async (url) => {
      if (url.endsWith('/auth/refresh')) return jsonResponse(session)
      if (url.endsWith('/auth/revoke')) return new Response(null, { status: 204 })
      if (url.endsWith('/reviews')) return jsonResponse([])
      throw new Error(`Unexpected request: ${url}`)
    })
    vi.stubGlobal('fetch', fetchMock)

    render(<App />)

    expect(await screen.findByText('Welcome back! medzo.user')).toBeInTheDocument()
    expect(screen.queryByRole('link', { name: 'Login' })).not.toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: 'Logout' }))

    await waitFor(() => expect(screen.getByRole('link', { name: 'Login' })).toBeInTheDocument())
    expect(fetchMock).toHaveBeenCalledWith(
      expect.stringContaining('/auth/revoke'),
      expect.objectContaining({ method: 'POST', credentials: 'include' }),
    )
    expect(localStorage.getItem('medzo.auth')).toBeNull()
  })

  it('opens Login from the homepage and redirects an Admin to the dashboard', async () => {
    const adminSession = {
      ...session,
      user: { ...session.user, staffId: 'A1001', roles: ['Admin'] },
    }
    const fetchMock = vi.fn(async (url, options) => {
      if (url.endsWith('/auth/refresh')) return jsonResponse({ message: 'No session' }, 401)
      if (url.endsWith('/reviews')) return jsonResponse([])
      if (url.endsWith('/auth/login')) {
        expect(JSON.parse(options.body)).toEqual({ identifier: 'A1001', password: 'Strong1!' })
        return jsonResponse(adminSession)
      }
      if (url.endsWith('/dashboard/admin')) return jsonResponse({ modules: [], users: [], totalUsers: 0 })
      if (url.endsWith('/users/staff-invitations')) return jsonResponse([])
      throw new Error(`Unexpected request: ${url}`)
    })
    vi.stubGlobal('fetch', fetchMock)
    window.history.replaceState({}, '', '/')
    render(<App />)

    await userEvent.click(await screen.findByRole('link', { name: 'Login' }))
    await screen.findByRole('heading', { name: 'Welcome' })
    await userEvent.type(screen.getByLabelText('Staff ID, Username, or Email'), 'A1001')
    await userEvent.type(screen.getByLabelText('Password'), 'Strong1!')
    await userEvent.click(screen.getByRole('button', { name: 'Login to Account' }))

    expect(await screen.findByRole('heading', { name: 'Administrator Dashboard' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Back to home' })).toHaveAttribute('href', '/')
    expect(screen.getByText('Signed in as')).toBeInTheDocument()
    expect(window.location.pathname).toBe('/admin')
  })

  it('lets an Admin create a staff account with an explicit matching role', async () => {
    const adminSession = {
      ...session,
      user: { ...session.user, staffId: 'A1001', roles: ['Admin'] },
    }
    let createdBody = null
    const fetchMock = vi.fn(async (url, options = {}) => {
      if (url.endsWith('/auth/refresh')) return jsonResponse(adminSession)
      if (url.endsWith('/dashboard/admin')) return jsonResponse({ modules: [], users: [], totalUsers: 0 })
      if (url.endsWith('/users/staff-invitations')) return jsonResponse([])
      if (url.endsWith('/users') && options.method === 'POST') {
        createdBody = JSON.parse(options.body)
        return jsonResponse({ user: { id: 'new-user' } }, 201)
      }
      throw new Error(`Unexpected request: ${url}`)
    })
    vi.stubGlobal('fetch', fetchMock)
    window.history.replaceState({}, '', '/admin')
    render(<App />)

    await screen.findByRole('heading', { name: 'Administrator Dashboard' })
    expect(screen.queryByRole('option', { name: 'Admin' })).not.toBeInTheDocument()
    await userEvent.type(screen.getByLabelText('First name'), 'New')
    await userEvent.type(screen.getByLabelText('Last name'), 'Pharmacist')
    await userEvent.type(screen.getByLabelText('Username'), 'new.pharmacist')
    await userEvent.type(screen.getByLabelText('Email'), 'new@example.com')
    await userEvent.type(screen.getByLabelText('Staff ID (P prefix)'), 'P2001')
    await userEvent.type(screen.getByLabelText('Temporary password'), 'Strong1!')
    await userEvent.type(screen.getByLabelText('Confirm password'), 'Strong1!')
    await userEvent.click(screen.getByRole('button', { name: 'Create account' }))

    expect(await screen.findByText('Staff account created successfully.')).toBeInTheDocument()
    expect(createdBody).toEqual(expect.objectContaining({ staffId: 'P2001', role: 'Pharmacist' }))
  })

  it('shows the database-assigned formatted user ID on the Admin dashboard', async () => {
    const adminSession = {
      ...session,
      user: { ...session.user, staffId: 'A1001', roles: ['Admin'] },
    }
    const managedUser = {
      id: 'managed-user', userNumber: 1, userCode: '001', staffId: 'P2001',
      firstName: 'New', lastName: 'Pharmacist', roles: ['Pharmacist'], isActive: true,
    }
    const fetchMock = vi.fn(async (url) => {
      if (url.endsWith('/auth/refresh')) return jsonResponse(adminSession)
      if (url.endsWith('/dashboard/admin')) {
        return jsonResponse({ modules: [], users: [managedUser], totalUsers: 1 })
      }
      if (url.endsWith('/users/staff-invitations')) return jsonResponse([])
      throw new Error(`Unexpected request: ${url}`)
    })
    vi.stubGlobal('fetch', fetchMock)
    window.history.replaceState({}, '', '/admin')

    render(<App />)

    expect(await screen.findByText('001')).toBeInTheDocument()
    expect(screen.getByText('P2001')).toBeInTheDocument()
  })

  it('lets an Admin edit a staff role and deactivate the account', async () => {
    const adminSession = {
      ...session,
      user: { ...session.user, id: 'admin-user', staffId: 'A1234', roles: ['Admin'] },
    }
    let managedUser = {
      id: 'managed-user', userNumber: 2, userCode: '002', username: 'pharmacy.user',
      staffId: 'P2001', email: 'pharmacy@example.com', firstName: 'Pharmacy',
      lastName: 'User', roles: ['Pharmacist'], isActive: true,
    }
    let invitations = []
    const fetchMock = vi.fn(async (url, options = {}) => {
      if (url.endsWith('/auth/refresh')) return jsonResponse(adminSession)
      if (url.endsWith('/dashboard/admin')) {
        return jsonResponse({ modules: [], users: [managedUser], totalUsers: 1 })
      }
      if (url.endsWith('/users/staff-invitations')) return jsonResponse(invitations)
      if (url.endsWith('/users/managed-user/managed') && options.method === 'PUT') {
        const body = JSON.parse(options.body)
        managedUser = { ...managedUser, ...body, roles: [body.role] }
        return jsonResponse(managedUser)
      }
      if (url.endsWith('/users/managed-user/status') && options.method === 'PATCH') {
        managedUser = { ...managedUser, isActive: JSON.parse(options.body).isActive }
        invitations = [{
          id: 'reserved-managed-user', staffId: managedUser.staffId,
          role: managedUser.roles[0], isClaimed: true,
        }]
        return jsonResponse(managedUser)
      }
      throw new Error(`Unexpected request: ${url}`)
    })
    vi.stubGlobal('fetch', fetchMock)
    window.history.replaceState({}, '', '/admin')
    render(<App />)

    await screen.findByText('P2001')
    await userEvent.click(screen.getByRole('button', { name: 'Edit' }))
    const editForm = screen.getByRole('heading', { name: 'Edit staff account' }).closest('form')
    await userEvent.selectOptions(within(editForm).getByLabelText('Role'), 'InventoryManager')
    expect(within(editForm).getByLabelText('Staff ID')).toHaveValue('I2001')
    await userEvent.click(within(editForm).getByRole('button', { name: 'Save changes' }))

    expect(await screen.findByText('Staff account updated successfully.')).toBeInTheDocument()
    expect(screen.getByText('InventoryManager')).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: 'Deactivate' }))
    const dialog = screen.getByRole('dialog', { name: 'Confirm account deactivation' })
    expect(within(dialog).getByText(/I2001/)).toBeInTheDocument()

    await userEvent.click(within(dialog).getByRole('button', { name: 'Cancel' }))
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    expect(screen.getByText('I2001')).toBeInTheDocument()
    expect(fetchMock.mock.calls.filter(([url]) => url.endsWith('/users/managed-user/status'))).toHaveLength(0)

    await userEvent.click(screen.getByRole('button', { name: 'Deactivate' }))
    await userEvent.click(within(screen.getByRole('dialog')).getByRole('button', { name: 'Confirm deactivation' }))

    expect(await screen.findByText('Staff account successfully deactivated.')).toBeInTheDocument()
    expect(screen.getAllByText('I2001')).not.toHaveLength(0)
    expect(screen.getByText('Deactivated')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Edit' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Deactivate' })).toBeDisabled()
    expect(screen.getByText('Claimed')).toBeInTheDocument()
    const statusRequest = fetchMock.mock.calls.find(([url]) => url.endsWith('/users/managed-user/status'))
    expect(JSON.parse(statusRequest[1].body)).toEqual({ isActive: false })
  })
})
