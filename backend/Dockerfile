FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY PensionAgent.Api.csproj ./
COPY NuGet.Config ./
RUN dotnet restore PensionAgent.Api.csproj
COPY . ./
RUN dotnet publish PensionAgent.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
COPY --from=build /app/publish ./
USER $APP_UID
ENTRYPOINT ["dotnet", "PensionAgent.Api.dll"]
