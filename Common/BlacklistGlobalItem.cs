using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace ItemBlacklist.Common
{
	// Blocks use/equip of anything listed in BlacklistConfig - either a
	// whole mod (with its own category toggles), or specific items (with
	// their own toggles too). If an item is both individually listed AND
	// belongs to a whole-mod entry, the individual listing wins for that
	// one item - see EffectiveToggles/WeaponDamagePercent/UsesPostFireNerf.
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

		private static ItemBlacklistEntry FindItemEntry(Item item)
		{
			if (item.ModItem == null)
				return null;

			string modName = item.ModItem.Mod.Name;
			string itemName = item.ModItem.Name;
			return ModContent.GetInstance<BlacklistConfig>().BlacklistedItems.Find(e => e.ModName == modName && e.ItemName == itemName);
		}

		public static bool IsBlacklisted(Item item) => FindModEntry(item) != null || FindItemEntry(item) != null;

		private static bool IsTool(Item item) => item.pick > 0 || item.axe > 0 || item.hammer > 0;
		private static bool IsPlaceable(Item item) => item.createTile >= 0 || item.createWall >= 0;
		private static bool IsWeapon(Item item) => item.damage > 0 && !item.accessory && !IsTool(item) && !IsPlaceable(item);

		// The category toggles that actually apply to this item. Individually
		// listing an item OVERRIDES its mod's own whole-mod entry (checked
		// first here) rather than being shadowed by it - this is what lets
		// one problem item (e.g. a weapon with its own IL hooks bypassing
		// the mod-wide nerf) be configured differently without touching the
		// rest of that mod. Falls back to the mod-wide entry, then sane
		// defaults, only if the item isn't individually listed.
		private static (bool weapons, bool accessories, bool tools, bool placeables, bool other) EffectiveToggles(Item item)
		{
			ItemBlacklistEntry itemEntry = FindItemEntry(item);
			if (itemEntry != null)
				return (itemEntry.BlockWeapons, itemEntry.BlockAccessories, itemEntry.BlockTools, itemEntry.BlockPlaceables, itemEntry.BlockOther);

			ModBlacklistEntry modEntry = FindModEntry(item);
			if (modEntry != null)
				return (modEntry.BlockWeapons, modEntry.BlockAccessories, modEntry.BlockTools, modEntry.BlockPlaceables, modEntry.BlockOther);

			return (true, true, false, false, false);
		}

		internal static bool CategoryBlocksUse(Item item)
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
		// hit for less, instead of being fully blocked. Same override order
		// as EffectiveToggles: an individually listed item uses its own
		// percent instead of its mod's whole-mod entry, even if that mod is
		// also blacklisted as a whole.
		internal static int WeaponDamagePercent(Item item)
		{
			if (IsTool(item))
				return 100; // this nerf is for weapons specifically, not incidental tool damage

			ItemBlacklistEntry itemEntry = FindItemEntry(item);
			if (itemEntry != null)
				return itemEntry.WeaponDamagePercent;

			ModBlacklistEntry modEntry = FindModEntry(item);
			if (modEntry != null)
				return modEntry.WeaponDamagePercent;

			return 100;
		}

		// Opt-in per entry: some weapons (e.g. Calamity Overhaul's "Legend"
		// weapons) install their own IL hooks that bypass GlobalItem
		// entirely, so ModifyWeaponDamage below never fires for them. When
		// this is on for an entry, BlacklistPlayer.ModifyHitNPC applies the
		// nerf instead, after the hit lands - that can't be bypassed the
		// same way, but the tooltip's own damage number won't reflect it
		// (only the text line will), since nothing feeds vanilla's display
		// calculation in that path. Same override order as EffectiveToggles/
		// WeaponDamagePercent - this is exactly what lets a single
		// bypass-prone weapon opt into the fallback nerf without turning it
		// on for every other (normally-behaving) item in an otherwise
		// whole-mod-blacklisted mod.
		internal static bool UsesPostFireNerf(Item item)
		{
			ItemBlacklistEntry itemEntry = FindItemEntry(item);
			if (itemEntry != null)
				return itemEntry.PostFireNerf;

			ModBlacklistEntry modEntry = FindModEntry(item);
			if (modEntry != null)
				return modEntry.PostFireNerf;

			return false;
		}

		// This is what makes the tooltip's own damage number (not just the
		// house-rule note below it) actually show the nerfed value - it
		// feeds the same StatModifier vanilla uses to compute displayed
		// damage. Skipped when UsesPostFireNerf is on for this entry, since
		// BlacklistPlayer.ModifyHitNPC applies the nerf instead in that
		// case - applying both would nerf the weapon twice.
		public override void ModifyWeaponDamage(Item item, Player player, ref StatModifier damage)
		{
			if (UsesPostFireNerf(item))
				return;

			int percent = WeaponDamagePercent(item);
			if (percent != 100)
				damage *= percent / 100f;
		}

		public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
		{
			if (!IsBlacklisted(item))
				return;

			// "Disabled" only if the relevant category toggle (or the
			// accessory-slot gate) actually blocks it - a blacklisted mod
			// with its Block* toggles off is only ever nerfed, never blocked,
			// and the tooltip shouldn't claim otherwise
			bool actuallyBlocked = item.accessory ? EffectiveToggles(item).accessories : CategoryBlocksUse(item);
			if (actuallyBlocked)
			{
				tooltips.Add(new TooltipLine(Mod, "Blacklisted", "Disabled by house rule")
				{
					OverrideColor = new Color(220, 60, 60)
				});
			}

			int percent = WeaponDamagePercent(item);
			if (percent != 100)
			{
				string suffix = UsesPostFireNerf(item) ? " (applied after the hit lands - damage shown above is unaffected)" : "";
				tooltips.Add(new TooltipLine(Mod, "DamageNerf", $"Deals {percent}% damage by house rule{suffix}")
				{
					OverrideColor = new Color(220, 140, 60)
				});
			}
		}
	}
}
