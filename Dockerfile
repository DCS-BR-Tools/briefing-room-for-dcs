# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# copy csproj and restore as distinct layers
COPY src src

RUN dotnet publish -c Release -o /app --use-current-runtime --self-contained false src/Web/Web.csproj

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:10.0
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV PUPPETEER_EXECUTABLE_PATH=/usr/bin/google-chrome-stable

# Install Google Chrome (Ubuntu's chromium is a snap wrapper that doesn't work in Docker)
RUN apt update \
    && apt install -y --no-install-recommends \
        wget \
        gnupg \
        libgdiplus \
    && wget -q -O - https://dl.google.com/linux/linux_signing_key.pub | gpg --dearmor -o /usr/share/keyrings/google-chrome.gpg \
    && echo "deb [arch=amd64 signed-by=/usr/share/keyrings/google-chrome.gpg] http://dl.google.com/linux/chrome/deb/ stable main" > /etc/apt/sources.list.d/google-chrome.list \
    && apt update \
    && apt install -y --no-install-recommends google-chrome-stable \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app .
COPY Database bin/Database
COPY DatabaseJSON bin/DatabaseJSON
COPY CustomConfigs bin/CustomConfigs
COPY Media bin/Media
COPY Include bin/Include
ENTRYPOINT ["dotnet", "bin/BriefingRoom-Web.dll"]