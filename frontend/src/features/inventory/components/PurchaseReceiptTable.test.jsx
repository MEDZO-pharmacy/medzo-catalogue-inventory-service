import { cleanup, render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it } from 'vitest'
import PurchaseReceiptTable from './PurchaseReceiptTable'

afterEach(cleanup)

describe('PurchaseReceiptTable', () => {
  it('shows an automatically applied purchase update', () => {
    render(<PurchaseReceiptTable items={[{
      movementId: 'movement-1', purchaseReference: 'PUR-100', medicineName: 'Amoxicillin',
      batchNumber: 'LOT-100', expiryDate: '2027-03-01', quantityReceived: 25,
      quantityAfter: 40, processedAtUtc: '2026-09-08T10:00:00Z',
    }]} />)

    expect(screen.getByText('PUR-100')).toBeInTheDocument()
    expect(screen.getByText('Amoxicillin')).toBeInTheDocument()
    expect(screen.getByText('+25')).toBeInTheDocument()
    expect(screen.getByText('40')).toBeInTheDocument()
  })

  it('shows an empty state before a purchase event is processed', () => {
    render(<PurchaseReceiptTable />)
    expect(screen.getByText(/No purchase stock updates/i)).toBeInTheDocument()
  })
})
