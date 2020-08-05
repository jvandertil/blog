#!/bin/sh

BUILD_CONFIGURATION="Release"

DIRECTORY=$(dirname `realpath $0`)
ROOT="$DIRECTORY/.."

ARTIFACTS="$ROOT/artifacts"
UPLOADER_ARTIFACT="$ARTIFACTS/uploader"

SOURCE="$ROOT/src"
UPLOADER_SOLUTION="$SOURCE/Uploader/Uploader.sln"

mkdir -p $UPLOADER_ARTIFACT

dotnet restore $UPLOADER_SOLUTION
dotnet build --no-restore -c $BUILD_CONFIGURATION $UPLOADER_SOLUTION
dotnet test --no-build -c $BUILD_CONFIGURATION  $UPLOADER_SOLUTION
dotnet publish --no-build -c $BUILD_CONFIGURATION -o $UPLOADER_ARTIFACT $UPLOADER_SOLUTION
