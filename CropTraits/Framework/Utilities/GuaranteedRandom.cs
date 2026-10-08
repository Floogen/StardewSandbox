using System;

namespace CropTraits.Framework.Utilities
{
    // Always rolls the lowest value, so every chance check made with it passes
    public class GuaranteedRandom : Random
    {
        protected override double Sample()
        {
            return 0;
        }

        public override double NextDouble()
        {
            return 0;
        }
    }
}
