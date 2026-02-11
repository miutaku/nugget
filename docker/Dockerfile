# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy solution and project files
COPY Nugget.slnx ./
COPY src/Nugget.Core/Nugget.Core.csproj src/Nugget.Core/
COPY src/Nugget.Infrastructure/Nugget.Infrastructure.csproj src/Nugget.Infrastructure/
COPY src/Nugget.Api/Nugget.Api.csproj src/Nugget.Api/
COPY src/Nugget.Web/Nugget.Web.csproj src/Nugget.Web/
COPY tests/Nugget.Api.Tests/Nugget.Api.Tests.csproj tests/Nugget.Api.Tests/
COPY tests/Nugget.Core.Tests/Nugget.Core.Tests.csproj tests/Nugget.Core.Tests/
COPY tests/Nugget.Infrastructure.Tests/Nugget.Infrastructure.Tests.csproj tests/Nugget.Infrastructure.Tests/

# Restore dependencies
RUN dotnet restore Nugget.slnx

# Copy the rest of the source code
COPY . .

# Test stage
FROM build AS test
RUN dotnet test Nugget.slnx -c Release --no-restore

# Publish stage
FROM build AS publish
RUN dotnet publish src/Nugget.Api/Nugget.Api.csproj -c Release -o /out/api
RUN dotnet publish src/Nugget.Web/Nugget.Web.csproj -c Release -o /out/web

# Runtime stage for API
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS api
RUN apt-get update && apt-get install -y libgssapi-krb5-2 && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=publish /out/api .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Nugget.Api.dll"]

# Nginx stage for static Blazor WebAssembly files
FROM nginx:alpine AS web
COPY --from=publish /out/web/wwwroot /usr/share/nginx/html
COPY docker/nginx.conf /etc/nginx/nginx.conf
EXPOSE 80
