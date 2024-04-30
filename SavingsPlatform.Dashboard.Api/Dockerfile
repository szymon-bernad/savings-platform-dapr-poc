#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["SavingsPlatform.Dashboard.Api/SavingsPlatform.Dashboard.Api.csproj", "SavingsPlatform.Dashboard.Api/"]
COPY ["SavingsPlatform.Contracts/SavingsPlatform.Contracts.csproj", "SavingsPlatform.Contracts/"]
RUN dotnet restore "./SavingsPlatform.Dashboard.Api/./SavingsPlatform.Dashboard.Api.csproj"
COPY . .
WORKDIR "/src/SavingsPlatform.Dashboard.Api"
RUN dotnet build "./SavingsPlatform.Dashboard.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./SavingsPlatform.Dashboard.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SavingsPlatform.Dashboard.Api.dll"]