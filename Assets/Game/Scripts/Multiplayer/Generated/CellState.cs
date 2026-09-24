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
	public partial class CellState : Schema {
#if UNITY_5_3_OR_NEWER
[Preserve]
#endif
public CellState() { }
		[Type(0, "int32")]
		public int x = default(int);

		[Type(1, "int32")]
		public int y = default(int);
	}
}
