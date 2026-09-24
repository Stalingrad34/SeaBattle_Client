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
	public partial class ShipState : Schema {
#if UNITY_5_3_OR_NEWER
[Preserve]
#endif
public ShipState() { }
		[Type(0, "string")]
		public string id = default(string);

		[Type(1, "array", typeof(ArraySchema<CellState>))]
		public ArraySchema<CellState> cells = null;
	}
}
