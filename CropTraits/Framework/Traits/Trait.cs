using CropTraits.Framework.Managers;
using CropTraits.Framework.Models;
using CropTraits.Framework.Utilities.Helpers;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Buffs;
using StardewValley.GameData.Crops;
using System;
using System.Collections.Generic;

namespace CropTraits.Framework.Traits
{
    public abstract class Trait
    {
        public const int COMMON_WEIGHT = 12;
        public const int UNCOMMON_WEIGHT = 8;
        public const int RARE_WEIGHT = 4;

        /// <summary>
        /// The ID stored in the trait key.
        /// </summary>
        public abstract string Id { get; }

        /// <summary>
        /// Whether the trait helps, hurts or does neither. Mutation picks a polarity first using fixed shares, then a trait within it.
        /// </summary>
        public virtual TraitPolarity Polarity => TraitPolarity.Positive;

        /// <summary>
        /// If this trait is harmful / useless, which shows it in red.
        /// </summary>
        public bool IsNegative => Polarity == TraitPolarity.Negative;

        /// <summary>
        /// The max tier possible with 10 being the upper most tier.
        /// </summary>
        public virtual int MaxTier => 3;

        /// <summary>
        /// How often mutation rolls this trait compared to the other traits of the same polarity. Also sets the default weight.
        /// </summary>
        public virtual TraitRarity Rarity => TraitRarity.Common;

        /// <summary>
        /// How likely the trait is to be rolled within its polarity during a mutation. Defaults to the rarity's weight, so the rarity shown in the Field Notes stays accurate.
        /// </summary>
        public virtual int Weight => GetRarityWeight(Rarity);

        /// <summary>
        /// The traits this trait cannot coexist with. Conflicts are declared one way and checked in both directions.
        /// </summary>
        public virtual IReadOnlyList<string> ConflictsWith => Array.Empty<string>();

        /// <summary>
        /// If this trait cannot coexist with any other trait.
        /// </summary>
        public virtual bool IsExclusive => false;

        /// <summary>
        /// Whether this trait can be rolled by mutation (occurs the night after planting).
        /// </summary>
        public virtual bool CanBeRolled => true;

        /// <summary>
        /// Whether mutation can roll this trait for the given crop type, so traits that would do nothing on it (such as extra regrowth on a crop that already regrows) are skipped.
        /// Checked per crop type rather than per location. The crop data is null when it's missing (such as a crop from a removed mod), so traits that depend on it should return false.
        /// </summary>
        public virtual bool CanApplyTo(CropData cropData)
        {
            return true;
        }

        /// <summary>
        /// If this is rejected by the Seed Maker and Seed Splicer.
        /// </summary>
        public virtual bool BlocksPropagation => false;

        /// <summary>
        /// If this trait modifies the produce's price, which also carries it into artisan goods made from the produce.
        /// </summary>
        public virtual bool IsPriceTrait => false;

        /// <summary>
        /// If this trait stops mutations from replacing or displacing the crop's existing traits.
        /// </summary>
        public virtual bool BlocksMutationReplacement => false;

        /// <summary>
        /// If the crop ignores every fertilizer effect: growth speed, water retention, quality and Mutagenic Fertilizer's nightly rolls.
        /// </summary>
        public virtual bool IgnoresFertilizer => false;

        /// <summary>
        /// If the crop ignores growth speed changes from fertilizer, the Agriculturist profession, paddy water and neighboring crops.
        /// </summary>
        public virtual bool IgnoresGrowthModifiers => false;

        /// <summary>
        /// If the crop withers when it's left unharvested through the whole day it was ready.
        /// </summary>
        public virtual bool WithersWhenLeftReady => false;

        /// <summary>
        /// If the crop can be planted and keeps growing outdoors in every season, winter included.
        /// </summary>
        public virtual bool GrowsInEverySeason => false;

        /// <summary>
        /// If crows never eat the crop. Crows pass over it instead.
        /// </summary>
        public virtual bool IsIgnoredByCrows => false;

        /// <summary>
        /// If the crop protects the eight tiles around it from crows, like a tiny scarecrow.
        /// </summary>
        public virtual bool ProtectsNeighborsFromCrows => false;

        /// <summary>
        /// If scarecrows (and protective neighbors) can't keep crows away from the crop.
        /// </summary>
        public virtual bool IgnoresScarecrows => false;

        /// <summary>
        /// If the crop gives off a soft glow at night once the trait is visible (after maturity for mutations).
        /// </summary>
        public virtual bool GlowsAtNight => false;

        /// <summary>
        /// If diluting the crop in the Seed Splicer always splits off one trait instead of sometimes wiping them all.
        /// </summary>
        public virtual bool GuaranteesDilutionSplit => false;

        /// <summary>
        /// If the crop's traits always carry over when crossbred, skipping the inherit roll (the trait cap and conflicts still apply).
        /// </summary>
        public virtual bool AlwaysPassesOnTraits => false;

        /// <summary>
        /// If a crop that normally regrows is removed once it's harvested.
        /// </summary>
        public virtual bool IsRemovedAfterHarvest => false;

        /// <summary>
        /// If the full days the crop sits ready to harvest are counted overnight, so traits like Fragile and Mellow can change the harvest quality by them.
        /// </summary>
        public virtual bool TracksDaysLeftReady => false;

        /// <summary>
        /// If the crop can never become a giant crop. Checked for every crop in the grid, so one is enough to stop it.
        /// </summary>
        public virtual bool PreventsGiantCrop => false;

        /// <summary>
        /// If the crop clears weeds from the eight tiles around it each morning.
        /// </summary>
        public virtual bool ClearsNearbyWeeds => false;

        /// <summary>
        /// If lightning fully grows the crop instead of killing it, with storms on the farm also striking it on their own.
        /// </summary>
        public virtual bool IsConductive => false;

        /// <summary>
        /// If the trait turns into a random positive trait the crop can roll whenever it comes out of the Seed Splicer.
        /// </summary>
        public virtual bool AwakensWhenSpliced => false;

        /// <summary>
        /// Adjusts the growth phase lengths. The last entry is the final phase and must be left alone.
        /// Called after planting, whenever the phases are rebuilt from crop data and when a seed's tooltip estimates its growth time, so it should only change the list it's given.
        /// </summary>
        public virtual void ModifyGrowth(IList<int> phaseDays, CropData cropData, int tier)
        {

        }

        /// <summary>
        /// Adjusts the chance that the crop mutates. The first roll is the night after planting, later ones are Mutagenic Fertilizer's nightly rolls.
        /// </summary>
        public virtual double ModifyMutationChance(double chance, int tier, bool isFirstRoll)
        {
            return chance;
        }

        /// <summary>
        /// If the crop grows tonight without being watered (such as Deep Rooted anywhere or Paddy near water). Also lets Mutagenic Fertilizer roll on those nights.
        /// </summary>
        public virtual bool CountsAsWatered(GameLocation location, Vector2 tile, int tier)
        {
            return false;
        }

        /// <summary>
        /// Adjusts the fertilizer quality level vanilla uses for the crop's harvest quality odds (Deluxe Fertilizer is level 3).
        /// </summary>
        public virtual int ModifyQualityBoostLevel(int level, int tier)
        {
            return level;
        }

        /// <summary>
        /// Called once the harvest finishes, while the harvest context is still active so any items created here also receive traits.
        /// </summary>
        public virtual void OnHarvestCompleted(HarvestContext context, int xTile, int yTile, GameLocation location, int tier)
        {

        }

        /// <summary>
        /// Offered the main harvest as it reaches the player (picked by hand or dropped by a scythe). Returning true takes the item so the player never receives it.
        /// Called at most once per harvest and never for Junimo harvests. Extra produce (from the crop's data or traits like Prolific) is never offered.
        /// </summary>
        public virtual bool TryReplaceMainHarvest(HarvestContext context, Item item, int tier)
        {
            return false;
        }

        /// <summary>
        /// Called once a harvest is going to remove the crop from its soil, after traits like Resprouting and Spent have decided whether it stays.
        /// </summary>
        public virtual void OnCropRemovedAfterHarvest(HarvestContext context, int tier)
        {

        }

        /// <summary>
        /// Called each morning (main player only) for every living crop with this trait, so anything it spawns is waiting when the day starts.
        /// </summary>
        public virtual void OnDayStarted(MorningContext context, int tier)
        {

        }

        /// <summary>
        /// Adjusts the harvest quality right after the game picks it, so the change carries into every copy the harvest makes.
        /// The result is clamped to the crop's own quality limits afterward.
        /// </summary>
        public virtual int ModifyHarvestQuality(int quality, Random random, int tier)
        {
            return quality;
        }

        /// <summary>
        /// The same as ModifyHarvestQuality, for traits that need to know about the harvest itself (such as whether the crop is past its season).
        /// The harvest manager calls this one, which passes through to the simpler version unless a trait overrides it.
        /// </summary>
        public virtual int ModifyHarvestQuality(int quality, HarvestContext context, Random random, int tier)
        {
            return ModifyHarvestQuality(quality, random, tier);
        }

        /// <summary>
        /// Quality steps to shift the harvest by for the full days it was left ready (negative drops it). Only counted when a trait on the crop sets TracksDaysLeftReady.
        /// The steps from every trait are added together. The result is clamped to the crop's own quality limits.
        /// </summary>
        public virtual int GetLeftReadyQualityShift(int daysLeftReady, int tier)
        {
            return 0;
        }

        /// <summary>
        /// How many times a single harvest crop grows back after being harvested. The highest value among the crop's traits is used.
        /// </summary>
        public virtual int GetExtraRegrowths(int tier)
        {
            return 0;
        }

        /// <summary>
        /// Plans tonight's effects at the end of the day (main player only), before the game grows crops overnight.
        /// Traits are read from a snapshot taken after mutations resolve, so the order crops are visited never changes the result.
        /// </summary>
        public virtual void OnDayEnding(NightlyContext context, int tier)
        {

        }

        /// <summary>
        /// How many nights an outdoor crop keeps growing after its season ends. The highest value among the crop's traits is used.
        /// </summary>
        public virtual int GetOutOfSeasonNights(int tier)
        {
            return 0;
        }

        /// <summary>
        /// Adjusts how large the produce is drawn (1 is normal size). Only applies to produce, never seeds or artisan goods.
        /// </summary>
        public virtual float ModifyProduceScale(float scale, int tier)
        {
            return scale;
        }

        /// <summary>
        /// Adjusts the sell price multiplier for produce and artisan goods carrying this trait.
        /// </summary>
        public virtual double ModifySellPrice(double multiplier, int tier)
        {
            return multiplier;
        }

        /// <summary>
        /// Adjusts the color the crop and its produce are tinted with (white is no tint). Called every frame they're drawn, so it should stay cheap.
        /// </summary>
        public virtual Color ModifyTint(Color tint, string seedId, int tier)
        {
            return tint;
        }

        /// <summary>
        /// Adds to the nightly chance that the crop's grid turns into a giant crop, on top of the game's own roll. The highest bonus in the grid is used.
        /// </summary>
        public virtual double ModifyGiantCropChance(double chance, int tier)
        {
            return chance;
        }

        /// <summary>
        /// Adjusts how much produce a giant crop with this trait drops when broken (1 is normal).
        /// </summary>
        public virtual double ModifyGiantCropYield(double multiplier, int tier)
        {
            return multiplier;
        }

        /// <summary>
        /// Adjusts how much energy and health the produce restores when eaten (1 is normal).
        /// </summary>
        public virtual double ModifyFoodRestoration(double multiplier, int tier)
        {
            return multiplier;
        }

        /// <summary>
        /// A buff given when the produce is eaten, or null for none. Reusing the same buff ID refreshes it instead of stacking.
        /// </summary>
        public virtual Buff GetFoodBuff(int tier)
        {
            return null;
        }

        /// <summary>
        /// Changes an artisan good as a machine makes it from this produce (such as raising its quality).
        /// Only called for outputs made from the produce itself, like wine or jelly.
        /// </summary>
        public virtual void ModifyArtisanOutput(Item output, int tier)
        {

        }

        /// <summary>
        /// Adjusts the sell price multiplier for artisan goods only, applied after ModifySellPrice.
        /// The trait must also be a price trait (IsPriceTrait), otherwise it never carries into artisan goods.
        /// </summary>
        public virtual double ModifyArtisanSellPrice(double multiplier, int tier)
        {
            return multiplier;
        }

        /// <summary>
        /// Adjusts the friendship gained when the produce is given as a gift the villager doesn't dislike or hate (1 is normal).
        /// </summary>
        public virtual double ModifyGiftFriendship(double multiplier, int tier)
        {
            return multiplier;
        }

        /// <summary>
        /// Adjusts the friendship lost when the produce is given as a gift the villager dislikes or hates (1 is normal, higher loses more).
        /// </summary>
        public virtual double ModifyBadGiftFriendship(double multiplier, int tier)
        {
            return multiplier;
        }

        /// <summary>
        /// Changes how much a villager likes the produce as a gift, using the game's gift taste values (such as NPC.gift_taste_dislike). Called whenever the game checks a villager's taste for it.
        /// </summary>
        public virtual int ModifyGiftTaste(int giftTaste, int tier)
        {
            return giftTaste;
        }

        /// <summary>
        /// Extra seeds the Seed Maker adds when it turns the produce into its own seeds. The extras from every trait are added together.
        /// Traits like Barren return a negative number to take seeds away, though the Seed Maker always makes at least one.
        /// </summary>
        public virtual int GetExtraSeeds(Random random, int tier)
        {
            return 0;
        }

        /// <summary>
        /// Changes the traits on seeds the Seed Maker makes from the produce (such as Fading dropping a tier). Only called for the real output, never when the machine checks what it accepts.
        /// The set given is a copy shared by every trait on the produce, so changes from earlier traits are already in it.
        /// </summary>
        public virtual void ModifyMadeSeedTraits(TraitSet seedTraits, Random random, int tier)
        {

        }

        /// <summary>
        /// An exact number of seeds for the Seed Maker to make from the produce, replacing the game's roll and any extras from traits like Seedy. Null leaves the count alone.
        /// The highest count among the crop's traits is used.
        /// </summary>
        public virtual int? GetFixedSeedCount(int tier)
        {
            return null;
        }

        /// <summary>
        /// Adjusts how fast a cask ages an artisan good carrying this trait (1 is normal). The trait must be a price trait (IsPriceTrait) to reach the artisan good.
        /// </summary>
        public virtual double ModifyCaskAgingRate(double multiplier, int tier)
        {
            return multiplier;
        }

        /// <summary>
        /// Adjusts the chance that each of this parent's own traits carries over when crossbred (1 is normal). Traits shared by both parents always carry over.
        /// </summary>
        public virtual double ModifyInheritChance(double multiplier, int tier)
        {
            return multiplier;
        }

        /// <summary>
        /// Adjusts how long a machine takes when the produce is put in it (1 is normal). Casks are left alone since they age by days instead.
        /// </summary>
        public virtual double ModifyMachineTime(double multiplier, int tier)
        {
            return multiplier;
        }

        /// <summary>
        /// Raises the limit on how far the crop's traits can pull on the mutation roll of the same crop type around it (1 is normal). The highest value among the crop's traits is used.
        /// </summary>
        public virtual int GetNeighborBonusCapMultiplier(int tier)
        {
            return 1;
        }

        /// <summary>
        /// What a single tier does, shown on the trait's Field Notes page. An empty string skips the line.
        /// </summary>
        public virtual string GetTierDetails(int tier)
        {
            return string.Empty;
        }

        /// <summary>
        /// Extra rules about the trait shown under its tier details. An empty string skips the line.
        /// </summary>
        public virtual string GetNotes()
        {
            return string.Empty;
        }

        public static int GetRarityWeight(TraitRarity rarity)
        {
            return rarity switch
            {
                TraitRarity.Uncommon => UNCOMMON_WEIGHT,
                TraitRarity.Rare => RARE_WEIGHT,
                _ => COMMON_WEIGHT
            };
        }

        /// <summary>
        /// Reads this trait's own text from the i18n file, such as "details" for "trait.swift.details".
        /// </summary>
        public string GetText(string textKey, object tokens = null)
        {
            return TranslationHelper.Get($"{TraitManager.GetTranslationPrefix(Id)}.{textKey}", tokens);
        }

        // A rounded percentage without its sign, since the i18n text places the sign (some languages put a space before it)
        public static string ToPercentValue(double value)
        {
            return Math.Round(value * 100).ToString();
        }
    }
}
