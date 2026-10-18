# Catalogue and Inventory: unit testing and mocking

## Additional business-rule tests

`InventoryBoundaryTests.cs` adds 10 cases covering the low-stock boundary, rejected stock changes, version consistency, invalid batch consumption, and receipt after partial consumption. `CatalogueValidationMockTests.cs` adds 6 cases that use Moq to execute the transaction callback and verify invalid medicines never query or save through the store. No database or broker is needed.

## Run locally

Run from this repository:

```powershell
dotnet test tests/Medzo.CatalogueInventory.UnitTests/Medzo.CatalogueInventory.UnitTests.csproj --collect:"XPlat Code Coverage"
```

Latest local validation: **26 passing cases**, including the pre-existing tests. Each parameterized input counts as one case.

This expansion adds the Application project to the unit-test references. Coverage now includes application code as well as domain code, so the overall percentage is not directly comparable with the earlier domain-only report.

## Live demonstration

Push to `feature/se3112-unit-mocking`, open this repository's **Actions** tab, and open the newest unit-test workflow run. Expand the test step for console output, then show the **Summary** table for total/passed/failed/skipped counts.

The workflow uploads TRX test results and Coverlet Cobertura XML in its test-evidence artifact. Coverage measures executed lines/branches within the assemblies included by the collector; it is not the same as the percentage of passing tests. Compare reports only when their included assemblies and filters match.

