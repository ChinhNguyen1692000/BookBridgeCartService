# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj
COPY ["BookstoreService.Api/BookstoreService.Api.csproj", "BookstoreService.Api/"]
COPY ["BookstoreService.Application/BookstoreService.Application.csproj", "BookstoreService.Application/"]
COPY ["BookstoreService.Domain/BookstoreService.Domain.csproj", "BookstoreService.Domain/"]
COPY ["BookstoreService.Infracstructure/BookstoreService.Infracstructure.csproj", "BookstoreService.Infracstructure/"]

# Restore
RUN dotnet restore "BookstoreService.Api/BookstoreService.Api.csproj"

# Copy source
COPY . .
WORKDIR "/src/BookstoreService.Api"
RUN dotnet build "BookstoreService.Api.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "BookstoreService.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "BookstoreService.Api.dll"]
