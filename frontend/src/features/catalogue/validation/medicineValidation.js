export const validateMedicine = (values) => {
  const errors = {}
  if (!values.name?.trim()) errors.name = 'Medicine name is required.'
  if (!values.genericName?.trim()) errors.genericName = 'Generic name is required.'
  if (!values.manufacturer?.trim()) errors.manufacturer = 'Manufacturer is required.'
  if (Number(values.unitPrice) < 0) errors.unitPrice = 'Unit price cannot be negative.'
  if (Number(values.reorderThreshold) < 0) errors.reorderThreshold = 'Reorder threshold cannot be negative.'
  return errors
}
