export const validateStockBatch = (v) => {
  const e = {}
  if (!v.medicineId) e.medicineId = 'Select a medicine.'
  if (!v.batchNumber?.trim()) e.batchNumber = 'Batch number is required.'
  else if (v.batchNumber.trim().length > 100) e.batchNumber = 'Batch number cannot exceed 100 characters.'
  if ((v.description?.trim().length || 0) > 500) e.description = 'Description cannot exceed 500 characters.'
  if (Number(v.quantity) <= 0 || !Number.isInteger(Number(v.quantity))) e.quantity = 'Enter a positive whole-number quantity.'
  if (!v.expiryDate || v.expiryDate <= new Date().toISOString().slice(0, 10)) e.expiryDate = 'Expiry date must be in the future.'
  return e
}
