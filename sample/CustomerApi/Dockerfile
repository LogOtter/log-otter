FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM base AS final
WORKDIR /app
COPY ["bin/docker-publish/", "."]
ENTRYPOINT ["dotnet", "CustomerApi.dll"]

LABEL org.opencontainers.image.source=https://github.com/LogOtter/log-otter
