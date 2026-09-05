export const getPrimaryRole = (user) => {
  const roles = user?.roles || []
  if (roles.includes('Admin')) return 'Admin'
  if (roles.includes('Pharmacist')) return 'Pharmacist'
  if (roles.includes('InventoryManager')) return 'InventoryManager'
  return null
}

export const getRoleHome = (user) => {
  const role = getPrimaryRole(user)
  if (role === 'Admin') return '/admin'
  if (role === 'Pharmacist') return '/pharmacist'
  if (role === 'InventoryManager') return '/inventory'
  return '/'
}

