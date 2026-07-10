namespace ItemBlacklist.Common
{
	// One mod's own blacklist settings, independent of every other
	// blacklisted mod's toggles
	public class ModBlacklistEntry
	{
		public string ModName = "";

		public bool BlockWeapons = true;
		public bool BlockAccessories = true;
		public bool BlockTools = false;
		public bool BlockPlaceables = false;
		public bool BlockOther = false;

		// Independent of BlockWeapons - lets weapons stay usable but at
		// reduced damage instead of fully blocked. 100 = no change. Moot if
		// BlockWeapons is also on, since the weapon can't be used at all then.
		public int WeaponDamagePercent = 100;

		// Shown as the collapsed list entry's label in the config UI
		public override string ToString() => string.IsNullOrWhiteSpace(ModName) ? "(unnamed)" : ModName;
	}
}
