#!/bin/bash
set -e

# https://medium.com/@Drew_Stokes/bash-argument-parsing-54f3b81a6a8f
PARAMS=""
while (( "$#" )); do
  case "$1" in
    -d|--dir)
      targetDir=$2
      shift 2
      ;;
    -p|--platform)
      platform=$2
      shift 2
      ;;
    -v|--version)
      version=$2
      shift 2
      ;;
    --) # end argument parsing
      shift
      break
      ;;
    -*|--*=) # unsupported flags
      echo "Error: Unsupported flag $1" >&2
      exit 1
      ;;
    *) # preserve positional arguments
      PARAMS="$PARAMS $1"
      shift
      ;;
  esac
done
# set positional arguments in their proper place
eval set -- "$PARAMS"

PLATFORM_NAME="$1"
VERSION="$2"

fileName="$PLATFORM_NAME-$VERSION.tar.gz"
platformDir="/opt/$PLATFORM_NAME"

if [ -z "$targetDir" ]; then
    targetDir="$platformDir/$VERSION"
fi
#--------------------------------

platform="$1"
version="$2"
installationParentDir="$3"

fullInstallationPath="$installationParentDir/$version"
PLATFORM_SETUP_START=$SECONDS
echo
echo "Downloading and extracting '$platform' version '$version' to '$fullInstallationPath'..."
rm -rf "$fullInstallationPath"
mkdir -p "$fullInstallationPath"
cd "$fullInstallationPath"
PLATFORM_BINARY_DOWNLOAD_START=$SECONDS
curl \
    -D headers.txt \
    -SL "https://oryxsdksdev.blob.core.windows.net/python/python-3.8.3.tar.gz" \
    --output 3.8.3.tar.gz \
    >/dev/null 2>&1
PLATFORM_BINARY_DOWNLOAD_ELAPSED_TIME=$(($SECONDS - $PLATFORM_BINARY_DOWNLOAD_START))
echo "Downloaded in $PLATFORM_BINARY_DOWNLOAD_ELAPSED_TIME sec(s)."
echo Verifying checksum...
headerName="x-ms-meta-checksum"
checksumHeader=$(cat headers.txt | grep -i $headerName: | tr -d '\r')
checksumHeader=$(echo $checksumHeader | tr '[A-Z]' '[a-z]')
checksumValue=${checksumHeader#"$headerName: "}
rm -f headers.txt
echo "$checksumValue 3.8.3.tar.gz" | sha512sum -c - >/dev/null 2>&1
echo Extracting contents...
tar -xzf 3.8.3.tar.gz -C .
rm -f 3.8.3.tar.gz
PLATFORM_SETUP_ELAPSED_TIME=$(($SECONDS - $PLATFORM_SETUP_START))
echo "Done in $PLATFORM_SETUP_ELAPSED_TIME sec(s)."
echo
echo > /tmp/oryx/platforms/python/3.8.3/.oryx-sdkdownload-sentinel