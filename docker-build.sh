#! /bin/bash
set -e

# Builds Librariann for the three Linux architectures the Docker image supports, then builds and pushes a
# multi-arch image to Docker Hub. Multi-arch images can only be pushed to a registry, not loaded into local
# Docker directly - `docker buildx build --load` only supports a single platform at a time.
#
# Usage: ./docker-build.sh [tag]     (tag defaults to "latest")
#
# You must be logged in first: docker login

TAG="${1:-latest}"
IMAGE="kl3mta3/librariann"
outputFolder='_output'
BUILDER_NAME='librariann-builder'

ProgressStart()
{
    echo "Start '$1'"
}

ProgressEnd()
{
    echo "Finish '$1'"
}

BuildUI()
{
    ProgressStart 'Building UI'
    echo 'Removing old wwwroot'
    rm -rf Librariann.Server/wwwroot/*
    cd UI/Web/ || exit
    echo 'Installing web dependencies'
    npm ci
    echo 'Building UI'
    npm run prod
    echo 'Copying back to Librariann wwwroot'
    mkdir -p ../../Librariann.Server/wwwroot
    cp -R dist/browser/* ../../Librariann.Server/wwwroot
    cd ../../ || exit
    ProgressEnd 'Building UI'
}

Build()
{
    local RID="$1"

    ProgressStart "Build for $RID"

    slnFile=Librariann.sln

    dotnet clean $slnFile -c Release

    dotnet msbuild -restore $slnFile -p:Configuration=Release -p:Platform="Any CPU" -p:RuntimeIdentifiers=$RID

    ProgressEnd "Build for $RID"
}

Package()
{
    local runtime="$1"
    local lOutputFolder=../_output/"$runtime"/Librariann

    ProgressStart "Creating $runtime Package"

    cd Librariann.Server
    dotnet publish -c Release --no-restore --self-contained --runtime $runtime -o "$lOutputFolder"

    echo "Removing EF Core design-time folders"
    rm -rf "$lOutputFolder"/BuildHost-net472
    rm -rf "$lOutputFolder"/BuildHost-netcore

    echo "Removing cache-long from config"
    rm -rf "$lOutputFolder"/config/cache-long

    echo "Copying LICENSE"
    cp ../LICENSE "$lOutputFolder"/LICENSE.txt

    echo "Renaming Librariann.Server -> Librariann"
    mv "$lOutputFolder"/Librariann.Server "$lOutputFolder"/Librariann

    echo "Creating tar"
    cd ../$outputFolder/"$runtime"/
    tar -czvf ../librariann-$runtime.tar.gz Librariann
    cd ../../

    ProgressEnd "Creating $runtime Package"
}

EnsureBuildx()
{
    ProgressStart 'Ensuring a multi-arch buildx builder exists'
    if ! docker buildx inspect "$BUILDER_NAME" > /dev/null 2>&1; then
        echo "No dedicated builder found - installing QEMU emulation and creating one"
        docker run --privileged --rm tonistiigi/binfmt --install all
        docker buildx create --name "$BUILDER_NAME" --use
    else
        docker buildx use "$BUILDER_NAME"
    fi
    ProgressEnd 'Ensuring a multi-arch buildx builder exists'
}

dir=$PWD

if [ -d _output ]
then
    rm -r _output/
fi

BuildUI

Build "linux-x64"
Package "linux-x64"
cd "$dir"

Build "linux-arm"
Package "linux-arm"
cd "$dir"

Build "linux-arm64"
Package "linux-arm64"
cd "$dir"

EnsureBuildx

ProgressStart "Building and pushing $IMAGE:$TAG"
docker buildx build -t "$IMAGE:$TAG" --platform linux/amd64,linux/arm/v7,linux/arm64 . --push
ProgressEnd "Building and pushing $IMAGE:$TAG"

echo "Pushed $IMAGE:$TAG"
