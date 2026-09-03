using System.IO;
using PokemonHenshin.Content.Net;
using Terraria.ModLoader;

namespace PokemonHenshin
{
	/// <summary>
	/// Mod 入口。保持瘦身：只负责单例与网络包分发（dev-plan §2.4 / §3.11）。
	/// </summary>
	public class PokemonHenshinMod : Mod
	{
		public static PokemonHenshinMod Instance { get; private set; }

		public override void Load()
		{
			Instance = this;
		}

		public override void Unload()
		{
			Instance = null;
		}

		public override void HandlePacket(BinaryReader reader, int whoAmI)
		{
			HenshinNet.Handle(reader, whoAmI);
		}
	}
}
