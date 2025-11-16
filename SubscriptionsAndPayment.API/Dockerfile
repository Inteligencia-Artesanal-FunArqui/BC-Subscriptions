FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["OsitoPolar.Shared.Events/OsitoPolar.Shared.Events.csproj", "OsitoPolar.Shared.Events/"]
COPY ["OsitoPolar.Subscriptions.Service/SubscriptionsAndPayment.API/SubscriptionsAndPayment.API/SubscriptionsAndPayment.API.csproj", "OsitoPolar.Subscriptions.Service/SubscriptionsAndPayment.API/SubscriptionsAndPayment.API/"]
RUN dotnet restore "OsitoPolar.Subscriptions.Service/SubscriptionsAndPayment.API/SubscriptionsAndPayment.API/SubscriptionsAndPayment.API.csproj"
COPY OsitoPolar.Shared.Events/ OsitoPolar.Shared.Events/
COPY OsitoPolar.Subscriptions.Service/SubscriptionsAndPayment.API/SubscriptionsAndPayment.API/ OsitoPolar.Subscriptions.Service/SubscriptionsAndPayment.API/SubscriptionsAndPayment.API/
WORKDIR "/src/OsitoPolar.Subscriptions.Service/SubscriptionsAndPayment.API/SubscriptionsAndPayment.API"
RUN dotnet build "SubscriptionsAndPayment.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "SubscriptionsAndPayment.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SubscriptionsAndPayment.API.dll"]
