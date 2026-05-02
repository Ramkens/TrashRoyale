#!/usr/bin/env bash
# Headless Android APK build using local Unity install
set -e

UNITY_VERSION="2022.3.55f1"
UNITY_BIN="${UNITY_BIN:-$HOME/Unity/Hub/Editor/${UNITY_VERSION}/Editor/Unity}"
PROJECT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
BUILD_OUT="${BUILD_OUTPUT_DIR:-$PROJECT_DIR/build/Android}"
LOG_FILE="${LOG_FILE:-$PROJECT_DIR/build/Android/build.log}"

mkdir -p "$BUILD_OUT"

if [ ! -x "$UNITY_BIN" ]; then
    echo "Unity not found at $UNITY_BIN" >&2
    exit 1
fi

export BUILD_OUTPUT_DIR="$BUILD_OUT"

# Detect Android SDK / NDK / JDK from Unity install
ANDROID_HOME="$HOME/Unity/Hub/Editor/${UNITY_VERSION}/Editor/Data/PlaybackEngines/AndroidPlayer/SDK"
ANDROID_NDK_HOME="$HOME/Unity/Hub/Editor/${UNITY_VERSION}/Editor/Data/PlaybackEngines/AndroidPlayer/NDK"
JAVA_HOME="$HOME/Unity/Hub/Editor/${UNITY_VERSION}/Editor/Data/PlaybackEngines/AndroidPlayer/OpenJDK"
export ANDROID_HOME ANDROID_NDK_HOME JAVA_HOME

echo "Project: $PROJECT_DIR"
echo "Unity: $UNITY_BIN"
echo "Output: $BUILD_OUT"

"$UNITY_BIN" \
  -batchmode \
  -nographics \
  -quit \
  -projectPath "$PROJECT_DIR" \
  -logFile "$LOG_FILE" \
  -executeMethod TrashRoyale.EditorTools.BuildScript.BuildAndroid \
  -buildTarget Android || true

EXIT_CODE=$?

echo "---- Last 80 lines of build log ----"
tail -80 "$LOG_FILE" || true

if ls "$BUILD_OUT"/*.apk >/dev/null 2>&1; then
    echo "BUILD OK"
    ls -la "$BUILD_OUT"
    exit 0
fi

echo "BUILD FAILED (exit $EXIT_CODE)"
exit ${EXIT_CODE:-1}
