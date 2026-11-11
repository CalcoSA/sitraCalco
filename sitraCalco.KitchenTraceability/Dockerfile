FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["sitraCalco.KitchenTraceability/KitchenTraceability.Api/KitchenTraceability.Api.csproj", "sitraCalco.KitchenTraceability/KitchenTraceability.Api/"]
COPY ["sitraCalco.KitchenTraceability/KitchenTraceability.Application/KitchenTraceability.Application.csproj", "sitraCalco.KitchenTraceability/KitchenTraceability.Application/"]
COPY ["sitraCalco.KitchenTraceability/KitchenTraceability.Domain/KitchenTraceability.Domain.csproj", "sitraCalco.KitchenTraceability/KitchenTraceability.Domain/"]
COPY ["sitraCalco.KitchenTraceability/KitchenTraceability.Infrastructure.Persistance/KitchenTraceability.Infrastructure.Persistance.csproj", "sitraCalco.KitchenTraceability/KitchenTraceability.Infrastructure.Persistance/"]
RUN dotnet restore "sitraCalco.KitchenTraceability/KitchenTraceability.Api/KitchenTraceability.Api.csproj"

COPY . .
RUN dotnet publish "sitraCalco.KitchenTraceability/KitchenTraceability.Api/KitchenTraceability.Api.csproj" \
    -c Release -o /out --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /out/ .
USER $APP_UID
ENTRYPOINT ["dotnet", "KitchenTraceability.Api.dll"]