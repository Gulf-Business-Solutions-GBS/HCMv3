#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/HCMv3.Migrator/HCMv3.Migrator.csproj", "src/HCMv3.Migrator/"]
COPY ["src/HCMv3.EntityFrameworkCore/HCMv3.EntityFrameworkCore.csproj", "src/HCMv3.EntityFrameworkCore/"]
COPY ["src/HCMv3.Core/HCMv3.Core.csproj", "src/HCMv3.Core/"]
COPY ["src/HCMv3.Core.Shared/HCMv3.Core.Shared.csproj", "src/HCMv3.Core.Shared/"]
RUN dotnet restore "src/HCMv3.Migrator/HCMv3.Migrator.csproj"
COPY . .
WORKDIR "/src/src/HCMv3.Migrator"
RUN dotnet build "HCMv3.Migrator.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "HCMv3.Migrator.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HCMv3.Migrator.dll", "-s"]
