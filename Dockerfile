# syntax=docker/dockerfile:1

ARG DOTNETVERBUILD=8.0
ARG DOTNETVERRUN=8.0
ARG BASEIMAGE=alpine

# Use --platform=$BUILDPLATFORM in order to correctly pull the base image for the build platform.
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:$DOTNETVERBUILD-$BASEIMAGE AS build

COPY . /source
WORKDIR /source

ARG TARGETARCH
ARG BUILDPLATFORM
ARG TARGETPLATFORM

# Build the application.
RUN echo "Running on $BUILDPLATFORM, building for $TARGETPLATFORM ($TARGETARCH)"
# RUN dotnet restore -a $TARGETARCH
# RUN dotnet publish -a $TARGETARCH --no-restore --property:PublishDir=/app
RUN dotnet publish -a $TARGETARCH --property:PublishDir=/app MyPo.Blazor/MyPo.Blazor/MyPo.Blazor.csproj

################################################################################

# If you need to enable globalization and time zones:
# https://github.com/dotnet/dotnet-docker/blob/main/samples/enable-globalization.md

FROM mcr.microsoft.com/dotnet/aspnet:$DOTNETVERRUN-$BASEIMAGE AS final
# FROM mcr.microsoft.com/dotnet/sdk:$DOTNETVERRUN-$BASEIMAGE AS final
WORKDIR /app

RUN apk add --no-cache tzdata
# Change the time zone as needed.
ENV TZ="Etc/UTC"

# ATTENTION: Change this to match the name of your application.
ARG BASENAME="MyPo"

COPY --from=build /app ./
COPY ${BASENAME}.Blazor/${BASENAME}.Blazor/config ./config
COPY ${BASENAME}.Blazor/${BASENAME}.Blazor/data ./data

# Create a non-privileged user that the app will run under.
# See https://docs.docker.com/go/dockerfile-user-best-practices/
ARG UID=10001
RUN adduser \
    --disabled-password \
    --gecos "" \
    --home "/nonexistent" \
    --shell "/sbin/nologin" \
    --no-create-home \
    --uid "$UID" \
    appuser
RUN chown -R appuser:appuser /app
USER appuser

# Enable Swagger UI
ENV ENABLE_SWAGGER_UI=true

# API base URL setting for Blazor Server mode
ENV API__BaseUrl=http://localhost:8080

# Set database type to InMemory for demo purposes
ENV Databases__Identity__Type=InMemory
ENV Databases__Portfolio__Type=InMemory

# Default port for dotnet application
EXPOSE 8080

# Roll forward to latest major version of .NET installed in the container
ENV DOTNET_ROLL_FORWARD=LatestMajor

# ATTENTION: Change this to match the name of your application.
ENTRYPOINT ["dotnet", "MyPo.Blazor.dll"]
