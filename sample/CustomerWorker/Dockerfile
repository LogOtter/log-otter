FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM base AS final
WORKDIR /app
COPY ["bin/docker-publish/", "."]
ENTRYPOINT ["dotnet", "CustomerWorker.dll"]
