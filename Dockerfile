# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy the solution file and project files (including test projects)
# All paths are relative to the Git repository root (where the Dockerfile and WORKDIR are)
COPY src/MyMediaVerse/MyMediaVerse.sln src/MyMediaVerse/MyMediaVerse.sln
COPY src/MyMediaVerse/MyMediaVerse.Web.API/*.csproj src/MyMediaVerse/MyMediaVerse.Web.API/
COPY src/MyMediaVerse/MyMediaVerse.Application/*.csproj src/MyMediaVerse/MyMediaVerse.Application/
COPY src/MyMediaVerse/MyMediaVerse.Domain/*.csproj src/MyMediaVerse/MyMediaVerse.Domain/
COPY src/MyMediaVerse/MyMediaVerse.Infrastructure/*.csproj src/MyMediaVerse/MyMediaVerse.Infrastructure/
COPY src/MyMediaVerse/MyMediaVerse.Shared/*.csproj src/MyMediaVerse/MyMediaVerse.Shared/
COPY src/MyMediaVerse/MyMediaVerse.DTOs/*.csproj src/MyMediaVerse/MyMediaVerse.DTOs/
COPY tests/MyMediaVerse.UnitTests/*.csproj tests/MyMediaVerse.UnitTests/
COPY tests/MyMediaVerse.IntegrationTests/*.csproj tests/MyMediaVerse.IntegrationTests/


# Run dotnet restore for the solution file
# The path is relative to the WORKDIR /app
RUN dotnet restore src/MyMediaVerse/MyMediaVerse.sln

# Copy all remaining source code (after restore to leverage caching)
COPY . .

# Publish the Web.API project
# Change WORKDIR to the specific project folder for publishing
WORKDIR /app/src/MyMediaVerse/MyMediaVerse.Web.API
RUN dotnet publish "MyMediaVerse.Web.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Create the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app/publish
# Copy only the published output from the build stage
COPY --from=build /app/publish .

# Expose the port
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "MyMediaVerse.Web.API.dll"]