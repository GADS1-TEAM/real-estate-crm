FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and restore dependencies
COPY *.sln .
COPY **/*.csproj .
RUN dotnet restore

# Copy source and publish
COPY . .
RUN dotnet publish bffs/platform-admin-bff/src/PlatformAdminBff.Api/PlatformAdminBff.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://*:80
EXPOSE 80
ENTRYPOINT ["dotnet", "PlatformAdminBff.Api.dll"]
