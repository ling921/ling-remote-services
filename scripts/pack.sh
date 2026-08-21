#!/usr/bin/env sh

# Tests the repository and creates all public NuGet packages.

set -eu

configuration="Release"
runtime_identifier=""
output_directory="artifacts"
version=""
skip_tests="false"

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
        --output)
            output_directory="$2"
            shift 2
            ;;
        --version)
            version="$2"
            shift 2
            ;;
        --skip-tests)
            skip_tests="true"
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

case "$output_directory" in
    /*)
        ;;
    *)
        output_directory="$repository_root/$output_directory"
        ;;
esac

if [ "$skip_tests" = "false" ]; then
    if [ -n "$runtime_identifier" ]; then
        sh "$script_directory/test.sh" --configuration "$configuration" --runtime "$runtime_identifier"
    else
        sh "$script_directory/test.sh" --configuration "$configuration"
    fi
fi

mkdir -p "$output_directory"
cd "$repository_root"

for project in \
    "src/Ling.RemoteServices/Ling.RemoteServices.csproj" \
    "src/Ling.RemoteServices.Client/Ling.RemoteServices.Client.csproj" \
    "src/Ling.RemoteServices.AspNetCore/Ling.RemoteServices.AspNetCore.csproj"
do
    set -- dotnet pack "$project" \
        --configuration "$configuration" \
        --output "$output_directory" \
        -p:NoWarn=NU5118

    if [ "$skip_tests" = "false" ]; then
        set -- "$@" --no-build --no-restore
    fi

    if [ -n "$version" ]; then
        set -- "$@" "-p:Version=$version"
    fi

    "$@"
done

echo "NuGet packages were written to '$output_directory'."
