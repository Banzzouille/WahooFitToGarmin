#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
RUN apt update && apt install python3.7 -y && apt install python3-pip -y && apt clean
RUN pip3 install garmin-uploader
       
WORKDIR /app
EXPOSE 80


FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["WahooFitToGarmin/WahooFitToGarmin.csproj", "WahooFitToGarmin/"]
RUN dotnet restore "WahooFitToGarmin/WahooFitToGarmin.csproj"
COPY . .
WORKDIR "/src/WahooFitToGarmin"
RUN dotnet build "WahooFitToGarmin.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WahooFitToGarmin.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WahooFitToGarmin.dll"]
