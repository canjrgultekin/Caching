# Bu stage h�zl� debug i�in runtime imaj�n� kullan�r (Debug konfig�rasyonu i�in)
FROM mcr.microsoft.com/dotnet/nightly/aspnet:9.0 AS base
WORKDIR /app

# Bu stage uygulamay� build eder (SDK kullan�l�r)
FROM mcr.microsoft.com/dotnet/nightly/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Proje dosyas�n� kopyalay�p restore eder
COPY ["CachingTestProject/CachingTestProject.csproj", "CachingTestProject/"]
RUN dotnet restore "./CachingTestProject/CachingTestProject.csproj"

# T�m kaynak kodunu kopyalay�p build eder
COPY . .
WORKDIR "/src/CachingTestProject"
RUN dotnet build "./CachingTestProject.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Bu stage uygulamay� publish eder
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./CachingTestProject.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final stage: Uygulama production ortam�nda �al��t�r�l�r
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CachingTestProject.dll"]
