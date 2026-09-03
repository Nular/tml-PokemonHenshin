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
	/// 变身 Overlay（需求 §2.3，dev-plan §4.3）：在玩家脚底居中绘制宝可梦贴图。
	/// 原版各层由 <see cref="HenshinPlayer.HideDrawLayers"/> 隐藏；未变身时本层不可见，退出同 tick 无残留。
	/// M0 单帧：移动时轻微浮动、离地时微抬，按朝向翻转。
	/// </summary>
	public sealed class HenshinOverlayLayer : PlayerDrawLayer
	{
		public override Position GetDefaultPosition() => PlayerDrawLayers.AfterLastVanillaLayer;

		public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
		{
			if (Main.gameMenu || drawInfo.shadow != 0f)
				return false;
			Player player = drawInfo.drawPlayer;
			if (!player.active || player.dead || player.ghost)
				return false;
			return player.GetModPlayer<HenshinPlayer>().IsTransformed;
		}

		protected override void Draw(ref PlayerDrawSet drawInfo)
		{
			Player player = drawInfo.drawPlayer;
			HenshinPlayer mp = player.GetModPlayer<HenshinPlayer>();
			FormDefinition form = mp.CurrentForm;
			if (form == null)
				return;

			Texture2D texture = ModContent.Request<Texture2D>(form.TexturePath, AssetRequestMode.ImmediateLoad).Value;
			if (texture == null)
				return;

			// 朝向：原图朝左时，玩家朝右需翻转。
			bool flipX = (player.direction == 1) == form.TextureFacesLeft;
			SpriteEffects effects = flipX ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

			// 简易动画：地面移动上下浮动；空中微抬。
			float bob = 0f;
			bool airborne = player.velocity.Y != 0f;
			if (airborne)
				bob = -2f;
			else if (Math.Abs(player.velocity.X) > 0.1f)
				bob = (float)Math.Sin(mp.TransformTicks * 0.35f) * 2f;

			// 锚点：脚底中心（重力反转时改为头顶）。drawInfo.Position 已含 gfxOffY，勿重复加。
			Vector2 origin;
			float anchorY;
			if (player.gravDir >= 0f)
			{
				origin = new Vector2(texture.Width / 2f, texture.Height);
				anchorY = drawInfo.Position.Y + player.height;
			}
			else
			{
				origin = new Vector2(texture.Width / 2f, 0f);
				anchorY = drawInfo.Position.Y;
				effects |= SpriteEffects.FlipVertically;
				bob = -bob;
			}

			Vector2 drawPos = new(
				(int)(drawInfo.Position.X - Main.screenPosition.X + player.width / 2f),
				(int)(anchorY - Main.screenPosition.Y + bob));

			// colorArmorBody 已含光照 / 隐身 / 受击闪烁，遵循原版隐身规则。
			Color color = drawInfo.colorArmorBody;

			drawInfo.DrawDataCache.Add(new DrawData(texture, drawPos, null, color, drawInfo.rotation, origin, 1f, effects));
		}
	}
}
