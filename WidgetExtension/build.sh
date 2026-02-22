#!/bin/bash
set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_DIR="$SCRIPT_DIR"
BUILD_DIR="$PROJECT_DIR/build"
CONFIGURATION="${1:-Debug}"
SDK="${2:-iphonesimulator}"

echo "Building KmlRecorderWidget extension ($CONFIGURATION, $SDK)..."

xcodebuild \
    -project "$PROJECT_DIR/KmlRecorderWidget.xcodeproj" \
    -scheme "KmlRecorderWidgetExtension" \
    -configuration "$CONFIGURATION" \
    -sdk "$SDK" \
    -derivedDataPath "$BUILD_DIR" \
    CODE_SIGNING_ALLOWED=NO \
    -quiet

APPEX_PATH=$(find "$BUILD_DIR" -name "KmlRecorderWidgetExtension.appex" -type d | head -1)

if [ -z "$APPEX_PATH" ]; then
    echo "ERROR: Could not find KmlRecorderWidgetExtension.appex"
    exit 1
fi

echo "Built: $APPEX_PATH"
