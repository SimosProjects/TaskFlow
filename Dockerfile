# ---------- build stage ----------
    FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
    WORKDIR /src
    
    # Copy csproj(s) first to maximize layer caching
    COPY TaskFlow.Api/TaskFlow.Api.csproj TaskFlow.Api/
    
    RUN dotnet restore TaskFlow.Api/TaskFlow.Api.csproj
    
    # Copy the rest of the source
    COPY . .
    
    # Publish to a folder for the runtime image
    RUN dotnet publish TaskFlow.Api/TaskFlow.Api.csproj -c Release -o /app/publish /p:UseAppHost=false
    
    # ---------- runtime stage ----------
    FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
    WORKDIR /app
    
    # Defensive default: bind to 8080 inside the container regardless of what
    # environment variables are passed at deploy time. Without this, omitting
    # ASPNETCORE_URLS at deploy time causes the container to bind the wrong port silently.
    ENV ASPNETCORE_URLS=http://+:8080
    
    # Sets environment to Production by default in container
    ENV ASPNETCORE_ENVIRONMENT=Production
    
    EXPOSE 8080
    
    COPY --from=build /app/publish .
    
    ENTRYPOINT ["dotnet", "TaskFlow.Api.dll"]    