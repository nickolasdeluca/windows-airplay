#!/usr/bin/env bash
set -euo pipefail
export PATH="/ucrt64/bin:/usr/bin:$PATH"
cd "$(dirname "$0")/.."
source_dir="$PWD/.cache/source/UxPlay-df67c212a433cf6dda3676dd40c097900d24e645"
cmake -S "$source_dir" -B "$PWD/.cache/receiver-build" -G Ninja -DCMAKE_BUILD_TYPE=Release -DNO_MARCH_NATIVE=ON -DDNSSD_INCLUDE_DIR="$PWD/.cache"
cmake --build "$PWD/.cache/receiver-build" --parallel 4
