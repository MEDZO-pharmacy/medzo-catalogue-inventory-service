using Medzo.CatalogueInventory.Domain.Catalogue;
using Xunit;

namespace Medzo.CatalogueInventory.UnitTests;

public sealed class MedicineTests
{
    [Fact]
    public void Constructor_StoresNormalizedCatalogueDetailsAndAcceptsZeroPrice()
    {
        var categoryId = Guid.NewGuid();

        var medicine = new Medicine(
            " Paracetamol ",
            " Acetaminophen ",
            " Medzo Labs ",
            0m,
            DosageForm.Tablet,
            categoryId);

        Assert.Equal("Paracetamol", medicine.Name);
        Assert.Equal("PARACETAMOL", medicine.NormalizedName);
        Assert.Equal("Acetaminophen", medicine.GenericName);
        Assert.Equal("Medzo Labs", medicine.Manufacturer);
        Assert.Equal(0m, medicine.UnitPrice);
        Assert.Equal(DosageForm.Tablet, medicine.DosageForm);
        Assert.Equal(categoryId, medicine.CategoryId);
        Assert.True(medicine.IsActive);
    }

    [Fact]
    public void Update_ReplacesEveryEditableValue()
    {
        var medicine = ValidMedicine();
        var categoryId = Guid.NewGuid();

        medicine.Update(
            " Ibuprofen ",
            " Ibuprofen Generic ",
            " Updated Manufacturer ",
            25.50m,
            DosageForm.Capsule,
            categoryId);

        Assert.Equal("Ibuprofen", medicine.Name);
        Assert.Equal("IBUPROFEN", medicine.NormalizedName);
        Assert.Equal("Ibuprofen Generic", medicine.GenericName);
        Assert.Equal("Updated Manufacturer", medicine.Manufacturer);
        Assert.Equal(25.50m, medicine.UnitPrice);
        Assert.Equal(DosageForm.Capsule, medicine.DosageForm);
        Assert.Equal(categoryId, medicine.CategoryId);
    }

    [Fact]
    public void Update_WithNegativePrice_ThrowsForUnitPrice()
    {
        var medicine = ValidMedicine();

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => medicine.Update(
            "Paracetamol", "Acetaminophen", "Medzo Labs", -0.01m, DosageForm.Tablet, null));

        Assert.Equal("unitPrice", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithMissingName_ThrowsClearError(string? value)
    {
        var exception = Assert.Throws<ArgumentException>(() => new Medicine(
            value!, "Generic", "Manufacturer", 1m, DosageForm.Tablet));

        Assert.Equal("Required value is missing.", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithMissingGenericName_ThrowsClearError(string? value)
    {
        var exception = Assert.Throws<ArgumentException>(() => new Medicine(
            "Medicine", value!, "Manufacturer", 1m, DosageForm.Tablet));

        Assert.Equal("Required value is missing.", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithMissingManufacturer_ThrowsClearError(string? value)
    {
        var exception = Assert.Throws<ArgumentException>(() => new Medicine(
            "Medicine", "Generic", value!, 1m, DosageForm.Tablet));

        Assert.Equal("Required value is missing.", exception.Message);
    }

    [Fact]
    public void ActivateAndDeactivate_ChangeAvailabilityState()
    {
        var medicine = ValidMedicine();

        medicine.Deactivate();
        Assert.False(medicine.IsActive);

        medicine.Activate();
        Assert.True(medicine.IsActive);
    }

    private static Medicine ValidMedicine() =>
        new("Paracetamol", "Acetaminophen", "Medzo Labs", 12.50m, DosageForm.Tablet);
}
