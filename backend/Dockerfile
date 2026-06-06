FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

COPY PointsMall.csproj ./
RUN dotnet restore PointsMall.csproj

COPY . .
RUN dotnet publish PointsMall.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 80

ENTRYPOINT ["dotnet", "PointsMall.dll"]
