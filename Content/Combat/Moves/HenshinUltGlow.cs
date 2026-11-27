using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace PokemonHenshin.Content.Combat.Moves
{
	/// <summary>
	/// 大招少量发光粒子 + 色光（光照第 2 轮，§4.15 大招例外）。纯画面：只在客户端跑，不进联机包。
	/// <list type="bullet">
	/// <item>粒子撒在效果外缘（0.8–1.1 倍半径），不撒在贴图中心；noGravity、速度 ≤ 1.5。</item>
	/// <item>每客户端、每 owner、每 tick 合计最多 <see cref="MaxPerOwnerPerTick"/> 颗（静态计数，<see cref="Main.GameUpdateCount"/> 变了就清零）。</item>
	/// <item>子弹多的大招传 <c>sampleN</c>：只有 <c>whoAmI % sampleN == 0</c> 的子弹发粒子和光。</item>
	/// <item>位置只用 <see cref="Projectile.Center"/> / ai[] / 已同步字段算（AGENTS.md 硬禁止 11），不要传 owner-only 私有字段。</item>
	/// <item>自带光的 dust（<see cref="SelfLights"/>）不再额外 AddLight，除非 <c>forceLight</c>。</item>
	/// </list>
	/// </summary>
	public static class HenshinUltGlow
	{
		public const int MaxPerOwnerPerTick = 2;

		private static uint _tick = uint.MaxValue;
		private static readonly byte[] Count = new byte[256]; // owner 0..255（255 = 无主/服务器）

		/// <summary>
		/// 原版 <c>Dust.UpdateDust</c> 里会自己 AddLight 的 dust（tML 1.4.4.9 反编译核实，见 fx-light2/a.md）。
		/// 只列本模可能用到的：火把类 6、59–65、135；宝石 86–92；暗影焰 27；电 226。
		/// </summary>
		public static bool SelfLights(int dustType)
			=> dustType == DustID.Torch
			|| (dustType >= DustID.BlueTorch && dustType <= 65) // 59–65：蓝/红/绿/紫/白/黄/恶魔火把
			|| dustType == DustID.IceTorch
			|| (dustType >= DustID.GemAmethyst && dustType <= 92) // 86–92：宝石（光 ∝ (0.6·scale)²，偏弱）
			|| dustType == DustID.Shadowflame
			|| dustType == DustID.Electric;

		/// <summary>
		/// 弹幕版：位置 = <see cref="Projectile.Center"/>，计数按 <see cref="Projectile.owner"/>，抽样按 <see cref="Entity.whoAmI"/>。在 AI 里每 tick 调。
		/// </summary>
		/// <returns>本次发出的粒子数（0–1）。</returns>
		public static int Emit(Projectile p, float radius, int dustType, Vector3 lightRgb, int everyTicks = 2,
			bool? selfLit = null, int sampleN = 1, bool forceLight = false, float dustScale = 1f)
		{
			if (Main.dedServ || p == null || !p.active)
				return 0;
			return Emit(p.Center, p.owner, p.whoAmI, radius, dustType, lightRgb, everyTicks, selfLit, sampleN, forceLight, dustScale);
		}

		/// <summary>
		/// 坐标版：<paramref name="center"/> 必须由同步数据算出（Center、ai[]、velocity、owner 的位置），
		/// <paramref name="sampleKey"/> 用于抽样与错峰（一般传 whoAmI）。
		/// </summary>
		/// <param name="radius">效果外缘半径（世界像素，下限 8）。粒子落在 0.8–1.1 倍处。</param>
		/// <param name="lightRgb">色光（每 tick 一盏，放在 center）。glow 大招 0.8–1.2，lit 大招 0.35–0.5。Vector3.Zero = 不加光。</param>
		/// <param name="everyTicks">每几 tick 发一颗（默认 2 = 0.5 颗/tick；持续光束可传 1）。</param>
		/// <param name="selfLit">null = 按 <see cref="SelfLights"/> 自动；true = 粒子自带光，不 AddLight；false = 每 tick AddLight。</param>
		/// <param name="sampleN">子弹抽样：只有 sampleKey % sampleN == 0 的实例发粒子与光（≤1 = 全发）。</param>
		/// <param name="forceLight">true = 即使粒子自带光也 AddLight（要更亮时）。</param>
		/// <param name="dustScale">粒子基准大小（再随机 0.9–1.2 倍）。宝石 dust 光很弱，可传 1.2–1.5。</param>
		/// <returns>本次发出的粒子数（0–1）。</returns>
		public static int Emit(Vector2 center, int owner, int sampleKey, float radius, int dustType, Vector3 lightRgb,
			int everyTicks = 2, bool? selfLit = null, int sampleN = 1, bool forceLight = false, float dustScale = 1f)
		{
			if (Main.dedServ)
				return 0;
			if (sampleN > 1 && Math.Abs(sampleKey) % sampleN != 0)
				return 0;

			bool dustLights = selfLit ?? SelfLights(dustType);
			if ((forceLight || !dustLights) && lightRgb != Vector3.Zero)
				Lighting.AddLight(center, lightRgb);

			everyTicks = Math.Max(1, everyTicks);
			if ((Main.GameUpdateCount + (uint)Math.Abs(sampleKey)) % (uint)everyTicks != 0)
				return 0;

			if (_tick != Main.GameUpdateCount)
			{
				_tick = Main.GameUpdateCount;
				Array.Clear(Count);
			}
			int o = owner & 255;
			if (Count[o] >= MaxPerOwnerPerTick)
				return 0;

			radius = Math.Max(8f, radius);
			float ang = Main.rand.NextFloat(MathHelper.TwoPi);
			Vector2 dir = ang.ToRotationVector2();
			Vector2 pos = center + dir * radius * Main.rand.NextFloat(0.8f, 1.1f);
			// 沿切向慢漂，略向外，≤ 1.5。
			Vector2 vel = dir.RotatedBy(MathHelper.PiOver2 * (Main.rand.NextBool() ? 1f : -1f)) * Main.rand.NextFloat(0.3f, 1.1f)
				+ dir * Main.rand.NextFloat(0.1f, 0.4f);
			Dust d = Dust.NewDustPerfect(pos, dustType, vel, 0, default, dustScale * Main.rand.NextFloat(0.9f, 1.2f));
			d.noGravity = true;
			Count[o]++;
			return 1;
		}
	}
}
