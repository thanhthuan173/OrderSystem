FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY src/Contracts/Contracts.csproj src/Contracts/
COPY src/Payments/Payments.csproj src/Payments/

RUN dotnet restore src/Payments/Payments.csproj

COPY src/Contracts/ src/Contracts/
COPY src/Payments/ src/Payments/

RUN dotnet publish src/Payments/Payments.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Payments.dll"]