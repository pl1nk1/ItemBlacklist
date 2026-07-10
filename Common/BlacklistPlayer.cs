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
			string fullName = $"{hover.ModItem.Mod.Name}/{hover.ModItem.Name}";

			if (config.BlacklistedItems.Contains(fullName))
			{
				config.BlacklistedItems.Remove(fullName);
				Main.NewText($"Un-blacklisted {hover.Name}.", 120, 220, 120);
			}
			else
			{
				config.BlacklistedItems.Add(fullName);
				Main.NewText($"Blacklisted {hover.Name}.", 220, 60, 60);
			}

			config.SaveChanges();
		}
	}
}
