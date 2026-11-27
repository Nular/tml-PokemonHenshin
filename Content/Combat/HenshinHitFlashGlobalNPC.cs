using System;
using Microsoft.Xna.Framework;
using PokemonHenshin.Content.Combat.Moves;
using PokemonHenshin.Content.Damage;
using Terraria;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Combat
{
	/// <summary>
	/// NPC 受击白闪。只认本模伤害（<c>hit.DamageType</c> 计为 <see cref="HenshinDamage"/>），别的武器 / 熔岩 / 城镇 NPC 不闪。
	/// HitEffect 在服务器与每个客户端处理 StrikeNPC 时都会跑，包 28 带 DamageType（tML 1.4.4 NetMessage 补丁），所以所有端都有基础白闪；
	/// 主人端再经 <see cref="HenshinHitFeel.Flash"/> 按档位加长加亮。只改绘制色，不加贴图、不发包。
	/// 计数在 PostAI 递减（随游戏 tick，不随帧率）。
	/// <para>与睡眠改色的顺序：<c>HenshinNpcGlobalNPC.DrawEffects</c> 对睡眠 NPC 强制 (255,255,255,100)。
	/// 本类只向白色插值并保留原 Alpha，无论两者谁先执行，睡眠 NPC 结果都是白色 A=100，不会互相打架。</para>
	/// <para>shortcut: 灾厄部分 Boss 自绘时不用传入的 drawColor，看不到白闪；要支持需逐个 Boss 适配，暂不做。</para>
	/// </summary>
	public sealed class HenshinHitFlashGlobalNPC : GlobalNPC
	{
		/// <summary>所有端基础白闪：6 tick，强度 0.55（= Light 档）。</summary>
		public const int BaseTicks = 6;
		public const float BasePower = 0.55f;

		public override bool InstancePerEntity => true;

		private int _left;
		private int _total;
		private float _power;

		/// <summary>开始 / 加强白闪：只会变长变亮，不会被弱的覆盖。</summary>
		public void Flash(int ticks, float power)
		{
			if (ticks <= 0 || power <= 0f)
				return;
			float cur = Current;
			if (cur >= power && _left >= ticks)
				return;
			_total = Math.Max(ticks, _left);
			_left = _total;
			_power = Math.Max(power, cur);
		}

		/// <summary>当前白闪强度：先亮后快速收（平方衰减）。</summary>
		private float Current
		{
			get
			{
				if (_left <= 0 || _total <= 0)
					return 0f;
				float k = _left / (float)_total;
				return _power * k * k;
			}
		}

		public override void HitEffect(NPC npc, NPC.HitInfo hit)
		{
			if (hit.DamageType == null || !hit.DamageType.CountsAsClass(HenshinDamage.Instance))
				return;
			Flash(BaseTicks, BasePower);
		}

		public override void PostAI(NPC npc)
		{
			if (_left > 0)
				_left--;
		}

		public override void DrawEffects(NPC npc, ref Color drawColor)
		{
			float k = Current;
			if (k <= 0.01f)
				return;
			byte a = drawColor.A;
			drawColor = Color.Lerp(drawColor, Color.White, MathHelper.Clamp(k, 0f, 1f));
			drawColor.A = a;
		}
	}
}
