using Medzo.CatalogueInventory.Application.Catalogue;
using Medzo.CatalogueInventory.Application.Common;
using Medzo.CatalogueInventory.Domain.Catalogue;
using Moq;
using Xunit;

namespace Medzo.CatalogueInventory.UnitTests;

public sealed class CatalogueValidationMockTests
{
    [Theory]
    [InlineData("name")]
    [InlineData("genericName")]
    [InlineData("manufacturer")]
    [InlineData("unitPrice")]
    [InlineData("dosageForm")]
    [InlineData("reorderThreshold")]
    public async Task Invalid_medicine_is_rejected_before_querying_or_saving(string field)
    {
        var request = new MedicineRequest("Paracetamol", "Acetaminophen", "Medzo", 10m, DosageForm.Tablet, null, 5);
        request = field switch
        {
            "name" => request with { Name = " " },
            "genericName" => request with { GenericName = "" },
            "manufacturer" => request with { Manufacturer = "" },
            "unitPrice" => request with { UnitPrice = -1m },
            "dosageForm" => request with { DosageForm = (DosageForm)999 },
            _ => request with { ReorderThreshold = -1 }
        };
        var store = new Mock<ICatalogueInventoryStore>(MockBehavior.Strict);
        store.Setup(s => s.ExecuteTransactionAsync(It.IsAny<Func<CancellationToken, Task<MedicineResponse>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task<MedicineResponse>> action, CancellationToken ct) => action(ct));

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            new CatalogueService(store.Object).CreateAsync(request, CancellationToken.None));

        Assert.Contains(field, exception.Errors.Keys);
        store.Verify(s => s.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        store.VerifyGet(s => s.Medicines, Times.Never);
        store.Verify(s => s.ExecuteTransactionAsync(It.IsAny<Func<CancellationToken, Task<MedicineResponse>>>(), It.IsAny<CancellationToken>()), Times.Once);
        store.VerifyNoOtherCalls();
    }
}
