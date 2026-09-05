using Medzo.CatalogueInventory.Domain.Inventory;
using Xunit;
namespace Medzo.CatalogueInventory.UnitTests;
public sealed class InventoryItemTests{
 [Fact]public void ChangeStock_IncreasesBalanceAndCreatesAuditMovement(){var item=new InventoryItem(Guid.NewGuid(),5);var movement=item.ChangeStock(10,StockMovementType.BatchReceived,"test","1");Assert.Equal(10,item.QuantityOnHand);Assert.Equal(0,movement.QuantityBefore);Assert.Equal(10,movement.QuantityAfter);Assert.False(item.IsLowStock);}
 [Fact]public void ChangeStock_RejectsNegativeBalance(){var item=new InventoryItem(Guid.NewGuid());Assert.Throws<InvalidOperationException>(()=>item.ChangeStock(-1,StockMovementType.SaleDispensed,"test","1"));}
 [Fact]public void LowStock_UsesStrictlyBelowThreshold(){var item=new InventoryItem(Guid.NewGuid(),5);item.ChangeStock(5,StockMovementType.BatchReceived,"test","1");Assert.False(item.IsLowStock);item.ChangeStock(-1,StockMovementType.SaleDispensed,"test","2");Assert.True(item.IsLowStock);}}
