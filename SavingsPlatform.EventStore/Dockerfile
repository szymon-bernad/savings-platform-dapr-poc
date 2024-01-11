#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["SavingsPlatform.EventStore/SavingsPlatform.EventStore.csproj", "SavingsPlatform.EventStore/"]
COPY ["SavingsPlatform.Common/SavingsPlatform.Common.csproj", "SavingsPlatform.Common/"]
RUN dotnet restore "SavingsPlatform.EventStore/SavingsPlatform.EventStore.csproj"
COPY . .
WORKDIR "/src/SavingsPlatform.EventStore"
RUN dotnet build "SavingsPlatform.EventStore.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SavingsPlatform.EventStore.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ConnectionStrings__DocumentStore="host=host.docker.internal;username=postgres;password=example;port=5432;database=event-store"
ENV DocumentStoreConfig__PlatformId=21a47807-67d1-4458-858a-7c98800c8d15

ENTRYPOINT ["dotnet", "SavingsPlatform.EventStore.dll"]