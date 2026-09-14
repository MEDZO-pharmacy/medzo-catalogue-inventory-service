namespace Medzo.CatalogueInventory.Application.Common;
public sealed class NotFoundException(string message):Exception(message);
public sealed class ConflictException(string message):Exception(message);
public sealed class ValidationException(IReadOnlyDictionary<string,string[]> errors):Exception("Validation failed."){public IReadOnlyDictionary<string,string[]> Errors{get;}=errors;}
