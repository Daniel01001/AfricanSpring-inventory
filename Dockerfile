# ---- build ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first (layer-cached unless the csproj changes)
COPY *.csproj ./
RUN dotnet restore

# Build & publish
COPY . .
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# ---- runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish ./

ENV ASPNETCORE_ENVIRONMENT=Production
# App binds to $PORT (set by Render) at runtime; 8080 is just the documented default.
EXPOSE 8080
ENTRYPOINT ["dotnet", "AfricanSpringInventory.dll"]
