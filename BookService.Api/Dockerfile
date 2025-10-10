# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files
COPY ["BookService.Api/BookService.Api.csproj", "BookService.Api/"]
COPY ["BookService.Application/BookService.Application.csproj", "BookService.Application/"]
COPY ["BookService.Domain/BookService.Domain.csproj", "BookService.Domain/"]
COPY ["BookService.Infracstructure/BookService.Infracstructure.csproj", "BookService.Infracstructure/"]

# Restore dependencies
RUN dotnet restore "BookService.Api/BookService.Api.csproj"

# Copy all source code
COPY . .
WORKDIR "/src/BookService.Api"
RUN dotnet build "BookService.Api.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "BookService.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Final image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "BookService.Api.dll"]
