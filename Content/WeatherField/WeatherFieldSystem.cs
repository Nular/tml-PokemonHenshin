using Microsoft.Xna.Framework;
using PokemonHenshin.Content.Net;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.WeatherField
{
	public enum WeatherTag : byte
	{
		Rain = 0,
		Thunder = 1,
		Snow = 2,
		Sand = 3
	}

	public sealed class WeatherFieldState
	{
		public int Id;
		public Vector2 Center;
		public float Radius;
		public int TimeLeft;
		public WeatherTag Tag;
		public byte Owner;
	}

	/// <summary>服务端权威天气场（需求 §7.1）。</summary>
	public sealed class WeatherFieldSystem : ModSystem
	{
		public const float DefaultRadius = 24f * 16f;
		public const int DefaultDuration = 15 * 60;
		public const int SameTagCooldown = 30 * 60;

		private static readonly List<WeatherFieldState> fields = new();
		private static int nextId = 1;
		private static readonly Dictionary<(byte owner, WeatherTag tag), int> cooldowns = new();

		public static IReadOnlyList<WeatherFieldState> Fields => fields;

		public override void Unload()
		{
			fields.Clear();
			cooldowns.Clear();
			nextId = 1;
		}

		public override void PostUpdateWorld()
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			for (int i = fields.Count - 1; i >= 0; i--)
			{
				fields[i].TimeLeft--;
				if (fields[i].TimeLeft <= 0)
				{
					HenshinNet.BroadcastWeatherField(fields[i], remove: true);
					fields.RemoveAt(i);
				}
			}

			var keys = new List<(byte, WeatherTag)>(cooldowns.Keys);
			foreach (var key in keys)
			{
				cooldowns[key]--;
				if (cooldowns[key] <= 0)
					cooldowns.Remove(key);
			}
		}

		public static bool TrySpawn(Vector2 center, WeatherTag tag, byte owner, float radius = DefaultRadius, int duration = DefaultDuration)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return false;

			var cdKey = (owner, tag);
			if (cooldowns.ContainsKey(cdKey))
				return false;

			var state = new WeatherFieldState
			{
				Id = nextId++,
				Center = center,
				Radius = radius,
				TimeLeft = duration,
				Tag = tag,
				Owner = owner
			};
			fields.Add(state);
			cooldowns[cdKey] = SameTagCooldown;
			HenshinNet.BroadcastWeatherField(state, remove: false);
			return true;
		}

		public static bool IsInField(Vector2 worldPos, WeatherTag tag)
		{
			foreach (var f in fields)
			{
				if (f.Tag != tag)
					continue;
				if (Vector2.DistanceSquared(worldPos, f.Center) <= f.Radius * f.Radius)
					return true;
			}
			return false;
		}

		public static bool IsInAnyRain(Vector2 worldPos)
			=> IsInField(worldPos, WeatherTag.Rain) || Main.raining;

		internal static void ApplyRemoteUpsert(int id, Vector2 center, float radius, int timeLeft, WeatherTag tag, byte owner)
		{
			for (int i = 0; i < fields.Count; i++)
			{
				if (fields[i].Id == id)
				{
					fields[i].Center = center;
					fields[i].Radius = radius;
					fields[i].TimeLeft = timeLeft;
					fields[i].Tag = tag;
					fields[i].Owner = owner;
					return;
				}
			}
			fields.Add(new WeatherFieldState
			{
				Id = id,
				Center = center,
				Radius = radius,
				TimeLeft = timeLeft,
				Tag = tag,
				Owner = owner
			});
		}

		internal static void ApplyRemoteRemove(int id)
		{
			fields.RemoveAll(f => f.Id == id);
		}
	}
}
