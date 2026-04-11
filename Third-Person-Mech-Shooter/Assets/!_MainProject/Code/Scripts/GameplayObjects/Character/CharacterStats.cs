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

        public event System.Action OnAnyStatisticChanged;


        private void Awake()
        {
            _statisticChanges = new Dictionary<Statistic, StatisticAlteration>();
            _statisticTotals = new Dictionary<Statistic, float>();
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
        private void AddStatisticChange(Statistic statistic, StatisticAlterationType alterationType, float alterationValue)
        {
#if UNITY_EDITOR
            if (!Editor_ValidateStatisticAltertion(statistic, alterationType))
                throw new System.ArgumentException($"{statistic.ToString()} cannot use alteration {alterationType.ToString()}");
#endif

            
            StatisticAlteration statisticAlteration = _statisticChanges.GetOrCreateAndReturnValue(statistic);
            switch (alterationType)
            {
                case StatisticAlterationType.Base:          statisticAlteration.AddBaseChange(alterationValue);       break;
                case StatisticAlterationType.Addition:      statisticAlteration.AddAddition(alterationValue);         break;
                case StatisticAlterationType.Multiplier:    statisticAlteration.AddMultiplication(alterationValue);   break;
            }

            SyncStatisticServerRpc(statistic);
        }

        [Rpc(SendTo.Server)]
        public void RemoveStatisticChangeServerRpc(Statistic statistic, StatisticAlterationType alterationType, float alterationValue, RpcParams rpcParams = default)
        {
            RemoveStatisticChange(statistic, alterationType, alterationValue);
        }
        private void RemoveStatisticChange(Statistic statistic, StatisticAlterationType alterationType, float alterationValue)
        {
#if UNITY_EDITOR
            if (!Editor_ValidateStatisticAltertion(statistic, alterationType))
                throw new System.ArgumentException($"{statistic.ToString()} cannot use alteration {alterationType.ToString()}");
#endif


            StatisticAlteration statisticAlteration = _statisticChanges.GetOrCreateAndReturnValue(statistic);
            switch (alterationType)
            {
                case StatisticAlterationType.Base:          statisticAlteration.RemoveBaseChange(alterationValue);        break;
                case StatisticAlterationType.Addition:      statisticAlteration.RemoveAddition(alterationValue);          break;
                case StatisticAlterationType.Multiplier:    statisticAlteration.RemoveMultiplication(alterationValue);    break;
            }

            SyncStatisticServerRpc(statistic);
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


#if UNITY_EDITOR

        private bool Editor_ValidateStatisticAltertion(Statistic statistic, StatisticAlterationType alteration)
        {
            return statistic switch
            {
                Statistic.DamageResistances => true,
                Statistic.RegeneratingShieldResistances => true,
                Statistic.BoostCount => (alteration == StatisticAlterationType.Base || alteration == StatisticAlterationType.Addition),

                _ => true
            };
        }

        [ContextMenu("Log/Health")] private void Editor_LogHealth() => Editor_LogStatisticChange(Statistic.MaxHealth);
        [ContextMenu("Log/Boost Count")] private void Editor_LogBoostCount() => Editor_LogStatisticChange(Statistic.BoostCount);
        [ContextMenu("Log/Boost Recharge Multiplier")] private void Editor_LogBoostRechargeMultiplier() => Editor_LogStatisticChange(Statistic.BoostRechargeMultiplier);

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
                //Statistic.DamageResistances,

                //Statistic.RegeneratingShield,
                //Statistic.RegeneratingShieldResistances,
                Statistic.ShieldedExternalHeatGainMultiplier => 1.0f,

                Statistic.MaxHeat => _serverCharacter.BuildDataReference.GetFrameData().HeatCapacity,
                Statistic.PersonalHeatGainMultiplier => 1.0f,
                Statistic.ExternalHeatGainMultiplier => 1.0f,

                Statistic.MovementSpeed => _serverCharacter.BuildDataReference.GetFrameData().MovementSpeed,
                Statistic.BoostCount => _serverCharacter.BuildDataReference.GetFrameData().BoostCount,
                Statistic.BoostRechargeMultiplier => 1.0f,

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


            public void AddBaseChange(float baseChangeValue) => _baseChanges.Add(baseChangeValue);
            public void RemoveBaseChange(float baseChangeValue) => _baseChanges.Remove(baseChangeValue);

            public void AddAddition(float baseChangeValue) => _additions.Add(baseChangeValue);
            public void RemoveAddition(float baseChangeValue) => _additions.Remove(baseChangeValue);

            public void AddMultiplication(float baseChangeValue) => _multiplications.Add(baseChangeValue);
            public void RemoveMultiplication(float baseChangeValue) => _multiplications.Remove(baseChangeValue);


            public float GetTotal(float baseValue)
            {
                float total = baseValue;

                // Base Change: If multiple bases exist, determine the base value by adding their offsets from the default and applying that.
                //  (E.g. Default = 5, Overrides = 3 & 6, New Base = ((3 - 5) + (6 - 5) + 5) = (-2 + 1 + 5) = 4).
                foreach (float baseChange in _baseChanges)
                    total += (baseChange - baseValue);

                // Addition: Directly adds to the base value.
                foreach (float addition in _additions)
                    total += addition;

                // Multiplies the value after Base + Addition.
                //  (E.g. Base = 4, Addition = 2, Multiplier = 1.5, Total = (5 + 2) * 1.5 = 9).
                foreach (float multiplication in _multiplications)
                    total *= multiplication;

                return total;
            }


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
    }


    [System.Serializable]
    // (Unless otherwise specified: Addition, Multiplier)
    public enum Statistic
    {
        MaxHealth,      // Implemented - ServerCharacter listens to onChange event and modifies NetworkHealthComponent value.
        // (Damage Type | Base, Addition, Multiplier)
        DamageResistances,

        RegeneratingShield,
        // (Damage Type | Base, Addition, Multiplier)
        RegeneratingShieldResistances,
        ShieldedExternalHeatGainMultiplier,

        MaxHeat,        // Implemented - ServerCharacter's MaxHeat value directly reads from this.
        PersonalHeatGainMultiplier,
        ExternalHeatGainMultiplier,

        MovementSpeed,  // Implemented - ServerCharacter's MovementSpeed value directly reads from this.
        // Base & Addition
        BoostCount,
        BoostRechargeMultiplier,

        KnockbackForceMultiplier,
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