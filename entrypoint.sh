#! /bin/bash

# Removed - This is causing issues for Synology users
## Set default UID and GID for Librariann but allow overrides
#PUID=${PUID:-0}
#PGID=${PGID:-0}
#
## Add Librariann group if it doesn't already exist
#if [[ -z "$(getent group "$PGID" | cut -d':' -f1)" ]]; then
#    groupadd -o -g "$PGID" librariann
#fi
#
## Add Librariann user if it doesn't already exist
#if [[ -z "$(getent passwd "$PUID" | cut -d':' -f1)" ]]; then
#    useradd -o -u "$PUID" -g "$PGID" -d /librariann librariann
#fi

# https://www.libvips.org/API/current/developer-checklist.html#linux-memory-allocator
JEMALLOC_PATH=$(ldconfig -p | grep -m1 'libjemalloc.so.2' | awk '{print $NF}')
if [ -n "$JEMALLOC_PATH" ]; then
    export LD_PRELOAD="$JEMALLOC_PATH"
else
    echo "jemalloc not found, using default allocator. This may cause increased memory usage"
fi

#Checks if the config file exists, and creates it if it does not
if [ ! -f "/librariann/config/appsettings.json" ]; then
    echo "Librariann configuration file does not exist, copying from temp..."
    cp /tmp/config/appsettings.json /librariann/config/appsettings.json
    if [ -f "/librariann/config/appsettings.json" ]; then
        echo "Copy completed successfully, starting app..."
    else
        echo "Copy failed, check folder permissions. Exiting..."
        exit
    fi
fi

echo "Starting Librariann"
echo ls -l "/librariann/config/appsettings.json"

exec ./Librariann

#if [[ "$PUID" -eq 0 ]]; then
#    # Run as root
#    ./Librariann
#else
#    # Set ownership on config dir if running non-root and current ownership is different
#    if [[ ! "$(stat -c %u /librariann/config)" = "$PUID" ]]; then
#        echo "Specified PUID differs from Librariann config dir ownership, updating permissions now..."
#        if [[ ! "$(stat -c %g /librariann/config)" = "$PGID" ]]; then
#            chown -R "$PUID":"$PGID" /librariann/config
#        else
#            chown -R "$PUID" /librariann/config
#        fi
#
#    elif [[ ! "$(stat -c %g /librariann/config)" = "$PGID" ]]; then
#        echo "Specified PGID differs from Librariann config dir ownership, updating permissions now..."
#        chgrp -R "$PGID" /librariann/config
#    fi
#
#    # Run as non-root user
#    su -l librariann -c ./Librariann
#fi
