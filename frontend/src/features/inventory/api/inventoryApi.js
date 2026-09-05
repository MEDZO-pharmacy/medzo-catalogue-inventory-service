import { authenticatedServiceRequest } from '../../../services/authApi'
const base=import.meta.env.VITE_CATALOGUE_INVENTORY_API_URL||'http://localhost:8081'
export const getInventory=({search='',lowStock=false,page=1,pageSize=20}={})=>authenticatedServiceRequest(base,`/api/inventory/items?search=${encodeURIComponent(search)}&lowStock=${lowStock}&page=${page}&pageSize=${pageSize}`)
export const getLowStock=()=>authenticatedServiceRequest(base,'/api/inventory/items/low-stock')
export const recordBatch=data=>authenticatedServiceRequest(base,'/api/inventory/batches',{method:'POST',body:JSON.stringify(data)})
export const getBatches=({medicineId='',page=1,pageSize=20}={})=>authenticatedServiceRequest(base,`/api/inventory/batches?medicineId=${encodeURIComponent(medicineId)}&page=${page}&pageSize=${pageSize}`)
export const getMovements=medicineId=>authenticatedServiceRequest(base,`/api/inventory/items/${medicineId}/movements`)
