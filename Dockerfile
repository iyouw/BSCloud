FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /source

COPY BSCloud/*.csproj   ./BSCloud/
RUN dotnet restore

COPY BSCloud/. ./BSCloud/
WORKDIR /source/BSCloud
RUN dotnet publish -c release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=buld /app ./
ENTRYPOINT ["dotnet","BSCloud.dll"]