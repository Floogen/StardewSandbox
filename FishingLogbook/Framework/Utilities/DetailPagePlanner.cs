using FishingLogbook.Framework.Models.Fish;
using Parchment.Framework.API;
using System;
using System.Collections.Generic;

namespace FishingLogbook.Framework.Utilities
{
    /// <summary>
    /// Answers the overflow checks which paginate a fish's detail pages, from a plan recorded the first time that fish was laid out.
    /// The layout depends only on the page text, which a catch never changes, so the measurements are worth keeping between builds.
    /// </summary>
    /// <remarks>May be worth implementing something similar in Parchment? To help cut cost of rebuilding books with unchanged text.</remarks>
    public class DetailPagePlanner
    {
        private static readonly Dictionary<string, DetailPagePlan> _plans = new Dictionary<string, DetailPagePlan>(StringComparer.OrdinalIgnoreCase);

        private readonly DetailPagePlan _plan;
        private int _answerIndex;

        private DetailPagePlanner(DetailPagePlan plan)
        {
            _plan = plan;
        }

        /// <summary>
        /// Gets a planner for one fish, reusing its recorded plan when the given signature still matches.
        /// </summary>
        /// <param name="fishId">The fish's qualified item ID</param>
        /// <param name="signature">A value identifying the text the pages will be built from</param>
        public static DetailPagePlanner For(string fishId, int signature)
        {
            if (_plans.TryGetValue(fishId, out DetailPagePlan plan) is false)
            {
                plan = new DetailPagePlan();
                _plans[fishId] = plan;
            }

            // the text has changed, so what was measured against the old text says nothing about the new
            if (plan.Signature != signature)
            {
                plan.Answers.Clear();
                plan.Signature = signature;
            }

            return new DetailPagePlanner(plan);
        }

        /// <summary>
        /// Discards every recorded plan.
        /// </summary>
        public static void ClearPlans()
        {
            _plans.Clear();
        }

        /// <summary>
        /// Answers whether the page has run out of room, measuring only when the plan has nothing recorded for this point.
        /// </summary>
        /// <param name="page">The page being filled</param>
        public bool WouldOverflow(IPageBuilder page)
        {
            if (_answerIndex < _plan.Answers.Count)
            {
                return _plan.Answers[_answerIndex++];
            }

            bool wouldOverflow = page.WouldOverflow();
            _plan.Answers.Add(wouldOverflow);
            _answerIndex += 1;

            return wouldOverflow;
        }
    }
}
