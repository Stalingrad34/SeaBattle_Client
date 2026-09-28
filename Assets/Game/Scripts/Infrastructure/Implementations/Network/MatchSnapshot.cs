using System;
using System.Collections.Generic;
using Colyseus.Schema;
using Game.Scripts.Multiplayer.Generated;

namespace Game.Scripts.Infrastructure.Implementations.Network
{
    public static class MatchSnapshot
    {
        // SDK state is mutated in place. Delayed deliveries must own all nested data.
        public static MatchState Copy(MatchState source)
        {
            var state = new MatchState
            {
                matchId = source.matchId, phase = source.phase, activePlayerId = source.activePlayerId,
                winnerId = source.winnerId, revision = source.revision, turnId = source.turnId,
                deadlineMs = source.deadlineMs, boardSize = source.boardSize,
                turnDurationSeconds = source.turnDurationSeconds, playerCount = source.playerCount,
                shipLengths = CopyArray(source.shipLengths, value => value), players = new MapSchema<PlayerState>()
            };
            source.players.ForEach((key, player) => state.players[key] = new PlayerState
            {
                playerId = player.playerId,
                ownShips = CopyArray(player.ownShips, ship => new ShipState { id = ship.id, cells = CopyArray(ship.cells, CopyCell) }),
                incomingShots = CopyArray(player.incomingShots, CopyShot),
                outgoingShots = CopyArray(player.outgoingShots, CopyShot)
            });
            return state;
        }

        private static ShotState CopyShot(ShotState shot)
        {
            return new ShotState { x = shot.x, y = shot.y, result = shot.result, sunkCells = CopyArray(shot.sunkCells, CopyCell) };
        }

        private static CellState CopyCell(CellState cell)
        {
            return new CellState { x = cell.x, y = cell.y };
        }

        private static ArraySchema<T> CopyArray<T>(ArraySchema<T> source, Func<T, T> copy)
        {
            var items = new List<T>();
            if (source != null)
            {
                for (var i = 0; i < source.Count; i++)
                    items.Add(copy(source[i]));
            }
            return new ArraySchema<T>(items);
        }
    }
}
