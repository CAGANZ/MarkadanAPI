# ── Stage 1: Build ────────────────────────────────────────────────────────────
# SDK image'ı sadece derleme için kullanılır; final image'a dahil olmaz.
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Önce sadece .csproj dosyaları kopyalanır.
# Kaynak kod değişmediği sürece "dotnet restore" katmanı cache'den gelir.
COPY ["Markadan.API/Markadan.API.csproj",                         "Markadan.API/"]
COPY ["Markadan.Application/Markadan.Application.csproj",         "Markadan.Application/"]
COPY ["Markadan.Domain/Markadan.Domain.csproj",                   "Markadan.Domain/"]
COPY ["Markadan.Infrastructure/Markadan.Infrastructure.csproj",   "Markadan.Infrastructure/"]
RUN dotnet restore "Markadan.API/Markadan.API.csproj"

# Kaynak kodu kopyala ve yayınla.
COPY . .
RUN dotnet publish "Markadan.API/Markadan.API.csproj" \
        -c Release \
        -o /app/publish \
        --no-restore

# ── Stage 2: Runtime ──────────────────────────────────────────────────────────
# Sadece ASP.NET Core runtime içerir; SDK, araçlar ve kaynak kod yoktur.
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# .NET 8+ imajlarında varsayılan HTTP portu 8080'dir.
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# Güvenlik: imajda hazır gelen non-root 'app' kullanıcısıyla çalıştır.
USER app

ENTRYPOINT ["dotnet", "Markadan.API.dll"]
