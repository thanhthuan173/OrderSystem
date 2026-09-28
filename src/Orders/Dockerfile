FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY src/Contracts/Contracts.csproj src/Contracts/
COPY src/Orders/Orders.csproj src/Orders/

RUN dotnet restore src/Orders/Orders.csproj

COPY src/Contracts/ src/Contracts/
COPY src/Orders/ src/Orders/

RUN dotnet publish src/Orders/Orders.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Orders.dll"]