# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy csproj and restore
COPY ["RegisterApi/RegisterApi.csproj", "./"]
RUN dotnet restore "./RegisterApi.csproj"

# Copy everything else 
COPY . . 

# Publish
RUN dotnet publish "RegisterApi.csproj" -c Release -o /app/out

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "RegisterApi.dll"]