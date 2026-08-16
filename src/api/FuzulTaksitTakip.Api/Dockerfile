FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/api/FuzulTaksitTakip.Domain/FuzulTaksitTakip.Domain.csproj src/api/FuzulTaksitTakip.Domain/
COPY src/api/FuzulTaksitTakip.Application/FuzulTaksitTakip.Application.csproj src/api/FuzulTaksitTakip.Application/
COPY src/api/FuzulTaksitTakip.Infrastructure/FuzulTaksitTakip.Infrastructure.csproj src/api/FuzulTaksitTakip.Infrastructure/
COPY src/api/FuzulTaksitTakip.Api/FuzulTaksitTakip.Api.csproj src/api/FuzulTaksitTakip.Api/

RUN dotnet restore src/api/FuzulTaksitTakip.Api/FuzulTaksitTakip.Api.csproj

COPY src/api/ src/api/

RUN dotnet publish src/api/FuzulTaksitTakip.Api/FuzulTaksitTakip.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

RUN mkdir -p /app/App_Data/receipts

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "FuzulTaksitTakip.Api.dll"]
