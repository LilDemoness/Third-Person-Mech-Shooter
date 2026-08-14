using UnityEngine;

namespace Gameplay.Animations.ProceduralAnimations
{
    [System.Serializable]
    public class ConditionalGait
    {
        public Gait Gait;
        [SerializeReference, SubclassSelector] public GaitCondition[] Conditions;

        public bool TestCondition(MechBody body)
        {
            for(int i = 0; i < Conditions.Length; ++i)
            {
                if (!Conditions[i].TestCondition(body))
                    return false;   // Condition failed.
            }

            // All conditions passed.
            return true;
        }
    }
}