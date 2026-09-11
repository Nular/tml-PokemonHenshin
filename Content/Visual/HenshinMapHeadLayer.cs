using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PokemonHenshin.Content.Core;
using PokemonHenshin.Content.PlayerState;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Visual
{
	/// <summary>
	/// 变身时填入原版地图头像 RT（<see cref="Terraria.Graphics.Renderers.MapHeadRenderer"/>）。
	/// 必须是独立顶层 <see cref="IsHeadLayer"/>：Overlay 画在脚底且不是头图层，藏原版 Head 后 RT 会空。
	/// 不挂 AfterParent(Head)：藏 Head 时子层一起消失。虫洞药水点击仍走原版头像坐标。
	/// </summary>
	public sealed class HenshinMapHeadLayer : PlayerDrawLayer
	{
		/// <summary>
		/// 原版头像 RT 84×84（描边约占 8px）。按最长边缩到此像素，给队色描边留边。
		/// </summary>
		public const float TargetLongestSide = 60f;

		public override bool IsHeadLayer => true;

		public override Position GetDefaultPosition() => PlayerDrawLayers.AfterLastVanillaLayer;

		public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
		{
			if (!drawInfo.headOnlyRender || drawInfo.shadow != 0f)
				return false;
			Player player = drawInfo.drawPlayer;
			if (!player.active || player.dead || player.ghost)
				return false;
			return player.GetModPlayer<HenshinPlayer>().IsTransformed;
		}

		protected override void Draw(ref PlayerDrawSet drawInfo)
		{
			Player player = drawInfo.drawPlayer;
			FormDefinition form = player.GetModPlayer<HenshinPlayer>().CurrentForm;
			if (form == null)
				return;

			Texture2D texture = ModContent.Request<Texture2D>(form.TexturePath, AssetRequestMode.ImmediateLoad).Value;
			if (texture == null)
				return;

			int longest = Math.Max(texture.Width, texture.Height);
			if (longest <= 0)
				return;
			float scale = TargetLongestSide / longest;

			// ExampleMod 头图层坐标：HeadOnlySetup 把 Position 摆成「人头落在 RT 中心」，
			// Center 上移约一头高后减去 screenPosition，即 RT 内绘制点。
			Vector2 drawPos = drawInfo.Center + new Vector2(0f, -20f) - Main.screenPosition;
			drawPos.X = (int)drawPos.X;
			drawPos.Y = (int)drawPos.Y;

			bool flipX = (player.direction == 1) == form.TextureFacesLeft;
			SpriteEffects effects = flipX ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
			if (player.gravDir < 0f)
				effects |= SpriteEffects.FlipVertically;

			Color color = drawInfo.colorArmorHead;
			Vector2 origin = new(texture.Width / 2f, texture.Height / 2f);
			drawInfo.DrawDataCache.Add(new DrawData(texture, drawPos, null, color, drawInfo.rotation, origin, scale, effects));
		}
	}
}
