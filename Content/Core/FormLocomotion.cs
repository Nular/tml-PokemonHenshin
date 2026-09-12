using System;
using Terraria;

namespace PokemonHenshin.Content.Core
{
	/// <summary>
	/// 变身移动动画状态。本迭代只用 Idle/Run；Jump/Fall/Swim 预留，解析时回退到 Run。
	/// </summary>
	public enum FormLocomotionState : byte
	{
		Idle = 0,
		Run = 1,
		Jump = 2,
		Fall = 3,
		Swim = 4
	}

	/// <summary>
	/// 单套横条精灵表 clip。移动片资源须统一朝向（朝右 → <see cref="FacesLeft"/>=false）。
	/// </summary>
	public sealed class FormAnimClip
	{
		public string TexturePath { get; init; }
		public int FrameCount { get; init; }
		public int FrameWidth { get; init; }
		public int FrameHeight { get; init; }

		/// <summary>贴图默认朝左时为 true（与静帧 FormDefinition.TextureFacesLeft 同语义）。</summary>
		public bool FacesLeft { get; init; }

		/// <summary>每帧持续 tick；长度须等于 <see cref="FrameCount"/>。缺省时用 <see cref="DefaultTicksPerFrame"/>。</summary>
		public int[] DurationsTicks { get; init; }

		public int DefaultTicksPerFrame { get; init; } = 4;

		public int TicksForFrame(int frame)
		{
			if (FrameCount <= 0)
				return Math.Max(1, DefaultTicksPerFrame);
			int i = ((frame % FrameCount) + FrameCount) % FrameCount;
			if (DurationsTicks != null && i < DurationsTicks.Length && DurationsTicks[i] > 0)
				return DurationsTicks[i];
			return Math.Max(1, DefaultTicksPerFrame);
		}
	}

	/// <summary>
	/// 形态可选移动动画。缺省 null 时 Overlay 走单帧 + bob。
	/// 回退链：Jump/Fall/Swim → Run → Idle。
	/// </summary>
	public sealed class FormLocomotionSpec
	{
		/// <summary>统一目标画高（像素），对齐现役 Forms 静帧 64。</summary>
		public const float TargetDrawHeight = 64f;

		public const float MoveVelocityThreshold = 0.1f;

		/// <summary>此 |vx| 下 Run 按素材原速播放；更快则缩短每帧 tick。</summary>
		public const float RunAnimRefSpeed = 3f;

		public const float RunAnimMinSpeedScale = 0.6f;
		public const float RunAnimMaxSpeedScale = 2.25f;

		public FormAnimClip Idle { get; init; }
		public FormAnimClip Run { get; init; }

		/// <summary>地面 Run：横向越快，播放倍率越高（缩短帧时长）。</summary>
		public static float RunPlaybackScale(float absVelocityX)
		{
			float scale = absVelocityX / RunAnimRefSpeed;
			return Math.Clamp(scale, RunAnimMinSpeedScale, RunAnimMaxSpeedScale);
		}

		/// <summary>预留；无素材时 GetClip 回退到 Run。</summary>
		public FormAnimClip Jump { get; init; }
		public FormAnimClip Fall { get; init; }
		public FormAnimClip Swim { get; init; }

		public static FormLocomotionState ResolveState(Player player)
		{
			if (player == null)
				return FormLocomotionState.Idle;

			// 游泳语义：水中且竖直速度非零。无独立 Swim 素材时 GetClip → Run。
			if (player.wet && player.velocity.Y != 0f && !player.mount.Active)
				return FormLocomotionState.Swim;

			bool airborne = player.velocity.Y != 0f;
			if (airborne)
				return player.velocity.Y < 0f ? FormLocomotionState.Jump : FormLocomotionState.Fall;

			if (Math.Abs(player.velocity.X) > MoveVelocityThreshold)
				return FormLocomotionState.Run;

			return FormLocomotionState.Idle;
		}

		public FormAnimClip GetClip(FormLocomotionState state)
		{
			FormAnimClip clip = state switch
			{
				FormLocomotionState.Idle => Idle,
				FormLocomotionState.Run => Run,
				FormLocomotionState.Jump => Jump,
				FormLocomotionState.Fall => Fall,
				FormLocomotionState.Swim => Swim,
				_ => null
			};

			// 本迭代 Jump/Fall/Swim 无独立素材 → Run → Idle
			if (clip == null && state is FormLocomotionState.Jump or FormLocomotionState.Fall or FormLocomotionState.Swim)
				clip = Run;
			if (clip == null)
				clip = Run;
			if (clip == null)
				clip = Idle;
			return clip;
		}
	}
}
