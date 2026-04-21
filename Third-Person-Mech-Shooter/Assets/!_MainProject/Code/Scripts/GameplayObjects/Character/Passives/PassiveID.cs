using System;
using Unity.Netcode;

namespace Gameplay.Passives
{
    /// <summary>
    ///     A struct used by the Passives system to refer to a specific passive in runetime.<br/>
    ///     Wraps a simple integer to allow for referencing Passives over the network.
    /// </summary>
    public readonly struct PassiveID : INetworkSerializeByMemcpy, IEquatable<PassiveID>
    {
        public readonly int ID;

        public PassiveID(int id) => this.ID = id;

        public bool Equals(PassiveID other) => ID == other.ID;
        public override bool Equals(object obj) => obj is PassiveID other && Equals(other);
        public override int GetHashCode() => ID;

        public static bool operator ==(PassiveID x, PassiveID y) => x.Equals(y);
        public static bool operator !=(PassiveID x, PassiveID y) => !(x == y);

        public override string ToString() => $"PassiveID({ID})";
    }
}