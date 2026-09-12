using System.ComponentModel;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace PokemonHenshin.Content.Config
{
	/// <summary>
	/// 客户端偏好：变身属性入口按钮位置等。拖动入口或面板时通过
	/// <see cref="ModConfig.SaveChanges"/> 落盘；面板始终贴在入口右侧。
	/// </summary>
	public sealed class HenshinClientConfig : ModConfig
	{
		public static HenshinClientConfig Instance => ModContent.GetInstance<HenshinClientConfig>();

		public override ConfigScope Mode => ConfigScope.ClientSide;

		[DefaultValue(false)]
		public bool UseCustomButtonPos;

		[DefaultValue(0)]
		[Range(-4000, 8000)]
		public int ButtonPosX;

		[DefaultValue(0)]
		[Range(-4000, 8000)]
		public int ButtonPosY;

		public void SaveButtonPos(int x, int y)
		{
			UseCustomButtonPos = true;
			ButtonPosX = x;
			ButtonPosY = y;
			SaveChanges(silent: true);
		}

		public void ResetButtonPos()
		{
			UseCustomButtonPos = false;
			ButtonPosX = 0;
			ButtonPosY = 0;
			SaveChanges(silent: true);
		}
	}
}
