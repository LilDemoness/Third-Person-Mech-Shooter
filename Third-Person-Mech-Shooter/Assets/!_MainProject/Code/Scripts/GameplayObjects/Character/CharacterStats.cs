using Gameplay.Passives;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.GameplayObjects.Character.Statistics
{
    public class CharacterStats : NetworkBehaviour
    {
        [SerializeField] private ServerCharacter _serverCharacter;

        private Dictionary<Statistic, StatisticAlteration> _statisticChanges;   // Server Only.
        private Dictionary<Statistic, float> _statisticTotals;                  // Never accessed on the server.

        private Dictionary<DamageTakenStatistic, DamageTakenAlterations> _damageTakenStatisticChanges;   // Server Only.

        public event System.Action OnAnyStatisticChanged;
        public event System.Action<Statistic> OnStatisticChanged;


        private void Awake()
        {
            _statisticChanges = new Dictionary<Statistic, StatisticAlteration>();
            _statisticTotals = new Dictionary<Statistic, float>();
            _damageTakenStatisticChanges = new Dictionary<DamageTakenStatistic, DamageTakenAlterations>();
        }
        public override void OnNetworkSpawn()
        {
            if (!IsServer)
                SyncAllStatsServerRpc();   // Ensure that late-joiners have the correct information.
        }


        [Rpc(SendTo.Server)]
        public void AddStatisticChangeServerRpc(Statistic statistic, StatisticAlterationType alterationType, float alterationValue, RpcParams rpcParams = default)
        {
            AddStatisticChange(statistic, alterationType, alterationValue);
        }
        public void AddStatisticChange(Statistic statistic, StatisticAlterationType alterationType, float alterationValue)
        {
#if UNITY_EDITOR
            if (!Editor_ValidateStatisticAltertion(statistic, alterationType))
                throw new System.ArgumentException($"{statistic.ToString()} cannot use alteration {alterationType.ToString()}");
#endif

            _statisticChanges.GetOrCreateAndReturnValue(statistic).AddAlteration(alterationValue, alterationType);
            SyncStatisticServerRpc(statistic);
        }


        [Rpc(SendTo.Server)]
        public void RemoveStatisticChangeServerRpc(Statistic statistic, StatisticAlterationType alterationType, float alterationValue, RpcParams rpcParams = default)
        {
            RemoveStatisticChange(statistic, alterationType, alterationValue);
        }
        public void RemoveStatisticChange(Statistic statistic, StatisticAlterationType alterationType, float alterationValue)
        {
#if UNITY_EDITOR
            if (!Editor_ValidateStatisticAltertion(statistic, alterationType))
                throw new System.ArgumentException($"{statistic.ToString()} cannot use alteration {alterationType.ToString()}");
#endif

            if (_statisticChanges.GetOrCreateAndReturnValue(statistic).RemoveAlteration(alterationValue, alterationType))
                SyncStatisticServerRpc(statistic);
        }


        public void AddDamageTakenStatisticChange(ServerCharacter source, DamageTakenStatistic statistic, float value, DamageTypes damageTypes, DirectionalCondition directionalCondition)
        {
            if (directionalCondition != null)
                _damageTakenStatisticChanges.GetOrCreateAndReturnValue(statistic).AddDirectionalDamageTakenAlteration(value, damageTypes, directionalCondition);
            else
                _damageTakenStatisticChanges.GetOrCreateAndReturnValue(statistic).AddDamageTakenAlteration(value, damageTypes);

            //SyncStatisticServerRpc(statistic);
        }
        public void RemoveDamageTakenStatisticChange(ServerCharacter source, DamageTakenStatistic statistic, float value, DamageTypes damageTypes, DirectionalCondition directionalCondition)
        {
            if (directionalCondition != null)
                _damageTakenStatisticChanges.GetOrCreateAndReturnValue(statistic).RemoveDirectionalDamageTakenAlteration(value, damageTypes, directionalCondition);
            else
                _damageTakenStatisticChanges.GetOrCreateAndReturnValue(statistic).RemoveDamageTakenAlteration(value, damageTypes);

            //SyncStatisticServerRpc(statistic);
        }




        [Rpc(SendTo.Server)]
        private void SyncAllStatsServerRpc(RpcParams rpcParams = default)
        {
            Statistic[] statistics = _statisticChanges.Keys.ToArray();

            int statisticsCount = statistics.Length;
            float[] statisticTotals = new float[statisticsCount];
            for(int i = 0; i < statisticsCount; ++i)
                statisticTotals[i] = GetStatisticValue_Server(statistics[i]);
            

            //SyncAllStatsToClientRpc(statistics, statisticTotals, RpcTarget.Single(rpcParams.Receive.SenderClientId, RpcTargetUse.Temp));
            SyncAllStatsToClientRpc(statistics, statisticTotals);
        }
        [Rpc(SendTo.Owner)]
        public void SyncAllStatsToClientRpc(Statistic[] keys, float[] values, RpcParams rpcParams = default)
        {
            _statisticTotals = new Dictionary<Statistic, float>(keys.Length);
            for (int i = 0; i < keys.Length; ++i)
                _statisticTotals.Add(keys[i], values[i]);

            _statisticTotals.LogPairs();
            OnAnyStatisticChanged?.Invoke();
            for (int i = 0; i < keys.Length; ++i)
                OnStatisticChanged?.Invoke(keys[i]);
        }

        [Rpc(SendTo.Server)]
        private void SyncStatisticServerRpc(Statistic statistic, RpcParams rpcParams = default)
        {
            //SyncStatisticClientRpc(statistic, GetOrCreateStatisticAlteration(statistic).Total, RpcTarget.Single(rpcParams.Receive.SenderClientId, RpcTargetUse.Temp));
            SyncStatisticClientRpc(statistic, GetStatisticValue_Server(statistic));
        }
        [Rpc(SendTo.Owner)]
        public void SyncStatisticClientRpc(Statistic statistic, float newTotal, RpcParams rpcParams = default)
        {
            if (!_statisticTotals.TryAdd(statistic, newTotal))
                _statisticTotals[statistic] = newTotal;

            Debug.Log(statistic.ToString() + ": " + newTotal, this);
            OnAnyStatisticChanged?.Invoke();
            OnStatisticChanged?.Invoke(statistic);
        }


        private float GetStatisticValue_Server(Statistic statistic) => _statisticChanges.GetOrCreateAndReturnValue(statistic).GetTotal(GetBaseValue(statistic));
        public float GetStatisticValue(Statistic statistic)
        {
            if (IsServer)
                return GetStatisticValue_Server(statistic);

            if (_statisticTotals.TryGetValue(statistic, out float total))
                return total;

            // Default to the base value.
            return GetBaseValue(statistic);
        }

        public float GetDamageTakenMultiplier(DamageTakenStatistic damageTakenStatistic, DamageTypes damageType, Vector3 damageSourceLocalDirection) => _damageTakenStatisticChanges.GetOrCreateAndReturnValue(damageTakenStatistic).GetTotal(damageSourceLocalDirection, damageType);


#if UNITY_EDITOR

        private bool Editor_ValidateStatisticAltertion(Statistic statistic, StatisticAlterationType alteration)
        {
            return statistic switch
            {
                Statistic.BoostCount => (alteration == StatisticAlterationType.Base || alteration == StatisticAlterationType.Addition),

                _ => true
            };
        }

        [ContextMenu("Log/Health")] private void Editor_LogHealth() => Editor_LogStatisticChange(Statistic.MaxHealth);
        [ContextMenu("Log/Boost Count")] private void Editor_LogBoostCount() => Editor_LogStatisticChange(Statistic.BoostCount);
        [ContextMenu("Log/Boost Recharge Multiplier")] private void Editor_LogBoostRechargeMultiplier() => Editor_LogStatisticChange(Statistic.BoostRechargeMultiplier);
        [ContextMenu("Log/Damage Multiplier")] private void Editor_LogDamageMultiplier() => Editor_LogStatisticChange(Statistic.DamageMultiplier);

        private void Editor_LogStatisticChange(Statistic statistic)
        {
            StatisticAlteration statisticAlteration = _statisticChanges.GetOrCreateAndReturnValue(statistic);
            Debug.Log(statisticAlteration.GetTotal(GetBaseValue(statistic)) + "\n" + statisticAlteration.ToString());
        }

        #endif


        private float GetBaseValue(Statistic statistic)
        {
            return statistic switch
            {
                Statistic.MaxHealth => _serverCharacter.BuildDataReference.GetFrameData().MaxHealth,

                //Statistic.RegeneratingShield,
                //Statistic.RegeneratingShieldResistances,

                Statistic.MaxHeat => _serverCharacter.BuildDataReference.GetFrameData().HeatCapacity,
                Statistic.PersonalHeatGainMultiplier => 1.0f,

                Statistic.MovementSpeed => _serverCharacter.BuildDataReference.GetFrameData().MovementSpeed,
                Statistic.BoostCount => _serverCharacter.BuildDataReference.GetFrameData().BoostCount,
                Statistic.BoostRechargeMultiplier => 1.0f,

                Statistic.DamageMultiplier => 1.0f,
                Statistic.HealingMultiplier => 1.0f,

                _ => 0.0f
            };
        }

        private class StatisticAlteration : INetworkSerializeByMemcpy
        {
            private List<float> _baseChanges;
            private List<float> _additions;
            private List<float> _multiplications;

            public StatisticAlteration()
            {
                _baseChanges = new List<float>();
                _additions = new List<float>();
                _multiplications = new List<float>();
            }


            public void AddAlteration(float alterationValue, StatisticAlterationType alterationType)
            {
                switch (alterationType)
                {
                    case StatisticAlterationType.Base:          AddBaseChange(alterationValue);      break;
                    case StatisticAlterationType.Addition:      AddAddition(alterationValue);        break;
                    case StatisticAlterationType.Multiplier:    AddMultiplication(alterationValue);  break;
                }
            }
            public void AddBaseChange(float baseChangeValue) => _baseChanges.Add(baseChangeValue);
            public void AddAddition(float baseChangeValue) => _additions.Add(baseChangeValue);
            public void AddMultiplication(float baseChangeValue) => _multiplications.Add(baseChangeValue);


            public bool RemoveAlteration(float alterationValue, StatisticAlterationType alterationType)
            {
                return alterationType switch
                {
                    StatisticAlterationType.Base        => RemoveBaseChange(alterationValue),
                    StatisticAlterationType.Addition    => RemoveAddition(alterationValue),
                    StatisticAlterationType.Multiplier  => RemoveMultiplication(alterationValue),

                    _ => throw new System.NotImplementedException($"No implementation for: {alterationType.ToString()}")
                };
            }

            public bool RemoveBaseChange(float baseChangeValue) => _baseChanges.Remove(baseChangeValue);
            public bool RemoveAddition(float baseChangeValue) => _additions.Remove(baseChangeValue);
            public bool RemoveMultiplication(float baseChangeValue) => _multiplications.Remove(baseChangeValue);


            // Base Change: If multiple bases exist, determine the base value by adding their offsets from the default and applying that.
            //  (E.g. Default = 5, Overrides = 3 & 6, New Base = ((3 - 5) + (6 - 5) + 5) = (-2 + 1 + 5) = 4).
            public float ApplyBaseChanges(float baseValue)
            {
                float total = baseValue;
                foreach (float baseChange in _baseChanges)
                    total += (baseChange - baseValue);

                return total;
            }
            // Addition: Directly adds to the base value.
            public float ApplyAdditions(float value)
            {
                foreach (float addition in _additions)
                    value += addition;
                return value;
            }
            // Multiplies the value after Base + Addition.
            //  (E.g. Base = 4, Addition = 2, Multiplier = 1.5, Total = (5 + 2) * 1.5 = 9).
            public float ApplyMultiplications(float value)
            {
                foreach (float multiplication in _multiplications)
                    value *= multiplication;
                return value;
            }
            public float GetTotal(float baseValue) => ApplyMultiplications(ApplyAdditions(ApplyBaseChanges(baseValue)));


            public override string ToString()
            {
                string output = "Base: ";
                foreach (float baseChange in _baseChanges)
                    output += baseChange.ToString() + ", ";

                output += "\nAdditions: ";
                foreach (float addition in _additions)
                    output += addition.ToString() + ", ";

                output += "\nMultiplications: ";
                foreach (float multiplication in _multiplications)
                    output += multiplication.ToString() + ", ";

                return output;
            }
        }
        private class DamageTakenAlterations // One instance for each 'DamageTaken' Statistic
        {
            private Dictionary<DamageTypes, List<DirectionalDamageTypeAlteration>> _directionalDamageTakenAlterations;
            private Dictionary<DamageTypes, DamageTypeAlteration> _damageTakenAlterations;

            private Dictionary<DamageTypes, int> _immunities;

            public DamageTakenAlterations()
            {
                _directionalDamageTakenAlterations = new();
                _damageTakenAlterations = new();
                _immunities = new();
            }


            public void AddDamageTakenAlteration(float alterationValue, DamageTypes damageTypes)
            {
                IEnumerable<DamageTypes> individualDamageTypes = System.Enum.GetValues(typeof(DamageTypes))
                    .Cast<DamageTypes>()
                    .Where(dmg => dmg != DamageTypes.None && dmg != DamageTypes.AllDamage && damageTypes.HasFlag(dmg));


                if (alterationValue <= 0.0f)
                {
                    // This is an immunity, so add it to our immunities list for early exiting.
                    foreach(DamageTypes damageType in individualDamageTypes)
                    {
                        if (!_immunities.TryAdd(damageType, 1))
                            _immunities[damageType] = 1;
                    }
                }
                else
                {
                    foreach (DamageTypes damageType in individualDamageTypes)
                        _damageTakenAlterations.GetOrCreateAndReturnValue(damageType).AddAlteration(alterationValue);
                }
            }
            public void RemoveDamageTakenAlteration(float alterationValue, DamageTypes damageTypes)
            {
                IEnumerable<DamageTypes> individualDamageTypes = System.Enum.GetValues(typeof(DamageTypes))
                    .Cast<DamageTypes>()
                    .Where(dmg => dmg != DamageTypes.None && dmg != DamageTypes.AllDamage && damageTypes.HasFlag(dmg));

                if (alterationValue <= 0.0f)
                {
                    // Immunities are always only added to the '_immunities' dictionary and not the actual values list.
                    foreach (DamageTypes damageType in individualDamageTypes)
                        _immunities[damageType] -= 1;
                }
                else
                {
                    // Remove the alteration.
                    foreach (DamageTypes damageType in individualDamageTypes)
                        _damageTakenAlterations.GetOrCreateAndReturnValue(damageType).RemoveAlteration(alterationValue);
                }
            }


            public void AddDirectionalDamageTakenAlteration(float alterationValue, DamageTypes damageTypes, DirectionalCondition condition)
            {
                IEnumerable<DamageTypes> individualDamageTypes = System.Enum.GetValues(typeof(DamageTypes))
                    .Cast<DamageTypes>()
                    .Where(dmg => dmg != DamageTypes.None && dmg != DamageTypes.AllDamage && damageTypes.HasFlag(dmg));


                // Due to these alterations being conditional, caching a set value for immunities is less effective
                //  and so we've elected to not do so.
                foreach (DamageTypes damageType in individualDamageTypes)
                {
                    var alterations = _directionalDamageTakenAlterations.GetOrCreateAndReturnValue(damageType);

                    DirectionalDamageTypeAlteration correspondingAlteration = alterations.FirstOrDefault(dmgAlteration => dmgAlteration.ConditionEquals(condition));
                    if (correspondingAlteration != null)
                        correspondingAlteration.AddAlteration(alterationValue);
                    else
                        alterations.Add(new DirectionalDamageTypeAlteration(alterationValue, condition));
                }
            }
            public void RemoveDirectionalDamageTakenAlteration(float alterationValue, DamageTypes damageTypes, DirectionalCondition condition)
            {
                IEnumerable<DamageTypes> individualDamageTypes = System.Enum.GetValues(typeof(DamageTypes))
                    .Cast<DamageTypes>()
                    .Where(dmg => dmg != DamageTypes.None && dmg != DamageTypes.AllDamage && damageTypes.HasFlag(dmg));

                // Due to these alterations being conditional, caching a set value for immunities is less effective
                //  and so we've elected to not do so.
                foreach (DamageTypes damageType in individualDamageTypes)
                {
                    if (!_directionalDamageTakenAlterations.TryGetValue(damageType, out var alterations))
                        continue;

                    foreach (DirectionalDamageTypeAlteration dirDmgAlteration in alterations)
                    {
                        if (!dirDmgAlteration.ConditionEquals(condition))
                            continue;

                        dirDmgAlteration.RemoveAlteration(alterationValue);
                    }
                }
            }


            /// <summary>
            ///     Calculates and returns the total.
            /// </summary>
            /// <returns> A positive float value representing the multiplier to be applied to the damage taken.</returns>
            public float GetTotal(Vector3 damageSourceLocalDirection, DamageTypes damageType)
            {
                if (_immunities.ContainsKey(damageType))
                    return 0.0f;    // Exit early for immunities to save time.

                float total = _damageTakenAlterations.GetOrCreateAndReturnValue(damageType).GetValue();
                if (_directionalDamageTakenAlterations.TryGetValue(damageType, out List<DirectionalDamageTypeAlteration> directionalAlterations))
                {
                    foreach(DirectionalDamageTypeAlteration alteration in directionalAlterations)
                    {
                        if (alteration.Evaluate(damageSourceLocalDirection))
                            total *= alteration.GetValue();
                    }
                }

                return total;
            }


            private class DirectionalDamageTypeAlteration
            {
                private readonly DirectionalCondition _condition;
                private DamageTypeAlteration _alteration;

                public DirectionalDamageTypeAlteration(float alterationValue, DirectionalCondition condition)
                {
                    _condition = condition;

                    _alteration = new();
                    _alteration.AddAlteration(alterationValue);
                }

                public bool Evaluate(Vector3 damageSourceLocalDirection) => _condition.Evaluate(damageSourceLocalDirection);

                public void AddAlteration(float value) => _alteration.AddAlteration(value);
                public void RemoveAlteration(float value) => _alteration.RemoveAlteration(value);
                public float GetValue() => _alteration.GetValue();

                public bool ConditionEquals(DirectionalCondition condition) => _condition.Equals(condition);
            }
            private class DamageTypeAlteration
            {
                private float _alteration;
                private List<float> _allAlterations;

                public DamageTypeAlteration()
                {
                    _alteration = 1.0f;
                    _allAlterations = new List<float>();
                }

                public void AddAlteration(float alteration)
                {
                    _allAlterations.Add(alteration);
                    RecalculateTotal();
                }
                public void RemoveAlteration(float alteration)
                {
                    _allAlterations.Remove(alteration);
                    RecalculateTotal();
                }

                private void RecalculateTotal()
                {
                    _alteration = 1.0f;
                    foreach(float alteration in _allAlterations)
                        _alteration *= alteration;
                }
                public float GetValue() => _alteration;
            }
        }
    }


    [System.Serializable]
    // (Unless otherwise specified: Addition, Multiplier)
    public enum Statistic
    {
        MaxHealth,      // Implemented - ServerCharacter listens to onChange event and modifies NetworkHealthComponent value.

        RegeneratingShield,

        MaxHeat,        // Implemented - ServerCharacter's MaxHeat value directly reads from this.
        PersonalHeatGainMultiplier, // Implemented - ServerCharacter reads from this in 'ReceiveHeatChange'.

        MovementSpeed,  // Implemented - ServerCharacterMovement's MovementSpeed value reads directly from this.
        // Base & Addition
        BoostCount,     // Implemented - ServerCharacterMovement's _boostCount value reads directly from this | ServerCharacterMovement is also listening to changes.
        BoostRechargeMultiplier,    // Implemented - ServerCharacterMovement's _boostRechargeMultiplier value reads directly from this.

        KnockbackForceMultiplier,

        DamageMultiplier,   // Implemented - DamageEffect reads directly from this when calculating the damage to apply.
        HealingMultiplier,  // Implemented - HealingEffect reads directly from this when calculating the healing to apply.
    }
    [System.Serializable]
    // (Damage Type | Base, Addition, Multiplier)
    public enum DamageTakenStatistic
    {
        DamageResistances,
        RegeneratingShieldResistances,
    }
    [System.Serializable]
    public enum StatisticAlterationType
    {
        // Overrides the default base value.
        // If multiple bases exist, determine the base value by adding their offsets from the default and applying that (E.g. Default = 5, Overrides = 3 & 6, New Base = ((3 - 5) + (6 - 5) + 5) = (-2 + 1 + 5) = 4).
        Base,
        // Adds to the base value.
        Addition,
        // Multiplies the value after Base + Addition (E.g. Base = 4, Addition = 2, Multiplier = 1.5, Total = (5 + 2) * 1.5 = 9).
        Multiplier,
    }
}