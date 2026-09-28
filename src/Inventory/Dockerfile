FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY src/Contracts/Contracts.csproj src/Contracts/
COPY src/Inventory/Inventory.csproj src/Inventory/

RUN dotnet restore src/Inventory/Inventory.csproj

COPY src/Contracts/ src/Contracts/
COPY src/Inventory/ src/Inventory/

RUN dotnet publish src/Inventory/Inventory.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Inventory.dll"]