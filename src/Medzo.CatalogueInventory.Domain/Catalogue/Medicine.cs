using Medzo.CatalogueInventory.Domain.Common;
using Medzo.CatalogueInventory.Domain.Inventory;
namespace Medzo.CatalogueInventory.Domain.Catalogue;
public sealed class Medicine : Entity {
 private Medicine() {}
 public Medicine(string name,string genericName,string manufacturer,decimal unitPrice,DosageForm dosageForm,Guid? categoryId=null){Update(name,genericName,manufacturer,unitPrice,dosageForm,categoryId);}
 public string Name {get;private set;}=null!; public string NormalizedName {get;private set;}=null!; public string GenericName {get;private set;}=null!; public string Manufacturer {get;private set;}=null!; public decimal UnitPrice {get;private set;} public DosageForm DosageForm {get;private set;} public Guid? CategoryId {get;private set;} public Category? Category {get;private set;} public bool IsActive {get;private set;}=true; public InventoryItem? InventoryItem {get;private set;}
 public void Update(string name,string genericName,string manufacturer,decimal unitPrice,DosageForm dosageForm,Guid? categoryId){if(unitPrice<0)throw new ArgumentOutOfRangeException(nameof(unitPrice));Name=Req(name);NormalizedName=Name.ToUpperInvariant();GenericName=Req(genericName);Manufacturer=Req(manufacturer);UnitPrice=unitPrice;DosageForm=dosageForm;CategoryId=categoryId;}
 public void Deactivate()=>IsActive=false; private static string Req(string value)=>string.IsNullOrWhiteSpace(value)?throw new ArgumentException("Required value is missing."):value.Trim(); }
