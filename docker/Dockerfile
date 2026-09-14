FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/Medzo.CatalogueInventory.Api/Medzo.CatalogueInventory.Api.csproj
RUN dotnet publish src/Medzo.CatalogueInventory.Api/Medzo.CatalogueInventory.Api.csproj -c Release -o /app/publish /p:UseAppHost=false
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet","Medzo.CatalogueInventory.Api.dll"]
