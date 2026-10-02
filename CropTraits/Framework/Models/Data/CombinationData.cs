namespace CropTraits.Framework.Models.Data
{
    public class CombinationData
    {
        public string FirstTraitId { get; set; }
        public string SecondTraitId { get; set; }
        public string ResultTraitId { get; set; }
        public double Chance { get; set; } = 0.25;

        public string Key => $"{FirstTraitId}+{SecondTraitId}>{ResultTraitId}";
    }
}
