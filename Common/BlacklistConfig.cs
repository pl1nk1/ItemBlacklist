using System.Collections.Generic;
using Terraria.ModLoader.Config;

namespace ItemBlacklist.Common
{
	// Personal house rules, not something a server should force on everyone -
	// each client decides their own blacklist
	public class BlacklistConfig : ModConfig
	{
		public override ConfigScope Mode => ConfigScope.ClientSide;

		// Each entry carries its own category toggles, independent of every
		// other mod's
		public List<ModBlacklistEntry> BlacklistedMods = new();

		public List<string> BlacklistedItems = new();

		// Category toggles used for BlacklistedItems only - BlacklistedMods
		// entries carry their own instead
		public bool BlockWeapons = true;
		public bool BlockAccessories = true;
		public bool BlockTools = false;
		public bool BlockPlaceables = false;
		public bool BlockOther = false;

		// Independent of BlockWeapons, same as ModBlacklistEntry's version -
		// 100 = no change
		public int WeaponDamagePercent = 100;
	}
}
