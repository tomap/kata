FROM mcr.microsoft.com/dotnet/sdk:6.0 as build

COPY . .

RUN dotnet publish -c Release -o /out sensor

FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=build /out . 
ENTRYPOINT ["dotnet", "sensor.dll"]