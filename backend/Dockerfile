# Multi-stage build for Asisya Catalog API (.NET 8)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Asisya.Catalog.sln ./
COPY src/Asisya.Domain/Asisya.Domain.csproj src/Asisya.Domain/
COPY src/Asisya.Application/Asisya.Application.csproj src/Asisya.Application/
COPY src/Asisya.Infrastructure/Asisya.Infrastructure.csproj src/Asisya.Infrastructure/
COPY src/Asisya.Api/Asisya.Api.csproj src/Asisya.Api/

RUN dotnet restore src/Asisya.Api/Asisya.Api.csproj

COPY src/ ./src/
RUN dotnet publish src/Asisya.Api/Asisya.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Asisya.Api.dll"]
