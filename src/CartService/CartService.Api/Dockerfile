# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy all source code (relative to context path = UserService.Api)
COPY . .

# Restore dependencies for UserService.Api
RUN dotnet restore "src/CartService/CartService.Api/CartService.Api.csproj"

# Build the application
RUN dotnet build "src/CartService/CartService.Api/CartService.Api.csproj" -c Release -o /app/build

# Stage 2: Publish the application
FROM build AS publish
RUN dotnet publish "src/CartService/CartService.Api/CartService.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "CartService.Api.dll"]
