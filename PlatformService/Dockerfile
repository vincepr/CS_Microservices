FROM mcr.microsoft.com/dotnet/sdk:7.0 as build-envFROM
WORKDIR /app

COPY *.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app
COPY --from=build-envFROM /app/out .
ENTRYPOINT [ "dotnet", "PlatformService.dll" ]