using System;

namespace CropTraits.Framework.Models
{
    // How many more monsters crops can spawn in one location this morning, shared by every crop there
    public class MonsterSpawnLimit
    {
        public int Remaining { get; private set; }

        public MonsterSpawnLimit(int remaining)
        {
            Remaining = Math.Max(remaining, 0);
        }

        public bool TryUse()
        {
            if (Remaining <= 0)
            {
                return false;
            }

            Remaining--;
            return true;
        }
    }
}
