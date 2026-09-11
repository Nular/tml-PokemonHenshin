using System.ComponentModel;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace PokemonHenshin.Content.Config
{
	/// <summary>
	/// 客户端偏好：变身属性面板位置等。拖动面板时通过 <see cref="ModConfig.SaveChanges"/> 落盘。
	/// </summary>
	public sealed class HenshinClientConfig : ModConfig
	{
		public static HenshinClientConfig Instance => ModContent.GetInstance<HenshinClientConfig>();

		public override ConfigScope Mode => ConfigScope.ClientSide;

		[DefaultValue(false)]
		public bool UseCustomPanelPos;

		[DefaultValue(0)]
		[Range(-4000, 8000)]
		public int PanelPosX;

		[DefaultValue(0)]
		[Range(-4000, 8000)]
		public int PanelPosY;

		public void SavePanelPos(int x, int y)
		{
			UseCustomPanelPos = true;
			PanelPosX = x;
			PanelPosY = y;
			SaveChanges(silent: true);
		}

		public void ResetPanelPos()
		{
			UseCustomPanelPos = false;
			PanelPosX = 0;
			PanelPosY = 0;
			SaveChanges(silent: true);
		}
	}
}
