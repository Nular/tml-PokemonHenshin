#!/usr/bin/env bash
# Idempotent Cloud Agent setup for building the PokemonHenshin tModLoader mod.
#
# Installs the pinned toolchain and reproduces the tModLoader "ModSources"
# development layout that the .csproj expects, so that a plain
#   dotnet build "$MODSRC/PokemonHenshin/PokemonHenshin.csproj"
# (see tools/build-mod.sh) produces a correctly named PokemonHenshin.tmod.
#
# Safe to run repeatedly: every step checks current state before acting.
set -euo pipefail

# --- Pinned versions (see README.md / AGENTS.md) --------------------------
DOTNET_CHANNEL="8.0"                 # tModLoader 2026.07 stable targets net8.0
TML_VERSION="v2026.07.3.0"           # 1.4.4.9 / 2026.07 stable

# --- Paths ----------------------------------------------------------------
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
DOTNET_DIR="$HOME/.dotnet"
TML_DIR="$HOME/tModLoader"
TML_SAVE="$HOME/.local/share/Terraria/tModLoader"
MODSRC="$HOME/Documents/My Games/Terraria/tModLoader/ModSources"

log() { printf '\033[1;36m[setup]\033[0m %s\n' "$*"; }

# --- 1. .NET 8 SDK --------------------------------------------------------
if [ -x "$DOTNET_DIR/dotnet" ] && "$DOTNET_DIR/dotnet" --version 2>/dev/null | grep -q '^8\.'; then
	log ".NET 8 SDK already present ($("$DOTNET_DIR/dotnet" --version))"
else
	log "Installing .NET $DOTNET_CHANNEL SDK -> $DOTNET_DIR"
	tmp_installer="$(mktemp)"
	curl -fsSL https://dot.net/v1/dotnet-install.sh -o "$tmp_installer"
	bash "$tmp_installer" --channel "$DOTNET_CHANNEL" --install-dir "$DOTNET_DIR"
	rm -f "$tmp_installer"
fi

# Make `dotnet` resolvable on the default PATH (used by the agent shell and by
# the nested `dotnet tModLoader.dll -build` packaging step).
if [ -w /usr/local/bin ] || command -v sudo >/dev/null 2>&1; then
	if command -v sudo >/dev/null 2>&1 && [ ! -w /usr/local/bin ]; then
		sudo ln -sfn "$DOTNET_DIR/dotnet" /usr/local/bin/dotnet
	else
		ln -sfn "$DOTNET_DIR/dotnet" /usr/local/bin/dotnet
	fi
fi
export PATH="$DOTNET_DIR:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1

# --- 2. tModLoader install (provides tModLoader.dll, Libraries/, targets) --
need_tml=1
if [ -f "$TML_DIR/tModLoader.dll" ] && [ "$(cat "$TML_DIR/.tml-version" 2>/dev/null)" = "$TML_VERSION" ]; then
	need_tml=0
fi
if [ "$need_tml" -eq 0 ]; then
	log "tModLoader $TML_VERSION already extracted at $TML_DIR"
else
	log "Downloading tModLoader $TML_VERSION"
	tmp_zip="$(mktemp --suffix=.zip)"
	curl -fL -o "$tmp_zip" \
		"https://github.com/tModLoader/tModLoader/releases/download/${TML_VERSION}/tModLoader.zip"
	rm -rf "$TML_DIR"
	mkdir -p "$TML_DIR"
	unzip -q -o "$tmp_zip" -d "$TML_DIR"
	rm -f "$tmp_zip"
	printf '%s\n' "$TML_VERSION" > "$TML_DIR/.tml-version"
fi

# --- 3. ModSources dev layout --------------------------------------------
# The .csproj imports tModLoader.targets from the ModSources folder and derives
# the mod's internal name from the source folder name. We recreate that layout
# by pointing a "PokemonHenshin" symlink at the checked-out repo.
mkdir -p "$MODSRC"
cat > "$MODSRC/tModLoader.targets" <<EOF
<Project ToolsVersion="14.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <!-- Cloud Agent dev targets: delegate to the extracted tModLoader install. -->
  <Import Project="$TML_DIR/tMLMod.targets" />
</Project>
EOF
ln -sfn "$REPO_ROOT" "$MODSRC/PokemonHenshin"
log "Linked $MODSRC/PokemonHenshin -> $REPO_ROOT"

# --- 4. Enable the mod for headless load checks ---------------------------
mkdir -p "$TML_SAVE/Mods"
if [ ! -f "$TML_SAVE/Mods/enabled.json" ] || ! grep -q '"PokemonHenshin"' "$TML_SAVE/Mods/enabled.json"; then
	printf '[\n  "PokemonHenshin"\n]\n' > "$TML_SAVE/Mods/enabled.json"
fi

# --- 5. Verify the toolchain by building once -----------------------------
log "Building PokemonHenshin to verify the toolchain"
dotnet build "$MODSRC/PokemonHenshin/PokemonHenshin.csproj"

log "Setup complete. Build with: bash tools/build-mod.sh"
