using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PokemonHenshin.Content.Config;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace PokemonHenshin.Content.Visual
{
	/// <summary>
	/// 物品栏右侧入口 + 当前变身属性面板。仅本地客户端；开背包时绘制。
	/// 入口与面板共用锚点：拖动任一则一起移动，面板始终贴在入口右侧；位置写入
	/// <see cref="HenshinClientConfig"/>。
	/// API：<see cref="ModSystem.UpdateUI"/> / <see cref="ModSystem.ModifyInterfaceLayers"/> /
	/// <see cref="UserInterface"/> / <see cref="UIState"/>（tML stable）。
	/// </summary>
	[Autoload(Side = ModSide.Client)]
	public sealed class HenshinStatUISystem : ModSystem
	{
		internal static HenshinStatUISystem Instance { get; private set; }

		private UserInterface userInterface;
		private HenshinStatUIState uiState;

		public override void Load()
		{
			Instance = this;
			if (Main.dedServ)
				return;

			uiState = new HenshinStatUIState();
			uiState.Activate();
			userInterface = new UserInterface();
		}

		public override void Unload()
		{
			Instance = null;
			userInterface = null;
			uiState = null;
		}

		public override void UpdateUI(GameTime gameTime)
		{
			if (userInterface == null || uiState == null)
				return;

			if (ShouldShow())
			{
				if (userInterface.CurrentState == null)
					userInterface.SetState(uiState);
				userInterface.Update(gameTime);
			}
			else if (userInterface.CurrentState != null)
			{
				uiState.CancelDrag();
				userInterface.SetState(null);
			}
		}

		public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
		{
			if (userInterface == null)
				return;

			int index = layers.FindIndex(l => l.Name == "Vanilla: Inventory");
			if (index >= 0)
				index += 1;
			else
			{
				index = layers.FindIndex(l => l.Name == "Vanilla: Mouse Text");
				if (index < 0)
					index = layers.Count;
			}

			layers.Insert(index, new LegacyGameInterfaceLayer(
				"PokemonHenshin: StatSheet",
				delegate
				{
					if (ShouldShow() && userInterface.CurrentState != null)
						userInterface.Draw(Main.spriteBatch, new GameTime());
					return true;
				},
				InterfaceScaleType.UI));
		}

		private static bool ShouldShow()
		{
			if (Main.dedServ || Main.gameMenu || Main.ingameOptionsWindow || Main.mapFullscreen)
				return false;
			if (!Main.playerInventory)
				return false;
			Player player = Main.LocalPlayer;
			return player != null && player.active && !player.dead;
		}

		/// <summary>
		/// 与原版物品栏格子一致：原点 (20, 20+小地图推挤)，格距 56×0.85。
		/// 不用 <see cref="Main.inventoryScale"/>——该字段在绘制箱子/商店时会被改掉。
		/// tML 未暴露原版 <c>mH</c>，小地图推挤按 1.4.4 惯例重算；若本帧已画过装备栏则改用
		/// <see cref="AccessorySlotLoader.DrawVerticalAlignment"/> 反推（装备栏顶 = 物品栏顶 + 154）。
		/// Y 再下移 64，避开原版图鉴按钮（与物品栏右上同列）。
		/// </summary>
		internal static void GetDefaultButtonPos(out int x, out int y)
		{
			const float scale = 0.85f;
			x = (int)(20f + 10f * 56f * scale) + 2;
			int mapPush = 0;
			if (Main.mapEnabled && !Main.mapFullscreen && Main.mapStyle == 1)
			{
				mapPush = 256;
				if (mapPush + 600 > Main.screenHeight)
					mapPush = Math.Max(0, Main.screenHeight - 600);
			}

			y = 20 + mapPush;
			int equipY = AccessorySlotLoader.DrawVerticalAlignment;
			if (equipY > 40)
				y = Math.Max(20, equipY - 154);
			y += 64;
		}

		/// <summary>面板左缘相对入口左缘的水平间距（入口宽 42 + 间隙 4）。</summary>
		internal const int PanelOffsetX = 46;

		internal static void ClampButtonPos(ref int x, ref int y, int buttonSize, int panelWidth, int panelHeight, bool panelOpen)
		{
			int groupW = panelOpen ? PanelOffsetX + panelWidth : buttonSize;
			int groupH = panelOpen ? Math.Max(buttonSize, Math.Min(panelHeight, 220)) : buttonSize;
			x = Math.Clamp(x, 12, Math.Max(12, Main.screenWidth - groupW - 12));
			y = Math.Clamp(y, 12, Math.Max(12, Main.screenHeight - groupH - 12));
		}
	}

	public sealed class HenshinStatUIState : UIState
	{
		internal bool PanelOpen;

		internal void CancelDrag()
		{
			dragging = false;
			dragMoved = false;
			dragFromToggle = false;
		}

		private StatToggleButton toggle;
		private UIPanel panel;
		private StatPanelHeader header;
		private UIList list;
		private UIScrollbar scrollbar;
		private string lastFingerprint = string.Empty;
		private string formTexturePath;
		private bool dragging;
		private bool dragMoved;
		private bool dragFromToggle;
		private Vector2 dragOffset;
		private Vector2 dragStartMouse;
		private int lastClickTick = -999;
		private const int PanelWidth = 380;
		private const int ButtonSize = 42;
		private const int TitleHeight = 30;
		private const float DragThreshold = 4f;

		public override void OnInitialize()
		{
			IgnoresMouseInteraction = true;
			Width.Set(0f, 1f);
			Height.Set(0f, 1f);

			toggle = new StatToggleButton();
			toggle.Width.Set(ButtonSize, 0f);
			toggle.Height.Set(ButtonSize, 0f);
			toggle.OnLeftMouseDown += OnToggleMouseDown;
			toggle.OnUpdate += MouseBlock;
			Append(toggle);

			panel = new UIPanel();
			panel.Width.Set(PanelWidth, 0f);
			panel.Height.Set(520f, 0f);
			panel.BackgroundColor = new Color(28, 30, 48) * 0.94f;
			panel.BorderColor = new Color(90, 110, 160);
			panel.SetPadding(8f);
			panel.OverflowHidden = true;
			panel.OnUpdate += MouseBlock;

			header = new StatPanelHeader();
			header.Width.Set(0f, 1f);
			header.Height.Set(TitleHeight, 0f);
			header.OnDragStart += OnHeaderDragStart;
			header.OnResetClicked += OnResetClicked;
			panel.Append(header);

			list = new UIList();
			list.Width.Set(-22f, 1f);
			list.Top.Set(TitleHeight + 2f, 0f);
			list.Height.Set(-(TitleHeight + 2f), 1f);
			list.ListPadding = 2f;
			list.ManualSortMethod = _ => { };
			panel.Append(list);

			scrollbar = new UIScrollbar();
			scrollbar.Top.Set(TitleHeight + 2f, 0f);
			scrollbar.Height.Set(-(TitleHeight + 2f), 1f);
			scrollbar.HAlign = 1f;
			panel.Append(scrollbar);
			list.SetScrollbar(scrollbar);
		}

		public override void Update(GameTime gameTime)
		{
			int height = Math.Min(520, Math.Max(220, Main.screenHeight - 36));
			ResolveButtonPos(height, out int bx, out int by);

			if (dragging)
			{
				Vector2 mouse = Main.MouseScreen;
				if (!dragMoved && Vector2.DistanceSquared(mouse, dragStartMouse) >= DragThreshold * DragThreshold)
					dragMoved = true;

				if (dragMoved)
				{
					bx = (int)(mouse.X - dragOffset.X);
					by = (int)(mouse.Y - dragOffset.Y);
					HenshinStatUISystem.ClampButtonPos(ref bx, ref by, ButtonSize, PanelWidth, height, PanelOpen);
				}

				if (!Main.mouseLeft)
				{
					bool wasMoved = dragMoved;
					bool fromToggle = dragFromToggle;
					dragging = false;
					dragMoved = false;
					dragFromToggle = false;
					if (wasMoved)
						HenshinClientConfig.Instance?.SaveButtonPos(bx, by);
					else if (fromToggle)
						TogglePanel();
				}
			}

			toggle.Left.Set(bx, 0f);
			toggle.Top.Set(by, 0f);
			toggle.FormTexturePath = formTexturePath;
			toggle.PanelOpen = PanelOpen;

			if (PanelOpen)
			{
				if (panel.Parent == null)
					Append(panel);

				int px = bx + HenshinStatUISystem.PanelOffsetX;
				int py = by;
				panel.Left.Set(px, 0f);
				panel.Top.Set(py, 0f);
				panel.Height.Set(height, 0f);

				if (panel.IsMouseHovering)
					PlayerInput.LockVanillaMouseScroll("PokemonHenshin/StatSheet");
			}
			else if (panel.Parent != null)
				panel.Remove();

			RefreshSnapshot();
			Recalculate();
			base.Update(gameTime);
		}

		private void ResolveButtonPos(int panelHeight, out int bx, out int by)
		{
			HenshinClientConfig cfg = HenshinClientConfig.Instance;
			if (cfg != null && cfg.UseCustomButtonPos)
			{
				bx = cfg.ButtonPosX;
				by = cfg.ButtonPosY;
			}
			else
				HenshinStatUISystem.GetDefaultButtonPos(out bx, out by);

			HenshinStatUISystem.ClampButtonPos(ref bx, ref by, ButtonSize, PanelWidth, panelHeight, PanelOpen);
		}

		private void BeginDrag(bool fromToggle)
		{
			dragging = true;
			dragMoved = false;
			dragFromToggle = fromToggle;
			dragStartMouse = Main.MouseScreen;
			CalculatedStyle dims = toggle.GetDimensions();
			dragOffset = Main.MouseScreen - new Vector2(dims.X, dims.Y);
		}

		private void OnToggleMouseDown(UIMouseEvent evt, UIElement _)
		{
			BeginDrag(fromToggle: true);
		}

		private void OnHeaderDragStart(UIMouseEvent evt, UIElement _)
		{
			if (!PanelOpen)
				return;

			int tick = (int)Main.GameUpdateCount;
			if (tick - lastClickTick < 20)
			{
				OnResetClicked();
				lastClickTick = -999;
				return;
			}

			lastClickTick = tick;
			BeginDrag(fromToggle: false);
		}

		private void OnResetClicked()
		{
			dragging = false;
			dragMoved = false;
			dragFromToggle = false;
			HenshinClientConfig.Instance?.ResetButtonPos();
			SoundEngine.PlaySound(SoundID.MenuTick);
		}

		private void TogglePanel()
		{
			PanelOpen = !PanelOpen;
			lastFingerprint = string.Empty;
			SoundEngine.PlaySound(PanelOpen ? SoundID.MenuOpen : SoundID.MenuClose);
		}

		private void RefreshSnapshot()
		{
			HenshinStatSnapshot snap = HenshinStatSheet.Capture(Main.LocalPlayer);
			formTexturePath = snap.FormTexturePath;
			toggle.Transformed = snap.Transformed;
			if (!PanelOpen)
				return;
			if (snap.Fingerprint == lastFingerprint)
				return;
			lastFingerprint = snap.Fingerprint;
			RebuildList(snap);
		}

		private void RebuildList(HenshinStatSnapshot snap)
		{
			float view = list.ViewPosition;
			list.Clear();
			var items = new List<UIElement>(snap.Lines.Count);
			foreach (StatLine line in snap.Lines)
				items.Add(new StatLineElement(line));
			list.AddRange(items);
			float max = Math.Max(0f, list.GetTotalHeight() - 8f);
			list.ViewPosition = Math.Min(view, max);
		}

		private static void MouseBlock(UIElement affected)
		{
			if (affected.ContainsPoint(Main.MouseScreen))
				Main.LocalPlayer.mouseInterface = true;
		}
	}

	internal sealed class StatPanelHeader : UIElement
	{
		public event Action<UIMouseEvent, UIElement> OnDragStart;
		public event Action OnResetClicked;

		private UIText title;
		private UIText reset;

		public StatPanelHeader()
		{
			OnLeftMouseDown += (evt, el) =>
			{
				if (reset != null && reset.ContainsPoint(Main.MouseScreen))
					return;
				OnDragStart?.Invoke(evt, el);
			};
		}

		public override void OnInitialize()
		{
			title = new UIText(Language.GetTextValue("Mods.PokemonHenshin.StatsUI.PanelTitle"), 0.9f);
			title.TextColor = new Color(255, 210, 90);
			title.VAlign = 0.5f;
			title.Left.Set(4f, 0f);
			title.IgnoresMouseInteraction = true;
			Append(title);

			reset = new UIText(Language.GetTextValue("Mods.PokemonHenshin.StatsUI.PanelReset"), 0.78f);
			reset.TextColor = new Color(160, 190, 230);
			reset.VAlign = 0.5f;
			reset.HAlign = 1f;
			reset.Left.Set(-4f, 0f);
			reset.OnLeftClick += (_, _) => OnResetClicked?.Invoke();
			reset.OnMouseOver += (_, _) =>
			{
				reset.TextColor = Color.White;
				SoundEngine.PlaySound(SoundID.MenuTick);
			};
			reset.OnMouseOut += (_, _) => reset.TextColor = new Color(160, 190, 230);
			Append(reset);
		}

		protected override void DrawSelf(SpriteBatch spriteBatch)
		{
			CalculatedStyle dims = GetDimensions();
			var underline = new Rectangle((int)dims.X, (int)(dims.Y + dims.Height - 1f), (int)dims.Width, 1);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, underline, new Color(90, 110, 160) * 0.85f);

			if (IsMouseHovering && (reset == null || !reset.IsMouseHovering))
				Main.hoverItemName = Language.GetTextValue("Mods.PokemonHenshin.StatsUI.PanelDragHint");
			else if (reset != null && reset.IsMouseHovering)
				Main.hoverItemName = Language.GetTextValue("Mods.PokemonHenshin.StatsUI.PanelResetHint");
		}
	}

	internal sealed class StatToggleButton : UIElement
	{
		public string FormTexturePath;
		public bool Transformed;
		public bool PanelOpen;

		public StatToggleButton()
		{
			OnMouseOver += (_, _) => SoundEngine.PlaySound(SoundID.MenuTick);
		}

		protected override void DrawSelf(SpriteBatch spriteBatch)
		{
			CalculatedStyle dims = GetDimensions();
			Rectangle dest = dims.ToRectangle();
			Texture2D back = TextureAssets.InventoryBack.Value;
			Color backColor = PanelOpen ? new Color(140, 180, 255) : (Transformed ? Color.White : new Color(160, 160, 180));
			if (IsMouseHovering)
				backColor = Color.Lerp(backColor, Color.White, 0.25f);
			spriteBatch.Draw(back, dest, backColor);

			string path = FormTexturePath;
			if (string.IsNullOrEmpty(path))
				path = "PokemonHenshin/Assets/Forms/L01_F01";
			if (ModContent.HasAsset(path))
			{
				Texture2D tex = ModContent.Request<Texture2D>(path, AssetRequestMode.ImmediateLoad).Value;
				int pad = 5;
				var inner = new Rectangle(dest.X + pad, dest.Y + pad, dest.Width - pad * 2, dest.Height - pad * 2);
				float scale = Math.Min(inner.Width / (float)tex.Width, inner.Height / (float)tex.Height);
				var origin = new Vector2(tex.Width / 2f, tex.Height / 2f);
				var center = new Vector2(inner.Center.X, inner.Center.Y);
				Color icon = Transformed || PanelOpen ? Color.White : new Color(180, 180, 190) * 0.85f;
				spriteBatch.Draw(tex, center, null, icon, 0f, origin, scale, SpriteEffects.None, 0f);
			}

			if (IsMouseHovering)
				Main.hoverItemName = Language.GetTextValue("Mods.PokemonHenshin.StatsUI.ButtonHover");
		}
	}

	internal sealed class StatLineElement : UIElement
	{
		public StatLineElement(StatLine line)
		{
			Width.Set(0f, 1f);
			bool header = line.Kind == StatLineKind.Header;
			Height.Set(header ? 26f : 20f, 0f);
			IgnoresMouseInteraction = true;

			var text = new UIText(line.Text, header ? 0.92f : 0.78f);
			text.TextColor = line.Kind switch
			{
				StatLineKind.Header => new Color(255, 210, 90),
				StatLineKind.Active => new Color(140, 230, 150),
				StatLineKind.Inactive => new Color(150, 155, 170),
				StatLineKind.Warn => new Color(255, 170, 110),
				_ => new Color(230, 235, 245)
			};
			text.Top.Set(header ? 6f : 1f, 0f);
			Append(text);
		}
	}
}
