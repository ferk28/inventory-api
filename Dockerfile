# Build stage: restore and publish the API with the full SDK.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Inventory/Inventory.Domain/Inventory.Domain.csproj Inventory/Inventory.Domain/
COPY Inventory/Inventory.Application/Inventory.Application.csproj Inventory/Inventory.Application/
COPY Inventory/Inventory.Infrastructure/Inventory.Infrastructure.csproj Inventory/Inventory.Infrastructure/
COPY Inventory/Inventory.Api/Inventory.Api.csproj Inventory/Inventory.Api/
RUN dotnet restore Inventory/Inventory.Api/Inventory.Api.csproj
COPY Inventory/Inventory.Domain/ Inventory/Inventory.Domain/
COPY Inventory/Inventory.Application/ Inventory/Inventory.Application/
COPY Inventory/Inventory.Infrastructure/ Inventory/Inventory.Infrastructure/
COPY Inventory/Inventory.Api/ Inventory/Inventory.Api/
COPY .editorconfig ./
RUN dotnet publish Inventory/Inventory.Api/Inventory.Api.csproj -c Release -o /app/publish --no-restore

# Runtime stage: only the published output and the ASP.NET runtime.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
# SPEC section 11: the container must not run as root; the base image ships user "app".
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Inventory.Api.dll"]
