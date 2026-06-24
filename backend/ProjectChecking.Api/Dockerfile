FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY backend/ProjectChecking.Api/ProjectChecking.Api.csproj backend/ProjectChecking.Api/
RUN dotnet restore backend/ProjectChecking.Api/ProjectChecking.Api.csproj
COPY backend/ProjectChecking.Api/ backend/ProjectChecking.Api/
WORKDIR /src/backend/ProjectChecking.Api
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
COPY db ./db
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "ProjectChecking.Api.dll"]
