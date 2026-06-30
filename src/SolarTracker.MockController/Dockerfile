FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY src/SolarTracker.Shared/SolarTracker.Shared.csproj SolarTracker.Shared/
COPY src/SolarTracker.MockController/SolarTracker.MockController.csproj SolarTracker.MockController/
RUN dotnet restore SolarTracker.MockController/SolarTracker.MockController.csproj
COPY src/SolarTracker.Shared/ SolarTracker.Shared/
COPY src/SolarTracker.MockController/ SolarTracker.MockController/
RUN dotnet publish SolarTracker.MockController/SolarTracker.MockController.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:9.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "SolarTracker.MockController.dll"]
