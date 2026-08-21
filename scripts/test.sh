#!/usr/bin/env sh

# Builds and tests the repository, then verifies the Native AOT smoke application.

set -eu

configuration="Release"
runtime_identifier=""
skip_native_aot="false"

while [ "$#" -gt 0 ]; do
    case "$1" in
        --configuration)
            configuration="$2"
            shift 2
            ;;
        --runtime)
            runtime_identifier="$2"
            shift 2
            ;;
        --skip-native-aot)
            skip_native_aot="true"
            shift
            ;;
        *)
            echo "Unknown argument: $1" >&2
            exit 2
            ;;
    esac
done

script_directory=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
repository_root=$(CDPATH= cd -- "$script_directory/.." && pwd)
solution_path="$repository_root/Ling.RemoteServices.slnx"
smoke_project_path="$repository_root/tests/Ling.RemoteServices.NativeAotSmoke/Ling.RemoteServices.NativeAotSmoke.csproj"

detect_runtime_identifier() {
    operating_system=$(uname -s)
    architecture=$(uname -m)

    case "$architecture" in
        x86_64|amd64)
            architecture="x64"
            ;;
        arm64|aarch64)
            architecture="arm64"
            ;;
        *)
            echo "Native AOT smoke testing does not support architecture '$architecture'." >&2
            exit 1
            ;;
    esac

    case "$operating_system" in
        Linux)
            echo "linux-$architecture"
            ;;
        Darwin)
            echo "osx-$architecture"
            ;;
        MINGW*|MSYS*|CYGWIN*)
            echo "win-$architecture"
            ;;
        *)
            echo "Native AOT smoke testing is not supported on '$operating_system'." >&2
            exit 1
            ;;
    esac
}

cd "$repository_root"

dotnet restore "$solution_path"
dotnet build "$solution_path" --configuration "$configuration" --no-restore
dotnet test "$solution_path" --configuration "$configuration" --no-build --verbosity normal

if [ "$skip_native_aot" = "true" ]; then
    echo "Native AOT smoke verification was skipped."
    exit 0
fi

if [ -z "$runtime_identifier" ]; then
    runtime_identifier=$(detect_runtime_identifier)
fi

dotnet publish "$smoke_project_path" \
    --configuration "$configuration" \
    --runtime "$runtime_identifier" \
    --self-contained true \
    -p:TrimmerSingleWarn=false

executable_name="Ling.RemoteServices.NativeAotSmoke"

case "$runtime_identifier" in
    win-*)
        executable_name="$executable_name.exe"
        ;;
esac

executable_path="$repository_root/tests/Ling.RemoteServices.NativeAotSmoke/bin/$configuration/net8.0/$runtime_identifier/publish/$executable_name"

if [ ! -f "$executable_path" ]; then
    echo "The Native AOT executable was not found at '$executable_path'." >&2
    exit 1
fi

base_address="http://127.0.0.1:5199"
log_path="${TMPDIR:-/tmp}/ling-remote-services-aot-$$.log"
application_pid=""

cleanup() {
    if [ -n "$application_pid" ] && kill -0 "$application_pid" 2>/dev/null; then
        kill "$application_pid" 2>/dev/null || true
        wait "$application_pid" 2>/dev/null || true
    fi

    rm -f "$log_path"
}

trap cleanup EXIT INT TERM

"$executable_path" --urls "$base_address" >"$log_path" 2>&1 &
application_pid=$!
response_body=""
attempt=1

while [ "$attempt" -le 20 ]; do
    if ! kill -0 "$application_pid" 2>/dev/null; then
        echo "The Native AOT smoke application exited before it became ready." >&2
        cat "$log_path" >&2
        exit 1
    fi

    if response_body=$(curl --fail --silent --show-error "$base_address/client-smoke" 2>/dev/null); then
        break
    fi

    attempt=$((attempt + 1))
    sleep 1
done

case "$response_body" in
    *client-round-trip*)
        echo "Native AOT client and server round-trip succeeded for $runtime_identifier."
        ;;
    *)
        echo "The Native AOT round-trip returned an unexpected response: '$response_body'." >&2
        cat "$log_path" >&2
        exit 1
        ;;
esac
