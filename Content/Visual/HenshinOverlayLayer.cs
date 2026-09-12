using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PokemonHenshin.Content.Core;
using PokemonHenshin.Content.PlayerState;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Visual
{
	/// <summary>
	/// 变身 Overlay（需求 §2.3）：在玩家脚底居中绘制宝可梦贴图。
	/// 世界绘制时 <see cref="HenshinPlayer.HideDrawLayers"/> 只保留本层；地图头像见 <see cref="HenshinMapHeadLayer"/>。
	/// 有 <see cref="FormLocomotionSpec"/> 时采帧 + 统一身高；否则单帧 + bob。
	/// </summary>
	public sealed class HenshinOverlayLayer : PlayerDrawLayer
	{
		public override Position GetDefaultPosition() => PlayerDrawLayers.AfterLastVanillaLayer;

		public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
		{
			if (Main.gameMenu || drawInfo.headOnlyRender || drawInfo.shadow != 0f)
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

			FormLocomotionSpec loco = form.Locomotion;
			if (loco != null)
			{
				DrawLocomotion(ref drawInfo, player, mp, form, loco);
				return;
			}

			DrawStatic(ref drawInfo, player, mp, form);
		}

		private static void DrawStatic(ref PlayerDrawSet drawInfo, Player player, HenshinPlayer mp, FormDefinition form)
		{
			Texture2D texture = ModContent.Request<Texture2D>(form.TexturePath, AssetRequestMode.ImmediateLoad).Value;
			if (texture == null)
				return;

			RemoveForeignDraws(ref drawInfo, texture, form.ItemType);

			bool flipX = (player.direction == 1) == form.TextureFacesLeft;
			SpriteEffects effects = flipX ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

			float bob = 0f;
			bool airborne = player.velocity.Y != 0f;
			if (airborne)
				bob = -2f;
			else if (Math.Abs(player.velocity.X) > FormLocomotionSpec.MoveVelocityThreshold)
				bob = (float)Math.Sin(mp.TransformTicks * 0.35f) * 2f;

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

			Color color = drawInfo.colorArmorBody;
			drawInfo.DrawDataCache.Add(new DrawData(texture, drawPos, null, color, drawInfo.rotation, origin, 1f, effects));
		}

		private static void DrawLocomotion(
			ref PlayerDrawSet drawInfo, Player player, HenshinPlayer mp, FormDefinition form, FormLocomotionSpec loco)
		{
			FormAnimClip clip = loco.GetClip(mp.LocomotionState);
			if (clip == null || string.IsNullOrEmpty(clip.TexturePath) || clip.FrameCount <= 0)
			{
				DrawStatic(ref drawInfo, player, mp, form);
				return;
			}

			Texture2D texture = ModContent.Request<Texture2D>(clip.TexturePath, AssetRequestMode.ImmediateLoad).Value;
			if (texture == null)
				return;

			// 清掉静帧物品贴图与动画 sheet 的外来绘制。
			RemoveForeignDraws(ref drawInfo, texture, form.ItemType);
			Texture2D staticTex = ModContent.Request<Texture2D>(form.TexturePath, AssetRequestMode.ImmediateLoad).Value;
			if (staticTex != null && !ReferenceEquals(staticTex, texture))
				RemoveForeignDraws(ref drawInfo, staticTex, 0);

			int frame = mp.LocomotionFrame;
			if (frame < 0 || frame >= clip.FrameCount)
				frame = 0;

			Rectangle source = new(frame * clip.FrameWidth, 0, clip.FrameWidth, clip.FrameHeight);
			float scale = FormLocomotionSpec.TargetDrawHeight / Math.Max(1, clip.FrameHeight);

			bool flipX = (player.direction == 1) == clip.FacesLeft;
			SpriteEffects effects = flipX ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

			Vector2 origin;
			float anchorY;
			if (player.gravDir >= 0f)
			{
				origin = new Vector2(clip.FrameWidth / 2f, clip.FrameHeight);
				anchorY = drawInfo.Position.Y + player.height;
			}
			else
			{
				origin = new Vector2(clip.FrameWidth / 2f, 0f);
				anchorY = drawInfo.Position.Y;
				effects |= SpriteEffects.FlipVertically;
			}

			Vector2 drawPos = new(
				(int)(drawInfo.Position.X - Main.screenPosition.X + player.width / 2f),
				(int)(anchorY - Main.screenPosition.Y));

			Color color = drawInfo.colorArmorBody;
			drawInfo.DrawDataCache.Add(new DrawData(texture, drawPos, source, color, drawInfo.rotation, origin, scale, effects));
		}

		private static void RemoveForeignDraws(ref PlayerDrawSet drawInfo, Texture2D formTexture, int itemType)
		{
			Texture2D itemTexture = itemType > 0 && TextureAssets.Item[itemType].IsLoaded ? TextureAssets.Item[itemType].Value : null;
			var cache = drawInfo.DrawDataCache;
			for (int i = cache.Count - 1; i >= 0; i--)
			{
				Texture2D tex = cache[i].texture;
				if (ReferenceEquals(tex, formTexture) || (itemTexture != null && ReferenceEquals(tex, itemTexture)))
					cache.RemoveAt(i);
			}
		}
	}
}
