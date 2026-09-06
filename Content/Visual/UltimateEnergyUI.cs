using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PokemonHenshin.Content.PlayerState;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI;

namespace PokemonHenshin.Content.Visual
{
	/// <summary>变身时在角色脚下绘制大招能量条（世界坐标，跟镜头缩放）；右下角不再显示。</summary>
	public sealed class UltimateEnergyUI : ModSystem
	{
		public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
		{
			int index = layers.FindIndex(l => l.Name == "Vanilla: Resource Bars");
			if (index < 0)
				index = layers.FindIndex(l => l.Name == "Vanilla: Mouse Text");
			if (index < 0)
				index = layers.Count;

			layers.Insert(index, new LegacyGameInterfaceLayer(
				"PokemonHenshin: UltimateEnergy",
				delegate
				{
					DrawUnderPlayer(Main.spriteBatch);
					return true;
				},
				InterfaceScaleType.Game));
		}

		private static void DrawUnderPlayer(SpriteBatch spriteBatch)
		{
			Player player = Main.LocalPlayer;
			if (player == null || !player.active || player.dead)
				return;
			HenshinPlayer hp = player.GetModPlayer<HenshinPlayer>();
			if (!hp.IsTransformed)
				return;

			float pct = hp.UltimateEnergyMax <= 0f
				? 0f
				: MathHelper.Clamp(hp.UltimateEnergy / hp.UltimateEnergyMax, 0f, 1f);

			const int barW = 56;
			const int barH = 6;
			// 锚在脚底外侧（反重力时改到头顶外侧），跟角色走、不跟 HUD
			Vector2 feet = player.gravDir >= 0f
				? player.Bottom + new Vector2(0f, 8f)
				: player.Top - new Vector2(0f, 8f + barH);
			Vector2 center = feet + new Vector2(0f, player.gfxOffY) - Main.screenPosition;
			int x = (int)(center.X - barW * 0.5f);
			int y = (int)center.Y;
			Rectangle outer = new(x - 1, y - 1, barW + 2, barH + 2);
			Rectangle bg = new(x, y, barW, barH);
			Rectangle fill = new(x + 1, y + 1, (int)((barW - 2) * pct), barH - 2);

			Texture2D pixel = TextureAssets.MagicPixel.Value;
			spriteBatch.Draw(pixel, outer, Color.Black * 0.7f);
			spriteBatch.Draw(pixel, bg, new Color(20, 24, 40) * 0.9f);
			Color bar = pct >= 0.999f ? new Color(255, 210, 70) : new Color(70, 140, 255);
			if (fill.Width > 0)
				spriteBatch.Draw(pixel, fill, bar * 0.95f);
		}
	}
}
