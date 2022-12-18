FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env

WORKDIR /App

COPY . ./

COPY ./en_US.aff App/

COPY ./en_US.dic App/

RUN dotnet restore

RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:6.0

WORKDIR /App

COPY --from=build-env /App/out .

ENTRYPOINT ["dotnet", "WintBot.dll"]