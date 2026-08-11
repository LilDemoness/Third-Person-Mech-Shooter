using UnityEngine;

namespace Gameplay.Animations.ProceduralAnimations
{
    public class LerpGait
    {
        public float BodyHeight { get; private set; }
        public float TriggerZoneRadius { get; private set; }


        public LerpGait(float bodyHeight, float triggerZoneRadius)
        {
            this.BodyHeight = bodyHeight;
            this.TriggerZoneRadius = triggerZoneRadius;
        }
        public LerpGait(LerpGait other)
        {
            this.BodyHeight = other.BodyHeight;
            this.TriggerZoneRadius = other.TriggerZoneRadius;
        }


        public LerpGait Scale(float scale)
        {
            BodyHeight *= scale;
            TriggerZoneRadius *= scale;
            return this;
        }


        public LerpGait Lerp(LerpGait target, float factor)
        {
            this.BodyHeight = Mathf.Lerp(this.BodyHeight, target.BodyHeight, factor);
            this.TriggerZoneRadius = Mathf.Lerp(this.TriggerZoneRadius, target.TriggerZoneRadius, factor);
            return this;
        }
        public static LerpGait Lerp(LerpGait a, LerpGait b, float t) => new LerpGait(a).Lerp(b, t);
    }
}