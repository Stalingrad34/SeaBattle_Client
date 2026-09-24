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
	public partial class MatchState : Schema {
#if UNITY_5_3_OR_NEWER
[Preserve]
#endif
public MatchState() { }
		[Type(0, "string")]
		public string matchId = default(string);

		[Type(1, "string")]
		public string phase = default(string);

		[Type(2, "string")]
		public string activePlayerId = default(string);

		[Type(3, "string")]
		public string winnerId = default(string);

		[Type(4, "int64")]
		public long revision = default(long);

		[Type(5, "int64")]
		public long turnId = default(long);

		[Type(6, "float64")]
		public double deadlineMs = default(double);

		[Type(7, "int32")]
		public int boardSize = default(int);

		[Type(8, "int32")]
		public int turnDurationSeconds = default(int);

		[Type(9, "array", typeof(ArraySchema<int>), "int32")]
		public ArraySchema<int> shipLengths = null;

		[Type(10, "int32")]
		public int playerCount = default(int);

		[Type(11, "map", typeof(MapSchema<PlayerState>))]
		public MapSchema<PlayerState> players = null;
	}
}
