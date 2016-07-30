namespace GearBuffs
{
    public class GearBuff
    {
        public int item;
        public int buff, duration, range;
        public string aura;
        public string held;
        public int Id;

        public GearBuff(int item, int buff, int duration, string held, string aura, int range)
        {
            this.item = item;
            this.buff = buff;
            this.duration = duration;
            this.held = held;
            this.aura = aura;
            this.range = range;
        }
    }
}
