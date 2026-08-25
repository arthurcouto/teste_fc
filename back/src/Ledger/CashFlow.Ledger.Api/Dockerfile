FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /source

COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src ./src
RUN dotnet publish src/Ledger/CashFlow.Ledger.Api/CashFlow.Ledger.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app
RUN apk add --no-cache tzdata && adduser -D -u 1001 service
COPY --from=build /app ./
USER service
EXPOSE 8080
ENTRYPOINT ["dotnet", "CashFlow.Ledger.Api.dll"]
