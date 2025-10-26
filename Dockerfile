# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /source

# copy csproj and restore as distinct layers
COPY src src

RUN cd src/BriefingRoom && dotnet remove package IronPdf && dotnet add package IronPdf.Linux --version 2025.9.4 && cd ../../

RUN dotnet publish -c Release -o /app --use-current-runtime --self-contained false src/Web/Web.csproj

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:9.0
RUN apt update \
    && apt install -y libc6 libc6-dev libgtk2.0-0 libnss3 libatk-bridge2.0-0 libx11-xcb1 libxcb-dri3-0 libdrm-common libgbm1 libasound2 libappindicator3-1 libxrender1 libfontconfig1 libxshmfence1 libgdiplus libva-dev
WORKDIR /app
COPY --from=build /app .
COPY Database Database
COPY DatabaseJSON DatabaseJSON
COPY CustomConfigs CustomConfigs
COPY Media Media
COPY Include Include
ENTRYPOINT ["dotnet", "Web.dll"]