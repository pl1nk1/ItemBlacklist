using Terraria.ModLoader;
using ItemBlacklist.Common;

namespace ItemBlacklist
{
	public class ItemBlacklist : Mod
	{
		public override void Load()
		{
			BlacklistKeybinds.Toggle = KeybindLoader.RegisterKeybind(this, "Toggle Item Blacklist", "OemQuestion");
		}
	}
}
