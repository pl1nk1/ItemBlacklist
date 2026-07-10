using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace ItemBlacklist.Common
{
	// Blocks use/equip of anything listed in BlacklistConfig - either a
	// whole mod (with its own category toggles), or specific items by
	// ModName/ItemClassName (using the global category toggles instead)
	public class BlacklistGlobalItem : GlobalItem
	{
		// Throttles the "disabled" chat message so holding the use button
		// doesn't spam it every tick
		private static int lastMessageTick = -1000;

		private static ModBlacklistEntry FindModEntry(Item item)
		{
			if (item.ModItem == null)
				return null;

			string modName = item.ModItem.Mod.Name;
			return ModContent.GetInstance<BlacklistConfig>().BlacklistedMods.Find(e => e.ModName == modName);
		}

		private static bool IsBlacklistedItem(Item item)
		{
			if (item.ModItem == null)
				return false;

			string fullName = $"{item.ModItem.Mod.Name}/{item.ModItem.Name}";
			return ModContent.GetInstance<BlacklistConfig>().BlacklistedItems.Contains(fullName);
		}

		public static bool IsBlacklisted(Item item) => FindModEntry(item) != null || IsBlacklistedItem(item);

		private static bool IsTool(Item item) => item.pick > 0 || item.axe > 0 || item.hammer > 0;
		private static bool IsPlaceable(Item item) => item.createTile >= 0 || item.createWall >= 0;
		private static bool IsWeapon(Item item) => item.damage > 0 && !item.accessory && !IsTool(item) && !IsPlaceable(item);

		// The category toggles that actually apply to this item: its mod's
		// own entry if it has one, otherwise the global toggles (used for
		// individually blacklisted items)
		private static (bool weapons, bool accessories, bool tools, bool placeables, bool other) EffectiveToggles(Item item)
		{
			ModBlacklistEntry modEntry = FindModEntry(item);
			if (modEntry != null)
				return (modEntry.BlockWeapons, modEntry.BlockAccessories, modEntry.BlockTools, modEntry.BlockPlaceables, modEntry.BlockOther);

			BlacklistConfig config = ModContent.GetInstance<BlacklistConfig>();
			return (config.BlockWeapons, config.BlockAccessories, config.BlockTools, config.BlockPlaceables, config.BlockOther);
		}

		private static bool CategoryBlocksUse(Item item)
		{
			var toggles = EffectiveToggles(item);
			if (IsTool(item))
				return toggles.tools;
			if (IsPlaceable(item))
				return toggles.placeables;
			if (IsWeapon(item))
				return toggles.weapons;
			if (item.accessory)
				return false; // gated in CanEquipAccessory instead
			return toggles.other;
		}

		public override bool CanUseItem(Item item, Player player)
		{
			if (!IsBlacklisted(item) || !CategoryBlocksUse(item))
				return true;

			if (Main.GameUpdateCount - lastMessageTick > 60)
			{
				lastMessageTick = (int)Main.GameUpdateCount;
				Main.NewText($"{item.Name} is disabled by house rule.", 220, 60, 60);
			}
			return false;
		}

		public override bool CanEquipAccessory(Item item, Player player, int slot, bool modded)
		{
			if (!IsBlacklisted(item))
				return true;
			return !EffectiveToggles(item).accessories;
		}

		// Independent of the block toggles - lets a weapon stay usable but
		// hit for less, instead of being fully blocked
		private static int WeaponDamagePercent(Item item)
		{
			if (IsTool(item))
				return 100; // this nerf is for weapons specifically, not incidental tool damage

			ModBlacklistEntry modEntry = FindModEntry(item);
			if (modEntry != null)
				return modEntry.WeaponDamagePercent;

			if (IsBlacklistedItem(item))
				return ModContent.GetInstance<BlacklistConfig>().WeaponDamagePercent;

			return 100;
		}

		public override void ModifyWeaponDamage(Item item, Player player, ref StatModifier damage)
		{
			int percent = WeaponDamagePercent(item);
			if (percent != 100)
				damage *= percent / 100f;
		}

		public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
		{
			if (!IsBlacklisted(item))
				return;

			tooltips.Add(new TooltipLine(Mod, "Blacklisted", "Disabled by house rule")
			{
				OverrideColor = new Color(220, 60, 60)
			});

			int percent = WeaponDamagePercent(item);
			if (percent != 100)
			{
				tooltips.Add(new TooltipLine(Mod, "DamageNerf", $"Deals {percent}% damage by house rule")
				{
					OverrideColor = new Color(220, 140, 60)
				});
			}
		}
	}
}
