FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/api/PayDefteri.Domain/PayDefteri.Domain.csproj src/api/PayDefteri.Domain/
COPY src/api/PayDefteri.Application/PayDefteri.Application.csproj src/api/PayDefteri.Application/
COPY src/api/PayDefteri.Infrastructure/PayDefteri.Infrastructure.csproj src/api/PayDefteri.Infrastructure/
COPY src/api/PayDefteri.Api/PayDefteri.Api.csproj src/api/PayDefteri.Api/

RUN dotnet restore src/api/PayDefteri.Api/PayDefteri.Api.csproj

COPY src/api/ src/api/

RUN dotnet publish src/api/PayDefteri.Api/PayDefteri.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

RUN mkdir -p /app/App_Data/receipts

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "PayDefteri.Api.dll"]
