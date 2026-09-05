using Medzo.CatalogueInventory.Domain.Common;
namespace Medzo.CatalogueInventory.Domain.Inventory;
public sealed class InventoryItem : Entity {
 private InventoryItem() {}
 public InventoryItem(Guid medicineId,int reorderThreshold=0){MedicineId=medicineId;SetReorderThreshold(reorderThreshold);}
 public Guid MedicineId{get;private set;} public int QuantityOnHand{get;private set;} public int ReorderThreshold{get;private set;} public bool IsLowStock=>QuantityOnHand<ReorderThreshold; public long Version{get;private set;} public ICollection<StockBatch> Batches{get;}=new List<StockBatch>(); public ICollection<StockMovement> Movements{get;}=new List<StockMovement>();
 public void SetReorderThreshold(int value){if(value<0)throw new ArgumentOutOfRangeException(nameof(value));if(ReorderThreshold!=value){ReorderThreshold=value;Version++;}}
 public StockMovement ChangeStock(int delta,StockMovementType type,string sourceType,string sourceId,Guid? batchId=null){if(delta==0)throw new ArgumentException("Quantity change cannot be zero.");if(QuantityOnHand+delta<0)throw new InvalidOperationException("Insufficient stock.");var before=QuantityOnHand;QuantityOnHand+=delta;Version++;var movement=new StockMovement(Id,batchId,type,delta,before,QuantityOnHand,sourceType,sourceId);Movements.Add(movement);return movement;}
}

