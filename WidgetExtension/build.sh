#!/bin/bash
set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_DIR="$SCRIPT_DIR"
BUILD_DIR="$PROJECT_DIR/build"
CONFIGURATION="${1:-Debug}"
SDK="${2:-iphonesimulator}"

TEAM_ID="${3:-}"

echo "Building KmlRecorderWidget extension ($CONFIGURATION, $SDK)..."

SIGNING_ARGS="CODE_SIGNING_ALLOWED=NO"
EXTRA_ARGS=""
if [[ "$SDK" == "iphoneos" && -n "$TEAM_ID" ]]; then
    SIGNING_ARGS="CODE_SIGN_STYLE=Automatic DEVELOPMENT_TEAM=$TEAM_ID"
    EXTRA_ARGS="-allowProvisioningUpdates"
fi

xcodebuild \
    -project "$PROJECT_DIR/KmlRecorderWidget.xcodeproj" \
    -scheme "KmlRecorderWidgetExtension" \
    -configuration "$CONFIGURATION" \
    -sdk "$SDK" \
    -derivedDataPath "$BUILD_DIR" \
    $SIGNING_ARGS \
    $EXTRA_ARGS \
    -quiet

APPEX_PATH=$(find "$BUILD_DIR" -name "KmlRecorderWidgetExtension.appex" -type d | head -1)

if [ -z "$APPEX_PATH" ]; then
    echo "ERROR: Could not find KmlRecorderWidgetExtension.appex"
    exit 1
fi

echo "Built: $APPEX_PATH"
