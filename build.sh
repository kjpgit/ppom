#!/bin/bash
set -e
cd `dirname $0`

if [ -z "$1" ]; then
    echo "build dir required"
    exit 1
fi

DATA_DIR="$1"
BUILD_DIR="$2"

# Rename a file to HASH-file.extension
# We always prefer to rename files on change instead of mutating them.
set_md5_hash() {
    local checksum=`md5sum $1 | awk '{print $1}'`
    local newname="$checksum-`basename $1`"
    mv "$1" "`dirname $1`/$newname"
}

mkdir -p ${BUILD_DIR} ${BUILD_DIR}/shop

# Static assets
rm -rf ${BUILD_DIR}/include
cp -a assets/include ${BUILD_DIR}/include

lessc ${BUILD_DIR}/include/main.less ${BUILD_DIR}/include/main.css
rm ${BUILD_DIR}/include/main.less

cp -a assets/misc/favicon.ico $BUILD_DIR
cp -a assets/misc/BingSiteAuth.xml $BUILD_DIR
cp -a assets/misc/google88ea80b7546f5a5c.html $BUILD_DIR

# Misc stuff not in the repo yet
cp ${DATA_DIR}/sidebar.png ${BUILD_DIR}/include
convert -thumbnail 640 ${DATA_DIR}/services/services.jpg ${BUILD_DIR}/include/services-medium.jpg

# Blog Images / Files
for year in `ls ${DATA_DIR}/blog/`; do
    mkdir -p ${BUILD_DIR}/blog/$year/
    cp -a ${DATA_DIR}/blog/$year/images ${BUILD_DIR}/blog/$year/
    cp -a ${DATA_DIR}/blog/$year/downloads ${BUILD_DIR}/blog/$year/
done


# Generate site using C# program
# Change directory so it finds its templates
(cd source/SiteBuilder && dotnet run -- ${DATA_DIR} ${BUILD_DIR})


# Now move files to their hashed names
set_md5_hash ${BUILD_DIR}/include/main.css
set_md5_hash ${BUILD_DIR}/include/mycart.js


# Check for errors
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
