import { describe, expect, it } from 'vitest'
import { validateStockBatch } from './stockBatchValidation'

const valid = { medicineId: 'medicine-1', batchNumber: 'LOT-001', description: 'Supplier invoice 10', expiryDate: '2099-01-01', quantity: '10' }
describe('stock batch validation', () => {
  it('accepts a traceable valid batch', () => expect(validateStockBatch(valid)).toEqual({}))
  it('requires a batch number but allows an omitted description', () => expect(validateStockBatch({ ...valid, batchNumber: '', description: '' })).toEqual({ batchNumber: expect.any(String) }))
  it('rejects expired and non-positive stock', () => expect(validateStockBatch({ ...valid, expiryDate: '2020-01-01', quantity: '0' })).toMatchObject({ expiryDate: expect.any(String), quantity: expect.any(String) }))
})
