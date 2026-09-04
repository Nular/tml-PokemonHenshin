using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PokemonHenshin.Content.PlayerState;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Visual
{
	/// <summary>变身时在屏幕右下角绘制大招能量条（仿怒气条位置与比例）。</summary>
	public sealed class UltimateEnergyUI : ModSystem
	{
		public override void PostDrawInterface(SpriteBatch spriteBatch)
		{
			Player player = Main.LocalPlayer;
			if (player == null || !player.active || player.dead)
				return;
			HenshinPlayer hp = player.GetModPlayer<HenshinPlayer>();
			if (!hp.IsTransformed)
				return;

			float pct = hp.UltimateEnergyMax <= 0f ? 0f : MathHelper.Clamp(hp.UltimateEnergy / hp.UltimateEnergyMax, 0f, 1f);

			const int barW = 100;
			const int barH = 12;
			int x = Main.screenWidth - barW - 46;
			int y = Main.screenHeight - 64;
			Rectangle outer = new(x - 1, y - 1, barW + 2, barH + 2);
			Rectangle bg = new(x, y, barW, barH);
			Rectangle fill = new(x + 1, y + 1, (int)((barW - 2) * pct), barH - 2);

			Texture2D pixel = TextureAssets.MagicPixel.Value;
			spriteBatch.Draw(pixel, outer, Color.Black * 0.75f);
			spriteBatch.Draw(pixel, bg, new Color(20, 24, 40) * 0.9f);
			Color bar = pct >= 0.999f ? new Color(255, 210, 70) : new Color(70, 140, 255);
			if (fill.Width > 0)
				spriteBatch.Draw(pixel, fill, bar * 0.95f);
		}
	}
}
