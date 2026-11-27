using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Combat.Moves
{
	/// <summary>自制音效键。文件在 Assets/Sounds（程序化合成，见 SOURCES.md）；变体 Name1..NameN 由 SoundStyle 随机挑。</summary>
	internal enum SfxKey
	{
		HitLight,
		HitHeavy,
		HitUlt,
		Swing,
		SwingHeavy,
		BlastS,
		BlastM,
		BlastL,
		UltCharge,
		UltCast,
		FireCast,
		FireHit,
		WaterCast,
		WaterHit,
		GrassCast,
		GrassHit,
		ElecCast,
		ElecHit,
		RockCast,
		RockHit,
		PsyCast,
		PsyHit,
		GhostCast,
		GhostHit,
		DragonCast,
		DragonHit,
		SteelCast,
		SteelHit,
		WindCast,
		WindHit,
		PoisonHit,
		Count
	}

	/// <summary>
	/// 中央音效入口：键 → 自制 SoundStyle（每键显式 PitchVariance / MaxInstances / SoundLimitBehavior）。
	/// 接线规则（详见 Assets/TEMP_ASSETS/fx-feel/r2-sfx-fix.md §5）：
	/// 1. 施放音：放在招式弹 AI 首次运行（localAI 位防重），各端都跑；不要放 OnSpawn / Shoot / CanUseItem。
	///    放了自己的施放音的弹，在 SetStaticDefaults 里调 <see cref="ClaimCastSound"/>，之力就不再响 Item.UseSound / 共享 UltCast。
	/// 2. 大招共享 UltCast：由 HenshinAccGlobalProjectile 在大招根弹 PostAI 首见同步位时播，招式自己不用管。
	/// 3. 命中音：放在已同步的命中 FxProj 的 AI 首 tick（localAI 位防重），不要放 OnHitNPC（只有 owner 端跑），不要套 owner==myPlayer。
	/// 4. 多发（每 tick 一发 / 一次 12–64 发）用 <see cref="PlayCooldown"/>，冷却按端本地、按键分开。
	/// </summary>
	internal static class HenshinSfx
	{
		private const string Dir = "PokemonHenshin/Assets/Sounds/";
		private static readonly SoundStyle[] Styles = new SoundStyle[(int)SfxKey.Count];
		private static readonly SoundStyle[] Fallbacks = new SoundStyle[(int)SfxKey.Count];
		private static readonly string[][] AssetPaths = new string[(int)SfxKey.Count][];
		private static readonly uint[] LastPlayed = new uint[(int)SfxKey.Count];
		private static readonly HashSet<int> CastSoundOwners = new();

		static HenshinSfx()
		{
			const SoundLimitBehavior Replace = SoundLimitBehavior.ReplaceOldest;
			const SoundLimitBehavior Ignore = SoundLimitBehavior.IgnoreNew;
			// key, 文件名, 变体数(1 = 无数字后缀), 音量, 音高方差(总幅度), 同时最多实例, 超限行为, 资源缺失时的原版替身
			Def(SfxKey.HitLight, "HitLight", 3, 0.55f, 0.16f, 4, Replace, SoundID.Item10);
			Def(SfxKey.HitHeavy, "HitHeavy", 3, 0.75f, 0.12f, 3, Replace, SoundID.Item14);
			Def(SfxKey.HitUlt, "HitUlt", 1, 0.9f, 0.08f, 1, Replace, SoundID.Item14);
			Def(SfxKey.Swing, "Swing", 3, 0.5f, 0.18f, 3, Replace, SoundID.Item1);
			Def(SfxKey.SwingHeavy, "SwingHeavy", 2, 0.65f, 0.12f, 2, Replace, SoundID.Item1);
			Def(SfxKey.BlastS, "BlastS", 3, 0.6f, 0.15f, 4, Replace, SoundID.Item14);
			Def(SfxKey.BlastM, "BlastM", 2, 0.75f, 0.12f, 3, Replace, SoundID.Item14);
			Def(SfxKey.BlastL, "BlastL", 1, 0.9f, 0.08f, 2, Replace, SoundID.Item62);
			Def(SfxKey.UltCharge, "UltCharge", 1, 0.7f, 0.05f, 1, Ignore, SoundID.Item29);
			Def(SfxKey.UltCast, "UltCast", 2, 0.85f, 0.06f, 1, Replace, SoundID.Item20);
			Def(SfxKey.FireCast, "FireCast", 1, 0.55f, 0.14f, 3, Replace, SoundID.Item20);
			Def(SfxKey.FireHit, "FireHit", 2, 0.6f, 0.14f, 4, Replace, SoundID.Item74);
			Def(SfxKey.WaterCast, "WaterCast", 1, 0.55f, 0.14f, 3, Replace, SoundID.Item21);
			Def(SfxKey.WaterHit, "WaterHit", 2, 0.6f, 0.16f, 4, Replace, SoundID.Splash);
			Def(SfxKey.GrassCast, "GrassCast", 1, 0.55f, 0.14f, 3, Replace, SoundID.Item17);
			Def(SfxKey.GrassHit, "GrassHit", 2, 0.6f, 0.14f, 4, Replace, SoundID.Item10);
			Def(SfxKey.ElecCast, "ElecCast", 1, 0.5f, 0.14f, 3, Replace, SoundID.Item93);
			Def(SfxKey.ElecHit, "ElecHit", 2, 0.55f, 0.16f, 4, Replace, SoundID.Item94);
			Def(SfxKey.RockCast, "RockCast", 1, 0.6f, 0.12f, 3, Replace, SoundID.Item69);
			Def(SfxKey.RockHit, "RockHit", 2, 0.65f, 0.14f, 4, Replace, SoundID.Item14);
			Def(SfxKey.PsyCast, "PsyCast", 1, 0.5f, 0.1f, 3, Replace, SoundID.Item8);
			Def(SfxKey.PsyHit, "PsyHit", 2, 0.55f, 0.12f, 4, Replace, SoundID.Item10);
			Def(SfxKey.GhostCast, "GhostCast", 1, 0.55f, 0.1f, 3, Replace, SoundID.Item8);
			Def(SfxKey.GhostHit, "GhostHit", 2, 0.6f, 0.12f, 4, Replace, SoundID.Item10);
			Def(SfxKey.DragonCast, "DragonCast", 1, 0.6f, 0.12f, 3, Replace, SoundID.Item20);
			Def(SfxKey.DragonHit, "DragonHit", 2, 0.65f, 0.12f, 4, Replace, SoundID.Item14);
			Def(SfxKey.SteelCast, "SteelCast", 1, 0.5f, 0.12f, 3, Replace, SoundID.Item71);
			Def(SfxKey.SteelHit, "SteelHit", 2, 0.55f, 0.14f, 4, Replace, SoundID.Item52);
			Def(SfxKey.WindCast, "WindCast", 1, 0.55f, 0.16f, 3, Replace, SoundID.Item1);
			Def(SfxKey.WindHit, "WindHit", 2, 0.55f, 0.16f, 4, Replace, SoundID.Item10);
			Def(SfxKey.PoisonHit, "PoisonHit", 1, 0.6f, 0.16f, 4, Replace, SoundID.Item10);
		}

		private static void Def(SfxKey key, string name, int variants, float volume, float pitchVariance, int maxInstances, SoundLimitBehavior limit, SoundStyle fallback)
		{
			SoundStyle style = variants > 1 ? new SoundStyle(Dir + name, variants) : new SoundStyle(Dir + name);
			Styles[(int)key] = style with
			{
				Volume = volume,
				PitchVariance = pitchVariance,
				MaxInstances = maxInstances,
				SoundLimitBehavior = limit
			};
			// 替身沿用同一组限流参数，避免缺文件时反而叠音。
			Fallbacks[(int)key] = fallback with
			{
				Volume = volume,
				PitchVariance = pitchVariance,
				MaxInstances = maxInstances,
				SoundLimitBehavior = limit
			};
			// 与 SoundStyle(path, n) 的取名一致：path1..pathN（从 1 起）。
			string[] paths = new string[variants];
			for (int i = 0; i < variants; i++)
				paths[i] = variants > 1 ? Dir + name + (i + 1) : Dir + name;
			AssetPaths[(int)key] = paths;
		}

		/// <summary>客户端 Load 时调用：逐键逐变体查资源，缺任一个就记日志并整键换成原版替身。</summary>
		internal static void ValidateAssets(Mod mod)
		{
			int missing = 0;
			for (int k = 0; k < (int)SfxKey.Count; k++)
			{
				foreach (string path in AssetPaths[k])
				{
					// 等价于 ModContent.HasAsset(path)；直接问本模的内容源，不依赖 Load 期的模组表。
					if (mod.HasAsset(path.Substring(mod.Name.Length + 1)))
						continue;
					mod.Logger.Warn($"HenshinSfx: missing sound asset '{path}' for {(SfxKey)k}; falling back to a vanilla sound.");
					Styles[k] = Fallbacks[k];
					missing++;
					break;
				}
			}
			if (missing > 0)
				mod.Logger.Warn($"HenshinSfx: {missing} key(s) use vanilla fallback sounds.");
		}

		/// <summary>键对应的基础 SoundStyle（可再 with 改参数）。纯数据，服务器上也安全。</summary>
		public static SoundStyle Style(SfxKey key) => Styles[(int)key];

		/// <summary>在 pos 播放。volume 乘基础音量，pitch 加到基础音高；结果连同随机音高一起夹在 [-1, 1]。服务器直接返回。</summary>
		public static void Play(SfxKey key, Vector2 pos, float volume = 1f, float pitch = 0f)
		{
			if (Main.dedServ)
				return;
			SoundStyle style = Styles[(int)key];
			float room = 1f - style.PitchVariance * 0.5f;
			SoundEngine.PlaySound(style with
			{
				Volume = MathHelper.Clamp(style.Volume * volume, 0f, 1f),
				Pitch = MathHelper.Clamp(style.Pitch + pitch, -room, room)
			}, pos);
		}

		/// <summary>同一键 minTicks 内只响一次（本端本地、按键分开，同 HenshinTileImpact）。返回是否真的播了。</summary>
		public static bool PlayCooldown(SfxKey key, Vector2 pos, int minTicks = 4, float volume = 1f, float pitch = 0f)
		{
			if (Main.dedServ)
				return false;
			uint now = Main.GameUpdateCount;
			ref uint last = ref LastPlayed[(int)key];
			if (last != 0 && now - last < (uint)minTicks)
				return false;
			last = now == 0 ? 1u : now;
			Play(key, pos, volume, pitch);
			return true;
		}

		/// <summary>
		/// 声明此弹种在自己 AI 里播施放音。在该弹的 SetStaticDefaults 里调：<c>HenshinSfx.ClaimCastSound(Type);</c>
		/// 效果：技能 1/2 用它出招时之力 Item.UseSound = null；作为大招根弹时不再播共享 UltCast（要的话自己在 AI 里播）。
		/// </summary>
		public static void ClaimCastSound(int projectileType)
		{
			if (projectileType > ProjectileID.None)
				CastSoundOwners.Add(projectileType);
		}

		/// <summary>此弹种是否已声明自带施放音。数据由各弹 SetStaticDefaults 填入，各端一致。</summary>
		public static bool OwnsCastSound(int projectileType) => CastSoundOwners.Contains(projectileType);

		internal static void ClearClaims() => CastSoundOwners.Clear();
	}

	/// <summary>加载期自检自制音效资源（仅客户端）。</summary>
	internal sealed class HenshinSfxSystem : ModSystem
	{
		public override void Load()
		{
			if (!Main.dedServ)
				HenshinSfx.ValidateAssets(Mod);
		}

		public override void Unload() => HenshinSfx.ClearClaims();
	}
}
