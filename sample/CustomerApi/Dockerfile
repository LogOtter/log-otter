FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80

FROM base AS final
WORKDIR /app
COPY ["bin/docker-publish/", "."]
ENTRYPOINT ["dotnet", "CustomerApi.dll"]

LABEL org.opencontainers.image.title="Customer API"
LABEL org.opencontainers.image.description="Sample API showing how to use LogOtter"
