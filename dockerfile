FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 443
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore Ocelot.Gateway/*.csproj
COPY . .
WORKDIR "/src/Ocelot.Gateway"
RUN dotnet build *.csproj -c Release -o /app/build

FROM build AS publish
RUN dotnet publish *.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Ocelot.Gateway.dll"]
