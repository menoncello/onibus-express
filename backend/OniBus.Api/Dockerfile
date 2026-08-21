FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

COPY OniBus.Domain/OniBus.Domain.csproj OniBus.Domain/
COPY OniBus.Api/OniBus.Api.csproj OniBus.Api/
RUN dotnet restore OniBus.Api/OniBus.Api.csproj

COPY OniBus.Domain/ OniBus.Domain/
COPY OniBus.Api/ OniBus.Api/
RUN dotnet publish OniBus.Api/OniBus.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
# Npgsql tenta carregar libgssapi_krb5 para autenticação GSS; sem ela, funciona mas loga um
# aviso de biblioteca ausente em todo boot.
RUN apk add --no-cache krb5-libs
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "OniBus.Api.dll"]
