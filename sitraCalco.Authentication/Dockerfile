FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["sitraCalco.Authentication/Authentication.Api/Authentication.Api.csproj", "sitraCalco.Authentication/Authentication.Api/"]
COPY ["sitraCalco.Authentication/Authentication.Application/Authentication.Application.csproj", "sitraCalco.Authentication/Authentication.Application/"]
COPY ["sitraCalco.Authentication/Authentication.Domain/Authentication.Domain.csproj", "sitraCalco.Authentication/Authentication.Domain/"]
COPY ["sitraCalco.Authentication/Authentication.Infrastructure.Persistance/Authentication.Infrastructure.Persistance.csproj", "sitraCalco.Authentication/Authentication.Infrastructure.Persistance/"]
RUN dotnet restore "sitraCalco.Authentication/Authentication.Api/Authentication.Api.csproj"

COPY . .
RUN dotnet publish "sitraCalco.Authentication/Authentication.Api/Authentication.Api.csproj" \
    -c Release -o /out --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /out/ .
USER $APP_UID
ENTRYPOINT ["dotnet", "Authentication.Api.dll"]