FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-env
WORKDIR /app

ENV PATH="/root/.dotnet/tools:${PATH}"
RUN dotnet tool install --global dotnet-certificate-tool

# Copy csproj and restore as distinct layers
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/runtime:3.1

COPY --from=build-env /root/.dotnet/tools/ /opt/bin
ENV PATH="/opt/bin:${PATH}"

WORKDIR /app
COPY --from=build-env /app/out .
COPY --from=build-env /app/cert.pfx /

ENV cert_password passw0rd!
ENV cert_thumbprint D2AEF8C80A370D63DB4DF8F66BBFDF77815792CC
RUN certificate-tool add -f /cert.pfx --password $cert_password --thumbprint $cert_thumbprint

ENV ENV docker
ENTRYPOINT ["dotnet", "TwitchBotConsole.dll"]