using System;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace PokemonHenshin.Content.Core
{
	/// <summary>
	/// 形态注册表（docs/engineering.md）。按 FormId / NetworkId / ItemType 查询。
	/// 注册发生在物品 <c>SetStaticDefaults</c>（此时 ItemType 已分配），卸载时清空。
	/// </summary>
	public static class FormRegistry
	{
		private static readonly Dictionary<string, FormDefinition> byFormId = new(StringComparer.Ordinal);
		private static readonly Dictionary<ushort, FormDefinition> byNetId = new();
		private static readonly Dictionary<int, FormDefinition> byItemType = new();

		public static IReadOnlyCollection<FormDefinition> All => byFormId.Values;

		public static void Register(FormDefinition def, int itemType)
		{
			if (def == null)
				throw new ArgumentNullException(nameof(def));
			if (string.IsNullOrEmpty(def.FormId))
				throw new ArgumentException("FormId 不能为空", nameof(def));
			if (def.NetworkId == 0)
				throw new ArgumentException($"{def.FormId}: NetworkId 0 保留为「无形态」", nameof(def));
			if (byFormId.ContainsKey(def.FormId))
				throw new InvalidOperationException($"重复 FormId: {def.FormId}");
			if (byNetId.ContainsKey(def.NetworkId))
				throw new InvalidOperationException($"重复 NetworkId: {def.NetworkId} ({def.FormId})");
			if (byItemType.ContainsKey(itemType))
				throw new InvalidOperationException($"ItemType {itemType} 已绑定形态 {byItemType[itemType].FormId}");

			def.ItemType = itemType;
			byFormId[def.FormId] = def;
			byNetId[def.NetworkId] = def;
			byItemType[itemType] = def;
		}

		public static FormDefinition ByFormId(string formId)
			=> formId != null && byFormId.TryGetValue(formId, out var def) ? def : null;

		public static FormDefinition ByNetworkId(ushort netId)
			=> netId != 0 && byNetId.TryGetValue(netId, out var def) ? def : null;

		public static FormDefinition ByItemType(int itemType)
			=> itemType > 0 && byItemType.TryGetValue(itemType, out var def) ? def : null;

		/// <summary>查找以 <paramref name="formId"/> 为 EvolvesFrom 的下一形态；无则 null。</summary>
		public static FormDefinition FindEvolutionOf(string formId)
		{
			if (string.IsNullOrEmpty(formId))
				return null;
			foreach (FormDefinition def in byFormId.Values)
			{
				if (def.EvolvesFrom == formId)
					return def;
			}
			return null;
		}

		internal static void Clear()
		{
			byFormId.Clear();
			byNetId.Clear();
			byItemType.Clear();
		}
	}

	/// <summary>负责卸载时清空注册表，避免热重载残留。</summary>
	public sealed class FormRegistrySystem : ModSystem
	{
		public override void Unload() => FormRegistry.Clear();
	}
}
