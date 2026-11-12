FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["sitraCalco.Inventory/Inventory.Api/Inventory.Api.csproj", "sitraCalco.Inventory/Inventory.Api/"]
COPY ["sitraCalco.Inventory/Inventory.Application/Inventory.Application.csproj", "sitraCalco.Inventory/Inventory.Application/"]
COPY ["sitraCalco.Inventory/Inventory.Domain/Inventory.Domain.csproj", "sitraCalco.Inventory/Inventory.Domain/"]
COPY ["sitraCalco.Inventory/Inventory.Infrastructure.Persistance/Inventory.Infrastructure.Persistance.csproj", "sitraCalco.Inventory/Inventory.Infrastructure.Persistance/"]
RUN dotnet restore "sitraCalco.Inventory/Inventory.Api/Inventory.Api.csproj"

COPY . .
RUN dotnet publish "sitraCalco.Inventory/Inventory.Api/Inventory.Api.csproj" \
    -c Release -o /out --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /out/ .
USER $APP_UID
ENTRYPOINT ["dotnet", "Inventory.Api.dll"]