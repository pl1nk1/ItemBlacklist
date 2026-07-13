namespace ItemBlacklist.Common
{
	// One individually-blacklisted item's own settings, independent of every
	// other individual item and of any whole-mod entry that might also match
	// its mod. This is what lets a single problem item (e.g. one with its
	// own IL hooks bypassing a mod-wide nerf) be configured on its own,
	// without needing - or being shadowed by - a whole-mod entry.
	public class ItemBlacklistEntry
	{
		public string ModName = "";
		public string ItemName = "";

		public bool BlockWeapons = true;
		public bool BlockAccessories = true;
		public bool BlockTools = false;
		public bool BlockPlaceables = false;
		public bool BlockOther = false;

		// Independent of BlockWeapons - lets the item stay usable but at
		// reduced damage instead of fully blocked. 100 = no change. Moot if
		// BlockWeapons is also on, since the item can't be used at all then.
		public int WeaponDamagePercent = 100;

		// Some weapons (e.g. Calamity Overhaul's "Legend" weapons) install
		// their own IL hooks that bypass GlobalItem.ModifyWeaponDamage
		// entirely, so the normal nerf silently does nothing to them - and
		// forcing a block via BlockWeapons doesn't work either, since the
		// same bypass affects CanUseItem. Turning this on applies the nerf
		// from BlacklistPlayer.ModifyHitNPC instead, after the hit lands -
		// this can't be bypassed the same way, but it means the weapon's
		// tooltip damage number won't reflect the nerf (only WeaponDamagePercent's
		// text line will). Leave off for normal weapons; the default nerf
		// already updates the displayed damage correctly for those.
		public bool PostFireNerf = false;

		// Shown as the collapsed list entry's label in the config UI
		public override string ToString() =>
			string.IsNullOrWhiteSpace(ModName) || string.IsNullOrWhiteSpace(ItemName) ? "(unnamed)" : $"{ModName}/{ItemName}";
	}
}
