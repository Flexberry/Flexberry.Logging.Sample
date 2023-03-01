FROM node:10 as ember_app

COPY ember-app /ember-app

WORKDIR /ember-app

RUN \
  set -x; \
  yarn global add ember-cli@3.1.4; \
  yarn global add bower;

RUN \
  set -x; \
  yarn install ; \
  bower install  --allow-root;

RUN \
  set -x; \
  ember build --environment=docker

FROM mcr.microsoft.com/dotnet/sdk:3.1 as backend

COPY FlexberrySampleLogging /FlexberrySampleLogging

WORKDIR /FlexberrySampleLogging

RUN \
 dotnet restore FlexberrySampleLogging.sln;\
 dotnet build FlexberrySampleLogging.sln -c Release;\
 dotnet publish ODataBackend/FlexberrySampleLogging.ODataBackend.csproj -c Release -o out -f netcoreapp3.1

FROM mcr.microsoft.com/dotnet/aspnet:3.1

WORKDIR /app

COPY --from=backend /FlexberrySampleLogging/out ./
COPY --from=ember_app /ember-app/dist ./wwwroot/

# Check webapi responses
HEALTHCHECK --interval=60s --timeout=15s --start-period=20s \
  CMD curl -f "http://0.0.0.0/health" || exit

ENTRYPOINT ["dotnet", "FlexberrySampleLogging.ODataBackend.dll"]
