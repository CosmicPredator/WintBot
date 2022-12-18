FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env

WORKDIR /App

COPY . ./

ENV TOKEN=MTA1MTU1Nzc5MDY3OTg0Njk5Mg.Gjt0vD.k5gj5jFy6hMwAsHMKQIT5Q2UA1P05_efunWlOo

RUN dotnet restore

RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:6.0

WORKDIR /App

COPY --from=build-env /App/out .

ENTRYPOINT ["dotnet", "WintBot.dll"]