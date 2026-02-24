#!/bin/bash
set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_DIR="$SCRIPT_DIR"
BUILD_DIR="$PROJECT_DIR/build"
CONFIGURATION="${1:-Debug}"
SDK="${2:-watchsimulator}"

TEAM_ID="${3:-}"

echo "Building KmlRecorderWatch app ($CONFIGURATION, $SDK)..."

SIGNING_ARGS="CODE_SIGNING_ALLOWED=NO"
EXTRA_ARGS=""
if [[ "$SDK" == "watchos" && -n "$TEAM_ID" ]]; then
    SIGNING_ARGS="CODE_SIGN_STYLE=Automatic DEVELOPMENT_TEAM=$TEAM_ID"
    EXTRA_ARGS="-allowProvisioningUpdates"
fi

xcodebuild \
    -project "$PROJECT_DIR/KmlRecorderWatch.xcodeproj" \
    -scheme "KmlRecorderWatch" \
    -configuration "$CONFIGURATION" \
    -sdk "$SDK" \
    -derivedDataPath "$BUILD_DIR" \
    $SIGNING_ARGS \
    $EXTRA_ARGS \
    -quiet

APP_PATH=$(find "$BUILD_DIR" -name "KmlRecorderWatch.app" -type d | head -1)

if [ -z "$APP_PATH" ]; then
    echo "ERROR: Could not find KmlRecorderWatch.app"
    exit 1
fi

echo "Built: $APP_PATH"
