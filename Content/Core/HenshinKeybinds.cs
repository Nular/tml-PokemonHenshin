using Terraria.ModLoader;

namespace PokemonHenshin.Content.Core
{
	public sealed class HenshinKeybinds : ModSystem
	{
		public static ModKeybind Ultimate { get; private set; }

		public override void Load()
		{
			Ultimate = KeybindLoader.RegisterKeybind(Mod, "HenshinUltimate", "Mouse3");
		}

		public override void Unload()
		{
			Ultimate = null;
		}
	}
}
