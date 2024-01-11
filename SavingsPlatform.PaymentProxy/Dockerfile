FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["SavingsPlatform.PaymentProxy/SavingsPlatform.PaymentProxy.csproj", "SavingsPlatform.PaymentProxy/"]
COPY ["SavingsPlatform.Contracts/SavingsPlatform.Contracts.csproj", "SavingsPlatform.Contracts/"]
RUN dotnet restore "SavingsPlatform.PaymentProxy/SavingsPlatform.PaymentProxy.csproj"
COPY . .
WORKDIR "/src/SavingsPlatform.PaymentProxy"
RUN dotnet build "SavingsPlatform.PaymentProxy.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SavingsPlatform.PaymentProxy.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SavingsPlatform.PaymentProxy.dll"]