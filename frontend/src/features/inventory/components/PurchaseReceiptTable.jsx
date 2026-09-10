export default function PurchaseReceiptTable({ items = [] }) {
  if (!items.length) return <p className="rounded-2xl bg-white p-10 text-center text-[#6b7280]">No purchase stock updates have been processed yet.</p>

  return <div className="overflow-x-auto rounded-2xl bg-white shadow-sm" role="region" aria-label="Automatically processed purchase receipts" tabIndex="0">
    <table className="min-w-[760px] w-full text-left">
      <thead className="bg-slate-50 text-sm text-slate-500">
        <tr>{['Processed', 'Purchase reference', 'Medicine', 'Batch', 'Expiry', 'Received', 'New balance'].map(label => <th scope="col" className="p-4" key={label}>{label}</th>)}</tr>
      </thead>
      <tbody>{items.map(item => <tr className="border-t border-slate-100" key={item.movementId}>
        <td className="p-4">{new Date(item.processedAtUtc).toLocaleString()}</td>
        <td className="p-4 font-semibold text-medzo-blue">{item.purchaseReference}</td>
        <td className="p-4 font-bold text-[#0a192f]">{item.medicineName}</td>
        <td className="p-4">{item.batchNumber}</td>
        <td className="p-4">{item.expiryDate}</td>
        <td className="p-4 font-semibold text-green-700">+{item.quantityReceived}</td>
        <td className="p-4">{item.quantityAfter}</td>
      </tr>)}</tbody>
    </table>
  </div>
}
