namespace Medzo.CatalogueInventory.IntegrationTests;
using Xunit;
public sealed class SmokeTests{[Fact]public void ProgramAssembly_IsLoadable()=>Assert.NotNull(typeof(Program).Assembly);}
