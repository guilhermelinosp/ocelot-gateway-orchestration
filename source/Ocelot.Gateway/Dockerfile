# Base stage for runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS stage
WORKDIR /app

# Build stage for building the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# Copy only the project file and restore dependencies
COPY ["/source/Ocelot.Gateway/*.csproj", "/"]
RUN dotnet restore "*.csproj"

# Copy the entire project and build
COPY . .
WORKDIR "/source/Ocelot.Gateway"
RUN dotnet build "*.csproj" -c Release -o /app/build

# Publish stage to publish the application
FROM build AS publish
RUN dotnet publish "*.csproj" -c Release -o /app/publish

# Final stage with only necessary files for runtime
FROM stage AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Ocelot.Gateway.dll"]
