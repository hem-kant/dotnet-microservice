FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-env
RUN mkdir /app
WORKDIR /app

# copy the project and restore as distinct layers in the image
COPY ./*.csproj ./
RUN dotnet restore -c Release

# copy the rest and build
COPY ./ ./
RUN dotnet build
RUN dotnet publish -c Release -o out

# build runtime image
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1
RUN mkdir /app
WORKDIR /app

COPY --from=build-env /app/out ./
ENTRYPOINT ["dotnet", "Catalog.Service.dll"]