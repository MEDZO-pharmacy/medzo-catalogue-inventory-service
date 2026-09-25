using Medzo.CatalogueInventory.Domain.Common;
namespace Medzo.CatalogueInventory.Domain.Catalogue;
public sealed class Category : Entity { private Category() {} public Category(string name) => Name = Required(name, nameof(name)); public string Name { get; private set; } = null!; public ICollection<Medicine> Medicines { get; } = new List<Medicine>(); private static string Required(string value,string field)=>string.IsNullOrWhiteSpace(value)?throw new ArgumentException($"{field} is required."):value.Trim(); }
