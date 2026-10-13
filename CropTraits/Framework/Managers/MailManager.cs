using CropTraits.Framework.Utilities.Helpers;
using StardewModdingAPI;
using StardewValley;
using System.Collections.Generic;

namespace CropTraits.Framework.Managers
{
    public class MailManager : BaseManager
    {
        public const string FIELD_NOTES_MAIL_ID = "PeacefulEnd.CropTraits_FieldNotes";
        public const int FIELD_NOTES_FARMING_LEVEL = 3;

        public static readonly string FIELD_NOTES_SHOP_CONDITION = $"PLAYER_HAS_MAIL Current {FIELD_NOTES_MAIL_ID} Any";

        public MailManager(IMonitor monitor, IModHelper helper) : base(monitor, helper)
        {

        }

        public void EditMail(IDictionary<string, string> data)
        {
            data[FIELD_NOTES_MAIL_ID] = $"{TranslationHelper.Get("mail.field_notes.text")}%item id {CropTraits.JOURNAL_QUALIFIED_ID} 1 %%[#]{TranslationHelper.Get("mail.field_notes.title")}";
        }

        // Called at DayEnding on each player's own game (farmhands and split-screen players included), so the letter arrives the next morning
        public void TryQueueFieldNotesMail()
        {
            Farmer player = Game1.player;
            if (player is null || player.hasOrWillReceiveMail(FIELD_NOTES_MAIL_ID) || IsFieldNotesUnlocked(player) is false)
            {
                return;
            }

            player.mailForTomorrow.Add(FIELD_NOTES_MAIL_ID);
        }

        // Discoveries are shared across the save, so a trait found by any player counts
        public bool IsFieldNotesUnlocked(Farmer player)
        {
            return CropTraits.discoveryManager.Data.Traits.Count > 0 || player.farmingLevel.Value >= FIELD_NOTES_FARMING_LEVEL;
        }
    }
}
