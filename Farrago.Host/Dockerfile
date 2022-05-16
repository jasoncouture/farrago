FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
ENV ASPNETCORE_URLS=http://0.0.0.0:80/
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "Farrago.Host/Farrago.Host.csproj"
WORKDIR "/src/Farrago.Host"
RUN dotnet build "Farrago.Host.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Farrago.Host.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Farrago.Host.dll"]
