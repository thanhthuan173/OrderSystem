FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build

WORKDIR /source

RUN apt-get update \
    && apt-get install -y python3 \
    && dotnet workload install wasm-tools \
    && rm -rf /var/lib/apt/lists/*

COPY client/OrderFlow.Client.csproj client/
COPY src/Contracts/Contracts.csproj src/Contracts/

RUN dotnet restore client/OrderFlow.Client.csproj

COPY client/ client/
COPY src/Contracts/ src/Contracts/

RUN dotnet publish client/OrderFlow.Client.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM nginx:1.27-alpine

COPY --from=build /app/publish/wwwroot/ /usr/share/nginx/html/

COPY client/nginx.conf /etc/nginx/nginx.conf

EXPOSE 80