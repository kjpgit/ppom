#!/bin/bash
set -e
cd `dirname $0`

if [ -z "$1" ]; then
    echo "build dir required"
    exit 1
fi

export BUILD_DIR="$1"
export DATA_DIR=~/jgit/data

mkdir -p ${BUILD_DIR} ${BUILD_DIR}/shop


# Static assets
rm -rf ${BUILD_DIR}/include
cp -a include ${BUILD_DIR}/include

lessc ${BUILD_DIR}/include/main.less ${BUILD_DIR}/include/main.css
rm ${BUILD_DIR}/include/main.less

cp -a misc/favicon.ico $BUILD_DIR
cp -a misc/BingSiteAuth.xml $BUILD_DIR
cp -a misc/google88ea80b7546f5a5c.html $BUILD_DIR

# Misc stuff not in the repo yet
cp ${DATA_DIR}/sidebar.png ${BUILD_DIR}/include
convert -thumbnail 640 ${DATA_DIR}/services/services.jpg ${BUILD_DIR}/include/services-medium.jpg


# Check
if fgrep -r -I "{{" $BUILD_DIR --exclude-dir pswp;  then
    echo "Error: unhandled macros"
    exit 1
fi

if fgrep -r -I "#NAME" $BUILD_DIR --exclude-dir pswp;  then
    echo "Error: bad spreadsheet"
    exit 1
fi

if fgrep -r -I "Lorem" $BUILD_DIR ; then
    echo "WARNING: Lorem Ipsum text exists"
    exit 1
fi
