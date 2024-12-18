# Bu stage hýzlý debug için runtime imajýný kullanýr (Debug konfigürasyonu için)
FROM mcr.microsoft.com/dotnet/nightly/aspnet:9.0 AS base
WORKDIR /app

# Bu stage uygulamayý build eder (SDK kullanýlýr)
FROM mcr.microsoft.com/dotnet/nightly/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Proje dosyasýný kopyalayýp restore eder
COPY ["CachingTestProject/CachingTestProject.csproj", "CachingTestProject/"]
RUN dotnet restore "./CachingTestProject/CachingTestProject.csproj"

# Tüm kaynak kodunu kopyalayýp build eder
COPY . .
WORKDIR "/src/CachingTestProject"
RUN dotnet build "./CachingTestProject.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Bu stage uygulamayý publish eder
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./CachingTestProject.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final stage: Uygulama production ortamýnda çalýþtýrýlýr
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CachingTestProject.dll"]
