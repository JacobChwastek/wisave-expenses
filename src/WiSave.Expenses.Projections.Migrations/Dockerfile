FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props .
COPY NuGet.config .
COPY WiSave.Expenses.slnx .
COPY src/ src/
RUN dotnet restore src/WiSave.Expenses.Projections.Migrations/WiSave.Expenses.Projections.Migrations.csproj
RUN dotnet publish src/WiSave.Expenses.Projections.Migrations/WiSave.Expenses.Projections.Migrations.csproj -c Release -o /app/publish --no-restore

FROM base AS final
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "WiSave.Expenses.Projections.Migrations.dll"]
