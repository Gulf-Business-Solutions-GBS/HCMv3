#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/HCMv3.Web.Public/HCMv3.Web.Public.csproj", "src/HCMv3.Web.Public/"]
COPY ["src/HCMv3.Web.Core/HCMv3.Web.Core.csproj", "src/HCMv3.Web.Core/"]
COPY ["src/HCMv3.Application/HCMv3.Application.csproj", "src/HCMv3.Application/"]
COPY ["src/HCMv3.Application.Shared/HCMv3.Application.Shared.csproj", "src/HCMv3.Application.Shared/"]
COPY ["src/HCMv3.Core.Shared/HCMv3.Core.Shared.csproj", "src/HCMv3.Core.Shared/"]
COPY ["src/HCMv3.Core/HCMv3.Core.csproj", "src/HCMv3.Core/"]
COPY ["src/HCMv3.EntityFrameworkCore/HCMv3.EntityFrameworkCore.csproj", "src/HCMv3.EntityFrameworkCore/"]
COPY ["src/HCMv3.GraphQL/HCMv3.GraphQL.csproj", "src/HCMv3.GraphQL/"]
RUN dotnet restore "src/HCMv3.Web.Public/HCMv3.Web.Public.csproj"
COPY . .
WORKDIR "/src/src/HCMv3.Web.Public"
RUN dotnet build "HCMv3.Web.Public.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "HCMv3.Web.Public.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HCMv3.Web.Public.dll"]
