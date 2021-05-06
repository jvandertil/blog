#!/bin/sh

# Update the version in src/blog/hugo.ps1 as well
HUGO_VERSION="0.83.1"
HUGO_URL="https://github.com/gohugoio/hugo/releases/download/v$HUGO_VERSION/hugo_extended_${HUGO_VERSION}_Linux-64bit.tar.gz"

DIRECTORY=$(dirname `realpath $0`)
ROOT="$DIRECTORY/.."
ARTIFACTS="$ROOT/artifacts"
BLOG_ARTIFACT="$ARTIFACTS/blog"

SOURCE="$ROOT/src"
BLOG_SOURCE="$SOURCE/blog"

HUGO_BIN_DIR="$BLOG_SOURCE/.bin"
HUGO_BIN="$HUGO_BIN_DIR/hugo"

mkdir $HUGO_BIN_DIR
mkdir -p $BLOG_ARTIFACT

curl -L $HUGO_URL | tar -C $HUGO_BIN_DIR -xzf -

$HUGO_BIN --source $BLOG_SOURCE --destination $BLOG_ARTIFACT --minify
