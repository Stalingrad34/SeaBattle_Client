using System;
using Colyseus.Schema;
using Game.Scripts.Multiplayer.Generated;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.Infrastructure.Implementations.UI.Popups.BattlePopup
{
    public sealed class BoardView : MonoBehaviour
    {
        [SerializeField] private GridLayoutGroup grid;
        [SerializeField] private BoardCellView cellPrefab;
        private BoardCellView[] _cells;
        private int _size;

        public void Init(int size, Action<int, int> clicked)
        {
            if (_cells != null && size == _size)
                return;
            if (_cells != null)
            {
                foreach (var cell in _cells)
                    Destroy(cell.gameObject);
            }
            _size = size;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = size;
            var width = ((RectTransform)grid.transform).rect.width;
            var side = (width - grid.padding.horizontal - grid.spacing.x * (size - 1)) / size;
            grid.cellSize = new Vector2(side, side);
            _cells = new BoardCellView[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var cell = Instantiate(cellPrefab, grid.transform);
                    cell.Init(x, y, clicked);
                    _cells[y * size + x] = cell;
                }
            }
        }

        public void Render(PlayerState player, bool own, Func<int, int, bool> canShoot, int pendingX, int pendingY)
        {
            foreach (var cell in _cells)
                cell.Render("", new Color(.12f, .19f, .28f), false);
            if (own)
            {
                for (var i = 0; i < player.ownShips.Count; i++)
                {
                    var ship = player.ownShips[i];
                    for (var c = 0; c < ship.cells.Count; c++)
                        At(ship.cells[c].x, ship.cells[c].y).Render("■", new Color(.18f, .46f, .59f), false);
                }
            }
            else
            {
                for (var y = 0; y < _size; y++)
                {
                    for (var x = 0; x < _size; x++)
                        At(x, y).SetInteractable(canShoot(x, y));
                }
            }
            RenderShots(own ? player.incomingShots : player.outgoingShots);
            if (!own && pendingX >= 0 && pendingY >= 0)
                At(pendingX, pendingY).Render("…", new Color(.64f, .44f, .13f), false);
        }

        private void RenderShots(ArraySchema<ShotState> shots)
        {
            for (var i = 0; i < shots.Count; i++)
            {
                var shot = shots[i];
                At(shot.x, shot.y).Render(shot.result == "miss" ? "•" : "×",
                    shot.result == "miss" ? new Color(.25f, .34f, .43f) : new Color(.85f, .34f, .22f), false);
            }
            for (var i = 0; i < shots.Count; i++)
            {
                for (var c = 0; c < shots[i].sunkCells.Count; c++)
                {
                    var cell = shots[i].sunkCells[c];
                    At(cell.x, cell.y).Render("×", new Color(.53f, .16f, .23f), false);
                }
            }
        }

        private BoardCellView At(int x, int y)
        {
            return _cells[y * _size + x];
        }
    }
}
