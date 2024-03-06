FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /App

# Copy everything
COPY . ./
# Restore as distinct layers
WORKDIR /App/ConversationalSearchPlatform.BackOffice
RUN dotnet restore
# Build and publish a release
RUN dotnet publish -c Release -o out -v n

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /App
COPY --from=build-env /App/ConversationalSearchPlatform.BackOffice/out .

ENV ASPNETCORE_HTTPS_PORT=https://+:5001
ENV ASPNETCORE_URLS=http://+:5000

ENTRYPOINT ["dotnet", "ConversationalSearchPlatform.BackOffice.dll"]