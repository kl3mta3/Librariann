#!/bin/bash

#Copies the correct version of Librariann into the image

set -xv

if [ "$TARGETPLATFORM" == "linux/amd64" ]
then
	tar xf /files/librariann-linux-x64.tar.gz -C /
elif [ "$TARGETPLATFORM" == "linux/arm/v7" ]
then
	tar xf /files/librariann-linux-arm.tar.gz -C /
elif [ "$TARGETPLATFORM" == "linux/arm64" ]
then
	tar xf /files/librariann-linux-arm64.tar.gz -C /
fi
