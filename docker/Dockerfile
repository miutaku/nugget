# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY Nugget.sln ./
COPY src/Nugget.Core/Nugget.Core.csproj src/Nugget.Core/
COPY src/Nugget.Infrastructure/Nugget.Infrastructure.csproj src/Nugget.Infrastructure/
COPY src/Nugget.Api/Nugget.Api.csproj src/Nugget.Api/
COPY src/Nugget.Web/Nugget.Web.csproj src/Nugget.Web/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY src/ src/

# Build and publish API
RUN dotnet publish src/Nugget.Api/Nugget.Api.csproj -c Release -o /app/api

# Build and publish Web (Blazor WebAssembly)
RUN dotnet publish src/Nugget.Web/Nugget.Web.csproj -c Release -o /app/web

# Runtime stage for API
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS api
WORKDIR /app
COPY --from=build /app/api .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Nugget.Api.dll"]

# Nginx stage for static Blazor WebAssembly files
FROM nginx:alpine AS web
COPY --from=build /app/web/wwwroot /usr/share/nginx/html
COPY docker/nginx.conf /etc/nginx/nginx.conf
EXPOSE 80
