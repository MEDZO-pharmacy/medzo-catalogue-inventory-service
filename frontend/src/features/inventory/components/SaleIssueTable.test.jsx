import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import SaleIssueTable from './SaleIssueTable'

describe('SaleIssueTable', () => {
  it('shows an automatically applied sale update', () => {
    render(<SaleIssueTable items={[{
      movementId: 'movement-1', saleReference: 'SALE-100', medicineName: 'Paracetamol',
      batchNumber: 'LOT-100', expiryDate: '2027-01-01', quantitySold: 4,
      quantityAfter: 16, processedAtUtc: '2026-09-10T08:00:00Z',
    }]} />)

    expect(screen.getByText('SALE-100')).toBeInTheDocument()
    expect(screen.getByText('Paracetamol')).toBeInTheDocument()
    expect(screen.getByText('−4')).toBeInTheDocument()
    expect(screen.getByText('16')).toBeInTheDocument()
  })

  it('shows an empty state before a sale event is processed', () => {
    render(<SaleIssueTable />)
    expect(screen.getByText(/No sale stock updates/i)).toBeInTheDocument()
  })
})
