FROM mcr.microsoft.com/dotnet/sdk:6.0 AS sdk
WORKDIR /src

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
ENV ASPNETCORE_URLS=http://0.0.0.0:80/
EXPOSE 80

FROM sdk AS dependencies
WORKDIR /src

COPY ./*.sln ./
COPY */*.csproj ./

RUN for file in $(ls *.csproj); do mkdir -p ${file%.*} && mv $file ${file%.*}; done

RUN dotnet restore "Farrago.Host/Farrago.Host.csproj"

COPY . ./

RUN dotnet restore "Farrago.Host/Farrago.Host.csproj" --locked-mode

WORKDIR "/src/Farrago.Host"

FROM dependencies AS build
RUN dotnet publish "Farrago.Host.csproj" -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Farrago.Host.dll"]
