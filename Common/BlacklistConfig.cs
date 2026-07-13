using System.Collections.Generic;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader.Config;

namespace ItemBlacklist.Common
{
	// Shared house rules, synced from the server so every player sees and is
	// bound by the same blacklist - only the server host can actually
	// change it (see AcceptClientChanges), everyone else gets a read-only
	// synced copy.
	public class BlacklistConfig : ModConfig
	{
		public override ConfigScope Mode => ConfigScope.ServerSide;

		// Each entry carries its own category toggles, independent of every
		// other mod's
		public List<ModBlacklistEntry> BlacklistedMods = new();

		// Each entry carries its own toggles too, independent of every other
		// individual item and of any whole-mod entry that also matches its
		// mod (an individual entry overrides its mod's whole-mod entry - see
		// BlacklistGlobalItem.EffectiveToggles/WeaponDamagePercent/UsesPostFireNerf)
		public List<ItemBlacklistEntry> BlacklistedItems = new();

		// Called on the server whenever a client (including the host's own
		// client, in Host & Play) requests a change to this config. Only
		// accept it if the request came from the same process as the
		// server itself - i.e. the player hosting locally. On a true
		// dedicated server (no local player at all), this rejects
		// everyone; the server operator has to edit the config file
		// directly instead, which is the correct notion of "admin" there.
		public override bool AcceptClientChanges(ModConfig pendingConfig, int whoAmI, ref NetworkText message)
		{
			if (whoAmI != Main.myPlayer)
			{
				message = NetworkText.FromLiteral("Only the server host can change the Item Blacklist configuration.");
				return false;
			}
			return true;
		}

		// Without this, a rejected change (e.g. the hover+keybind toggle,
		// or the mod config screen, used by a non-host client) fails
		// silently - this surfaces the rejection reason to whoever
		// requested it.
		public override void HandleAcceptClientChangesReply(bool success, int player, NetworkText message)
		{
			if (!success)
				Main.NewText(message.ToString(), 220, 60, 60);
		}
	}
}
