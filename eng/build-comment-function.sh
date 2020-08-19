#!/bin/sh

BUILD_CONFIGURATION="Release"

DIRECTORY=$(dirname `realpath $0`)
ROOT="$DIRECTORY/.."

ARTIFACTS="$ROOT/artifacts"
FUNCTION_ARTIFACT="$ARTIFACTS/blog-comment-function"

FUNCTION_SOURCE="$ROOT/src/blog-comment-function/src"
FUNCTION_PROJECT="$FUNCTION_SOURCE/BlogComments/BlogComments.csproj"

mkdir -p $FUNCTION_ARTIFACT

dotnet restore $FUNCTION_PROJECT
dotnet build --no-restore -c $BUILD_CONFIGURATION $FUNCTION_PROJECT
dotnet publish --no-build -c $BUILD_CONFIGURATION -o $FUNCTION_ARTIFACT $FUNCTION_PROJECT

cd $FUNCTION_ARTIFACT
zip -r "$FUNCTION_ARTIFACT.zip" .
