FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["WahooFitToGarmin/WahooFitToGarmin.csproj", "Sources/"]
RUN dotnet restore "WahooFitToGarmin/WahooFitToGarmin.csproj"
COPY . .
WORKDIR "/src/Sources"
RUN dotnet build "WahooFitToGarmin.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WahooFitToGarmin.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WahooFitToGarmin.dll"]
