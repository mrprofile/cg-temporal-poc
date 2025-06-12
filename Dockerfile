# Use the official .NET runtime as base image
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app

# Use the SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

# Copy project files and restore dependencies
COPY ["src/ConsoleExecutor.Worker/ConsoleExecutor.Worker.csproj", "src/ConsoleExecutor.Worker/"]
COPY ["src/ConsoleExecutor.Activities/ConsoleExecutor.Activities.csproj", "src/ConsoleExecutor.Activities/"]
COPY ["src/ConsoleExecutor.Workflows/ConsoleExecutor.Workflows.csproj", "src/ConsoleExecutor.Workflows/"]
COPY ["src/ConsoleExecutor.Common/ConsoleExecutor.Common.csproj", "src/ConsoleExecutor.Common/"]

# Restore NuGet packages
RUN dotnet restore "src/ConsoleExecutor.Worker/ConsoleExecutor.Worker.csproj"

# Copy all source code
COPY . .

# Build the application
WORKDIR "/src/src/ConsoleExecutor.Worker"
RUN dotnet build "ConsoleExecutor.Worker.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "ConsoleExecutor.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final runtime stage
FROM base AS final
WORKDIR /app

# Create a non-root user for security
RUN groupadd -r temporal && useradd -r -g temporal temporal

# Copy published application
COPY --from=publish /app/publish .

# Set ownership
RUN chown -R temporal:temporal /app

# Switch to non-root user
USER temporal

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Health check endpoint (optional)
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "ConsoleExecutor.Worker.dll"]