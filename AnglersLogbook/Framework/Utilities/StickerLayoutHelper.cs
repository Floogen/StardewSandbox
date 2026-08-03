using AnglersLogbook.Framework.Models.Stickers;
using Microsoft.Xna.Framework;
using StardewValley.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AnglersLogbook.Framework.Utilities
{
    /// <summary>
    /// Handles the random arrangement of stickers within a bounded area.
    /// </summary>
    public static class StickerLayoutHelper
    {
        /// <summary>
        /// Randomly places the given stickers within the given area, at a random scale each.
        /// Empty space is always used first, so a sticker only overlaps once no free spot remains.
        /// </summary>
        /// <param name="stickers">The stickers to place</param>
        /// <param name="area">The area to place them within, in screen pixels</param>
        /// <param name="exclusions">Areas within the placement area which no sticker may cover, such as a title or a portrait</param>
        /// <param name="salt">An extra value to vary the layout, so separate pages do not arrange identically</param>
        /// <param name="minScale">The smallest scale a large sticker may be drawn at</param>
        /// <param name="maxScale">The largest scale a large sticker may be drawn at</param>
        /// <param name="largeScaleChance">The chance a sticker is drawn large rather than small</param>
        /// <param name="spacing">The gap to leave between stickers while free space remains, in screen pixels</param>
        /// <param name="gridStep">The spacing between candidate positions, in screen pixels. Smaller values pack tighter but cost more to solve</param>
        /// <param name="packingSamples">How many free positions to weigh against each other before picking one. 1 takes the first free spot, which scatters stickers and wastes space. Higher values settle them against their neighbors, leaving fewer unusable gaps</param>
        /// <param name="preserveOrder">Whether to place the stickers in the given order rather than largest first. Largest first fills the area far better, but reorders the layout whenever a new sticker joins the set</param>
        /// <returns>The placements, ordered back to front</returns>
        public static List<StickerPlacement> Arrange(IEnumerable<StickerData> stickers, Rectangle area, IEnumerable<Rectangle> exclusions = null, int salt = 0, int minScale = 4, int maxScale = 6, float largeScaleChance = 0.2f, int spacing = 2, int gridStep = 4, int packingSamples = 12, bool preserveOrder = false)
        {
            List<StickerPlacement> placements = new List<StickerPlacement>();

            if (stickers is null || area.Width <= 0 || area.Height <= 0)
            {
                return placements;
            }

            if (minScale > maxScale)
            {
                (minScale, maxScale) = (maxScale, minScale);
            }

            gridStep = Math.Max(1, gridStep);

            // only the parts of an exclusion which fall inside the area matter, and an exclusion outside it is discarded
            List<Rectangle> blocked = exclusions is null ? new List<Rectangle>() : exclusions.Select(r => Rectangle.Intersect(r, area)).Where(r => r.Width > 0 && r.Height > 0).ToList();

            var pending = stickers.Where(s => s is not null).Select(s => CreatePlacement(s, salt, minScale, maxScale, largeScaleChance)).ToList();

            if (preserveOrder is false)
            {
                // largest first packs the area better, at the cost of reordering whenever a new sticker joins the set
                pending = pending.OrderByDescending(p => p.Footprint.X * p.Footprint.Y).ToList();
            }

            List<Rectangle> occupied = new List<Rectangle>();

            foreach (StickerPlacement placement in pending)
            {
                Point footprint = placement.Footprint;
                List<Point> candidates = BuildCandidates(area, footprint, gridStep, blocked);

                // the candidate order is seeded off the sticker itself, so its preferred spots never change
                Shuffle(candidates, StickerHelper.CreateStickerRandom(placement.Sticker.Id, salt + 1));

                if (TryFindFreeSpot(candidates, footprint, occupied, area, spacing, gridStep, packingSamples, out Point position) is false)
                {
                    position = FindLeastCrowdedSpot(candidates, footprint, occupied);
                    placement.IsOverlapping = true;
                }

                placement.Position = new Vector2(position.X, position.Y);

                occupied.Add(new Rectangle(position.X, position.Y, footprint.X, footprint.Y));
                placements.Add(placement);
            }

            return placements;
        }

        /// <summary>
        /// Rolls the scale and flip for the given sticker.
        /// </summary>
        private static StickerPlacement CreatePlacement(StickerData sticker, int salt, int minScale, int maxScale, float largeScaleChance)
        {
            // one random is shared across every roll, since a fresh instance on the same seed would repeat the first draw
            Random random = StickerHelper.CreateStickerRandom(sticker.Id, salt);

            int smallMinScale = Math.Max(1, minScale / 2);
            int smallMaxScale = Math.Max(smallMinScale, minScale - 1);

            return new StickerPlacement()
            {
                Sticker = sticker,
                Scale = random.NextBool(largeScaleChance) ? random.Next(minScale, maxScale + 1) : random.Next(smallMinScale, smallMaxScale + 1),
                IsFlipped = random.NextBool(0.25f)
            };
        }

        /// <summary>
        /// Builds every candidate top-left position at which the given footprint fits inside the given area, without covering an exclusion.
        /// </summary>
        private static List<Point> BuildCandidates(Rectangle area, Point footprint, int gridStep, List<Rectangle> blocked)
        {
            List<Point> candidates = new List<Point>();

            // clamping to the left/top guarantees at least one candidate, even for a sticker too large for the area
            int maxX = Math.Max(area.Left, area.Right - footprint.X);
            int maxY = Math.Max(area.Top, area.Bottom - footprint.Y);

            for (int y = area.Top; y <= maxY; y += gridStep)
            {
                for (int x = area.Left; x <= maxX; x += gridStep)
                {
                    TryAddCandidate(candidates, x, y, footprint, blocked);
                }

                // the loop can stop short of the right edge, which would bias placements away from it
                if ((maxX - area.Left) % gridStep is not 0)
                {
                    TryAddCandidate(candidates, maxX, y, footprint, blocked);
                }
            }

            if ((maxY - area.Top) % gridStep is not 0)
            {
                for (int x = area.Left; x <= maxX; x += gridStep)
                {
                    TryAddCandidate(candidates, x, maxY, footprint, blocked);
                }

                TryAddCandidate(candidates, maxX, maxY, footprint, blocked);
            }

            if (candidates.Count is 0 && blocked.Count > 0)
            {
                // the exclusions leave nowhere valid, so they are ignored rather than dropping the sticker entirely
                return BuildCandidates(area, footprint, gridStep, new List<Rectangle>());
            }

            return candidates;
        }

        /// <summary>
        /// Adds the given position as a candidate, unless placing the footprint there would cover an exclusion.
        /// </summary>
        private static void TryAddCandidate(List<Point> candidates, int x, int y, Point footprint, List<Rectangle> blocked)
        {
            Rectangle bounds = new Rectangle(x, y, footprint.X, footprint.Y);

            foreach (Rectangle exclusion in blocked)
            {
                if (exclusion.Intersects(bounds))
                {
                    return;
                }
            }

            candidates.Add(new Point(x, y));
        }

        /// <summary>
        /// Finds a candidate position which touches nothing already placed, preferring one which settles against its neighbors.
        /// </summary>
        private static bool TryFindFreeSpot(List<Point> candidates, Point footprint, List<Rectangle> occupied, Rectangle area, int spacing, int gridStep, int packingSamples, out Point position)
        {
            position = Point.Zero;
            packingSamples = Math.Max(1, packingSamples);

            List<Point> free = new List<Point>();

            foreach (Point candidate in candidates)
            {
                Rectangle bounds = new Rectangle(candidate.X - spacing, candidate.Y - spacing, footprint.X + spacing * 2, footprint.Y + spacing * 2);

                if (occupied.Any(r => r.Intersects(bounds)))
                {
                    continue;
                }

                free.Add(candidate);

                // the candidates are shuffled, so the first few free ones are already a random sample of the free space
                if (free.Count >= packingSamples)
                {
                    break;
                }
            }

            if (free.Count is 0)
            {
                return false;
            }

            if (free.Count is 1 || packingSamples is 1)
            {
                position = free[0];

                return true;
            }

            long bestContact = long.MinValue;
            int reach = Math.Max(1, spacing + gridStep);

            foreach (Point candidate in free)
            {
                long contact = GetContact(candidate, footprint, occupied, area, reach);

                if (contact > bestContact)
                {
                    position = candidate;
                    bestContact = contact;
                }
            }

            return true;
        }

        /// <summary>
        /// Scores how much of the given position abuts an already placed sticker or the edge of the area.
        /// A higher score means less dead space is left stranded around the sticker.
        /// </summary>
        private static long GetContact(Point candidate, Point footprint, List<Rectangle> occupied, Rectangle area, int reach)
        {
            // the footprint is grown slightly, so anything it would have been sitting next to now registers as an overlap
            Rectangle probe = new Rectangle(candidate.X - reach, candidate.Y - reach, footprint.X + reach * 2, footprint.Y + reach * 2);
            long contact = 0;

            foreach (Rectangle other in occupied)
            {
                Rectangle intersection = Rectangle.Intersect(probe, other);

                contact += (long)intersection.Width * intersection.Height;
            }

            // the area edges count too, otherwise stickers drift inward and leave a wasted border
            if (probe.Left < area.Left)
            {
                contact += (long)(area.Left - probe.Left) * footprint.Y;
            }

            if (probe.Top < area.Top)
            {
                contact += (long)(area.Top - probe.Top) * footprint.X;
            }

            if (probe.Right > area.Right)
            {
                contact += (long)(probe.Right - area.Right) * footprint.Y;
            }

            if (probe.Bottom > area.Bottom)
            {
                contact += (long)(probe.Bottom - area.Bottom) * footprint.X;
            }

            return contact;
        }

        /// <summary>
        /// Finds the candidate position which overlaps the least of what is already placed.
        /// The candidates are pre-shuffled, so ties resolve randomly rather than toward the top-left.
        /// </summary>
        private static Point FindLeastCrowdedSpot(List<Point> candidates, Point footprint, List<Rectangle> occupied)
        {
            Point best = candidates.FirstOrDefault();
            long bestOverlap = long.MaxValue;

            foreach (Point candidate in candidates)
            {
                Rectangle bounds = new Rectangle(candidate.X, candidate.Y, footprint.X, footprint.Y);
                long overlap = 0;

                foreach (Rectangle other in occupied)
                {
                    Rectangle intersection = Rectangle.Intersect(bounds, other);
                    overlap += (long)intersection.Width * intersection.Height;
                }

                if (overlap < bestOverlap)
                {
                    best = candidate;
                    bestOverlap = overlap;

                    if (bestOverlap is 0)
                    {
                        break;
                    }
                }
            }

            return best;
        }

        /// <summary>
        /// Shuffles the given list in place, using the given random source rather than the shared game one.
        /// </summary>
        private static void Shuffle<T>(IList<T> list, Random random)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);

                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
