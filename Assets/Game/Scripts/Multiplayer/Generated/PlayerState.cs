// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 3.0.76
// 

using Colyseus.Schema;
#if UNITY_5_3_OR_NEWER
using UnityEngine.Scripting;
#endif

namespace Game.Scripts.Multiplayer.Generated {
	public partial class PlayerState : Schema {
#if UNITY_5_3_OR_NEWER
[Preserve]
#endif
public PlayerState() { }
		[Type(0, "string")]
		public string playerId = default(string);

		[Type(1, "array", typeof(ArraySchema<ShipState>))]
		public ArraySchema<ShipState> ownShips = null;

		[Type(2, "array", typeof(ArraySchema<ShotState>))]
		public ArraySchema<ShotState> incomingShots = null;

		[Type(3, "array", typeof(ArraySchema<ShotState>))]
		public ArraySchema<ShotState> outgoingShots = null;
	}
}
