#!/usr/bin/env bash
# Build PokemonHenshin.tmod via the tModLoader ModSources dev layout.
#
# tModLoader derives a mod's internal name from its source *folder* name, so we
# build through the "PokemonHenshin" symlink created by tools/cloud-agent-setup.sh
# rather than from the raw checkout directory (which would yield "workspace.tmod").
#
# Any extra arguments are forwarded to `dotnet build` (e.g. -c Release).
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
MODSRC="$HOME/Documents/My Games/Terraria/tModLoader/ModSources"
PROJ="$MODSRC/PokemonHenshin"

# Self-heal the symlink so the mod name is always "PokemonHenshin".
mkdir -p "$MODSRC"
ln -sfn "$REPO_ROOT" "$PROJ"

export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
dotnet build "$PROJ/PokemonHenshin.csproj" "$@"

echo "Output: $HOME/.local/share/Terraria/tModLoader/Mods/PokemonHenshin.tmod"
