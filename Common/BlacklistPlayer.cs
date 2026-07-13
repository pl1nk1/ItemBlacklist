using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;

namespace ItemBlacklist.Common
{
	// Press the keybind while hovering a modded item (inventory, chest,
	// anywhere ItemSlot draws one) to toggle it in/out of BlacklistedItems -
	// the same "hover + press a key" interaction vanilla uses for favoriting
	public class BlacklistPlayer : ModPlayer
	{
		// Throttles the "switched away" chat message the same way
		// BlacklistGlobalItem throttles its own "disabled" message
		private int lastSwapMessageTick = -1000;

		// Opt-in fallback nerf (ModBlacklistEntry.PostFireNerf /
		// ItemBlacklistEntry.PostFireNerf) for weapons whose own IL hooks
		// bypass GlobalItem.ModifyWeaponDamage entirely - applies the
		// percent after the hit lands instead, which can't be routed
		// around the same way, at the cost of the tooltip's damage number
		// not reflecting it. BlacklistGlobalItem.ModifyWeaponDamage skips
		// its own nerf whenever this is on for an item, so this is the
		// only place it gets applied - no double-nerfing.
		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
		{
			Item held = Player.HeldItem;
			if (held == null || held.IsAir || !BlacklistGlobalItem.UsesPostFireNerf(held))
				return;

			int percent = BlacklistGlobalItem.WeaponDamagePercent(held);
			if (percent != 100)
				modifiers.FinalDamage *= percent / 100f;
		}

		// Safety net for weapons whose own CanUseItem bypasses
		// GlobalItem.CanUseItem entirely. Confirmed by decompiling
		// InnoVault (the library Calamity Overhaul's "Legend" weapons are
		// built on): its OnCanUseItemHook is an IL hook installed directly
		// on ItemLoader.CanUseItem, the real static method. When the
		// weapon's own ItemOverride.On_CanUseItem returns a non-null
		// value - which SHPC's always does - the hook returns immediately
		// and never calls the original method at all, the one that would
		// have looped through every GlobalItem including ours. It isn't
		// skipped due to ordering; it's structurally unreachable.
		//
		// Earlier attempts assumed the weapon's firing logic still checks
		// SOME player-side signal we could suppress - first itemAnimation
		// (reset in PostUpdate, which just let it refire every tick with
		// no cooldown at all, since its own logic doesn't check elapsed
		// time, only "can I fire right now"), then player.controlUseItem
		// (checked in PreUpdate, but SHPC's default firing branch never
		// reads that field either - it always returns true once its own
		// mana/ammo conditions are met). Both failed because they assumed
		// hooks into a pipeline this weapon has already stepped outside of.
		//
		// The one thing nothing can route around: whether the item is
		// ever the held/selected item in the first place. If it never is,
		// none of the weapon's firing logic - however it's wired - gets a
		// chance to run. So instead of trying to interrupt use, immediately
		// swap the hotbar selection away from a blocked item the moment it
		// becomes selected.
		public override void PreUpdate()
		{
			int slot = Player.selectedItem;
			if (slot < 0 || slot >= 10)
				return;

			Item held = Player.inventory[slot];
			if (held == null || held.IsAir)
				return;
			if (!BlacklistGlobalItem.IsBlacklisted(held) || !BlacklistGlobalItem.CategoryBlocksUse(held))
				return;

			for (int i = 0; i < 10; i++)
			{
				if (i == slot)
					continue;

				Item candidate = Player.inventory[i];
				if (candidate.IsAir || !BlacklistGlobalItem.IsBlacklisted(candidate) || !BlacklistGlobalItem.CategoryBlocksUse(candidate))
				{
					Player.selectedItem = i;
					break;
				}
			}

			// Belt-and-braces in case every hotbar slot is somehow blocked
			// (or the swap above didn't find anywhere safe) - at least
			// prevent this tick's use directly
			Player.controlUseItem = false;
			Player.itemAnimation = 0;
			Player.itemTime = 0;
			Player.channel = false;

			if (Main.GameUpdateCount - lastSwapMessageTick > 60)
			{
				lastSwapMessageTick = (int)Main.GameUpdateCount;
				Main.NewText($"{held.Name} is disabled by house rule - switched away from it.", 220, 60, 60);
			}
		}

		public override void ProcessTriggers(TriggersSet triggersSet)
		{
			if (!BlacklistKeybinds.Toggle.JustPressed)
				return;

			Item hover = Main.HoverItem;
			if (hover == null || hover.IsAir || hover.ModItem == null)
			{
				Main.NewText("Hover over a modded item to toggle its blacklist status.", 220, 180, 60);
				return;
			}

			BlacklistConfig config = ModContent.GetInstance<BlacklistConfig>();
			string modName = hover.ModItem.Mod.Name;
			string itemName = hover.ModItem.Name;

			ItemBlacklistEntry existing = config.BlacklistedItems.Find(e => e.ModName == modName && e.ItemName == itemName);
			if (existing != null)
			{
				config.BlacklistedItems.Remove(existing);
				Main.NewText($"Un-blacklisted {hover.Name}.", 120, 220, 120);
			}
			else
			{
				config.BlacklistedItems.Add(new ItemBlacklistEntry { ModName = modName, ItemName = itemName });
				Main.NewText($"Blacklisted {hover.Name}. Adjust its toggles in the mod config if needed.", 220, 60, 60);
			}

			config.SaveChanges();
		}
	}
}
