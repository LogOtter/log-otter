FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app
EXPOSE 80

FROM base AS final
WORKDIR /app
COPY ["bin/docker-publish/", "."]
ENTRYPOINT ["dotnet", "CustomerWorker.exe"]

LABEL org.opencontainers.image.title="Customer Worker"
LABEL org.opencontainers.image.description="Sample worker showing how to use LogOtter"
