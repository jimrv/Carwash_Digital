FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY CarWashDigital.Web/CarWashDigital.Web.csproj CarWashDigital.Web/
RUN dotnet restore CarWashDigital.Web/CarWashDigital.Web.csproj

COPY . .
WORKDIR /src/CarWashDigital.Web
RUN dotnet publish CarWashDigital.Web.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "CarWashDigital.Web.dll"]
