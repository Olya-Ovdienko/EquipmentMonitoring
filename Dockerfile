
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy all project files
COPY . .

# Restore dependencies
RUN dotnet restore "EquipmentMonitoring.API/EquipmentMonitoring.API.csproj"

# Publish the API
RUN dotnet publish "EquipmentMonitoring.API/EquipmentMonitoring.API.csproj" \
    -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "EquipmentMonitoring.API.dll"]
