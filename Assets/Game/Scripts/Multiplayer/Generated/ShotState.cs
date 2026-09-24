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
	public partial class ShotState : Schema {
#if UNITY_5_3_OR_NEWER
[Preserve]
#endif
public ShotState() { }
		[Type(0, "int32")]
		public int x = default(int);

		[Type(1, "int32")]
		public int y = default(int);

		[Type(2, "string")]
		public string result = default(string);

		[Type(3, "array", typeof(ArraySchema<CellState>))]
		public ArraySchema<CellState> sunkCells = null;
	}
}
