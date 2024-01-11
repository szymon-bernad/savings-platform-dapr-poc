#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["SavingsPlatform.Api/SavingsPlatform.Api.csproj", "SavingsPlatform.Api/"]
COPY ["SavingsPlatform.Common/SavingsPlatform.Common.csproj", "SavingsPlatform.Common/"]
COPY ["SavingsPlatform.Accounts/SavingsPlatform.Accounts.csproj", "SavingsPlatform.Accounts/"]
RUN dotnet restore "SavingsPlatform.Api/SavingsPlatform.Api.csproj"
COPY . .
WORKDIR "/src/SavingsPlatform.Api"
RUN dotnet build "SavingsPlatform.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SavingsPlatform.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SavingsPlatform.Api.dll"]