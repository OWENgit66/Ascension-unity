using System;
namespace Ascension
{
    [Serializable] public sealed class Technique
    {
        public string id="baseline", name="基础功法", description="";
        public float dashCost=18, dashCooldown=2.5f, shieldCost=30, shieldCapacity=45, shieldDuration=2, qiValue=20;
        public void Apply(Balance b)
        {
            b.dashCost=dashCost;b.dashCooldown=dashCooldown;b.shieldCost=shieldCost;
            b.shieldCapacity=shieldCapacity;b.shieldDuration=shieldDuration;b.qiValue=qiValue;
        }
    }
}
