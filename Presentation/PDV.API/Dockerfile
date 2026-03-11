FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY PDV.sln Directory.Build.props ./
COPY Core/PDV.Core/PDV.Core.csproj Core/PDV.Core/
COPY Core/PDV.Shared/PDV.Shared.csproj Core/PDV.Shared/
COPY Infra/PDV.Data.Cloud/PDV.Data.Cloud.csproj Infra/PDV.Data.Cloud/
COPY Presentation/PDV.API/PDV.API.csproj Presentation/PDV.API/

RUN dotnet restore Presentation/PDV.API/PDV.API.csproj

COPY Core/ Core/
COPY Infra/PDV.Data.Cloud/ Infra/PDV.Data.Cloud/
COPY Presentation/PDV.API/ Presentation/PDV.API/

RUN dotnet publish Presentation/PDV.API/PDV.API.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "PDV.API.dll"]
