import { cleanup, fireEvent, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import StockBatchForm from './StockBatchForm'

afterEach(cleanup)

const medicine = { id: '6ef0069b-bfeb-4a19-914f-40cefcfe734f', name: 'Paracetamol' }

function futureDate() {
  const value = new Date()
  value.setDate(value.getDate() + 30)
  return value.toISOString().slice(0, 10)
}

describe('StockBatchForm', () => {
  it('submits a valid traceable batch without requiring a description', async () => {
    const user = userEvent.setup()
    const onSubmit = vi.fn().mockResolvedValue(true)
    render(<StockBatchForm medicines={[medicine]} onSubmit={onSubmit} busy={false} />)

    await user.selectOptions(screen.getByLabelText('Medicine'), medicine.id)
    await user.type(screen.getByLabelText('Batch number'), '  LOT-100  ')
    fireEvent.change(screen.getByLabelText('Expiry date'), { target: { value: futureDate() } })
    await user.type(screen.getByLabelText('Quantity'), '20')
    await user.type(screen.getByLabelText(/Source reference/), 'INV-100')
    await user.click(screen.getByRole('button', { name: 'Record stock batch' }))

    expect(onSubmit).toHaveBeenCalledOnce()
    expect(onSubmit).toHaveBeenCalledWith(expect.objectContaining({
      medicineId: medicine.id,
      batchNumber: 'LOT-100',
      description: null,
      quantity: 20,
      sourceReference: 'INV-100',
    }))
  })

  it('shows a clear error and does not submit an expired batch', async () => {
    const user = userEvent.setup()
    const onSubmit = vi.fn()
    render(<StockBatchForm medicines={[medicine]} onSubmit={onSubmit} busy={false} />)

    await user.selectOptions(screen.getByLabelText('Medicine'), medicine.id)
    await user.type(screen.getByLabelText('Batch number'), 'LOT-OLD')
    fireEvent.change(screen.getByLabelText('Expiry date'), { target: { value: '2020-01-01' } })
    await user.type(screen.getByLabelText('Quantity'), '5')
    await user.click(screen.getByRole('button', { name: 'Record stock batch' }))

    expect(onSubmit).not.toHaveBeenCalled()
    expect(screen.getByRole('alert')).toHaveTextContent(/future/i)
  })
})
