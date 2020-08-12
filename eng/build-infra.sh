#!/bin/sh

BUILD_CONFIGURATION="Release"

DIRECTORY=$(dirname `realpath $0`)
ROOT="$DIRECTORY/.."

ARTIFACTS="$ROOT/artifacts"
INFRA_ARTIFACT="$ARTIFACTS/infra"

INFRA_SOURCE="$ROOT/infra"
INFRA_PROJECT="$INFRA_SOURCE/blog/blog.csproj"

mkdir -p $INFRA_ARTIFACT

dotnet restore $INFRA_PROJECT
dotnet build --no-restore -c $BUILD_CONFIGURATION $INFRA_PROJECT
dotnet publish --no-build -c $BUILD_CONFIGURATION -o $INFRA_ARTIFACT $INFRA_PROJECT
