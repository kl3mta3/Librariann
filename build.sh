#! /bin/bash
set -e

outputFolder='_output'

CheckRequirements()
{
    if ! command -v npm &> /dev/null
    then
        echo "Warning!!! npm not found, it is required for building Librariann!"
    fi
    if ! command -v dotnet &> /dev/null
    then
        echo "Warning!!! dotnet not found, it is required for building Librariann!"
    fi
}

ProgressStart()
{
    echo "Start '$1'"
}

ProgressEnd()
{
    echo "Finish '$1'"
}


Build()
{
    ProgressStart 'Build'

    rm -rf $outputFolder

    slnFile=Librariann.sln

    dotnet clean $slnFile -c Release

    if [[ -z "$RID" ]];
    then
        dotnet msbuild -restore $slnFile -p:Configuration=Release -p:Platform="Any CPU"
    else
        dotnet msbuild -restore $slnFile -p:Configuration=Release -p:Platform="Any CPU" -p:RuntimeIdentifiers=$RID
    fi

    ProgressEnd 'Build'
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

Package()
{
    local runtime="$1"
    local lOutputFolder=../_output/"$runtime"/Librariann

    ProgressStart "Creating $runtime Package"

    # TODO: Use no-restore? Because Build should have already done it for us
    echo "Building"
    cd Librariann.Server
    echo dotnet publish -c Release --self-contained --runtime $runtime -o "$lOutputFolder"
    dotnet publish -c Release --self-contained --runtime $runtime -o "$lOutputFolder"

    echo "Recopying wwwroot due to bug"
    cp -R ./wwwroot/* $lOutputFolder/wwwroot

    echo "Removing EF Core design-time folders"
    rm -rf "$lOutputFolder"/BuildHost-net472
    rm -rf "$lOutputFolder"/BuildHost-netcore

    echo "Removing cache-long from config"
    rm -rf "$lOutputFolder"/config/cache-long

    echo "Copying LICENSE"
    cp ../LICENSE "$lOutputFolder"/LICENSE.txt

    echo "Renaming Librariann.Server -> Librariann"
    if [ $runtime == "win-x64" ] || [ $runtime == "win-x86" ]
    then
        mv "$lOutputFolder"/Librariann.Server.exe "$lOutputFolder"/Librariann.exe
    else
        mv "$lOutputFolder"/Librariann.Server "$lOutputFolder"/Librariann
    fi

    mkdir -p $lOutputFolder/config
    echo "Copying appsettings.json"
    cp config/appsettings.json $lOutputFolder/config/appsettings-init.json

    echo "Creating tar"
    cd ../$outputFolder/"$runtime"/
    tar -czvf ../librariann-$runtime.tar.gz Librariann


    ProgressEnd "Creating $runtime Package"
}


RID="$1"

CheckRequirements
BuildUI
Build

dir=$PWD

if [[ -z "$RID" ]];
then
    Package "win-x64"
    cd "$dir"
    Package "win-x86"
    cd "$dir"
    Package "linux-x64"
    cd "$dir"
    Package "linux-arm"
    cd "$dir"
    Package "linux-arm64"
    cd "$dir"
    Package "linux-musl-x64"
    cd "$dir"
    Package "osx-x64"
    cd "$dir"
    Package "osx-arm64"
    cd "$dir"
else
    Package "$RID"
    cd "$dir"
fi
