using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RoyalThrone.Match3Vfx.Demo
{
    /// <summary>
    /// Self-contained preview board. Add to an empty GameObject in an empty scene and press Play.
    /// The board logic here is deliberately tiny: it only shows how a game connects to the kit
    /// (host, board, HitCell, Pop, Fall). Your game keeps its own match rules.
    /// </summary>
    public sealed class Match3VfxDemo : MonoBehaviour, IMatch3Board
    {
        sealed class Piece
        {
            public Transform tr;
            public SpriteRenderer sr;
            public int color;
            public bool busy;
        }

        [Tooltip("Optional. Assign the Royal Throne art pack settings for sprites, painted chips and sound.")]
        public Match3VfxSettings settings;
        [Tooltip("Optional piece sprites by colour index. Empty = generated gems tinted by palette.")]
        public Sprite[] pieceSprites = new Sprite[0];
        [Range(4, 10)] public int columns = 8;
        [Range(4, 10)] public int rows = 8;
        public float pitch = 1f;

        static readonly Color[] Palette =
        {
            new Color(0.93f, 0.2f, 0.25f), new Color(0.25f, 0.55f, 1f), new Color(1f, 0.8f, 0.2f),
            new Color(0.35f, 0.85f, 0.35f), new Color(0.85f, 0.35f, 0.95f), new Color(0.95f, 0.95f, 0.95f)
        };

        Piece[,] pieces;
        readonly Match3VfxHost host = new Match3VfxHost();
        int cascade;
        Vector2 scroll;

        public int Columns => columns;
        public int Rows => rows;
        int ColorCount => pieceSprites != null && pieceSprites.Length > 0 ? pieceSprites.Length : 5;

        void Awake()
        {
            if (settings) Match3Vfx.Settings = settings;
            host.cellSize = pitch;
            Match3Vfx.Configure(null, host, this);
            host.boardBottomY = CellToWorld(Vector2Int.zero).y;
            EnsureCamera();
            BuildBoard();
            Match3Vfx.Prewarm();
        }

        void OnDestroy()
        {
            if (ReferenceEquals(Match3Vfx.Board, this)) Match3Vfx.Board = null;
            Match3Vfx.Clear();
        }

        void EnsureCamera()
        {
            var camera = Camera.main;
            if (!camera)
            {
                camera = new GameObject("Main Camera").AddComponent<Camera>();
                camera.tag = "MainCamera";
            }
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(rows, columns) * pitch * 0.62f + 1.5f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.07f, 0.12f, 0.28f);
        }

        void BuildBoard()
        {
            pieces = new Piece[columns, rows];
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    var tile = new GameObject("Cell " + x + "," + y).AddComponent<SpriteRenderer>();
                    tile.transform.SetParent(transform, false);
                    tile.transform.position = CellToWorld(new Vector2Int(x, y)) + Vector3.forward * 0.5f;
                    tile.sprite = Match3Shapes.GemSprite;
                    tile.color = (x + y) % 2 == 0 ? new Color(0.16f, 0.24f, 0.46f) : new Color(0.13f, 0.2f, 0.4f);
                    tile.transform.localScale = Vector3.one * pitch * 0.98f;
                    tile.sortingOrder = 0;

                    var sr = new GameObject("Piece").AddComponent<SpriteRenderer>();
                    sr.transform.SetParent(transform, false);
                    sr.sortingOrder = 2;
                    var piece = new Piece { tr = sr.transform, sr = sr };
                    pieces[x, y] = piece;
                    Recolor(piece);
                    piece.tr.position = CellToWorld(new Vector2Int(x, y));
                }
            }
        }

        void Recolor(Piece piece)
        {
            piece.color = Random.Range(0, ColorCount);
            bool art = pieceSprites != null && pieceSprites.Length > 0 && pieceSprites[piece.color];
            piece.sr.sprite = art ? pieceSprites[piece.color] : Match3Shapes.GemSprite;
            piece.sr.color = art ? Color.white : Palette[piece.color % Palette.Length];
            Vector3 size = piece.sr.sprite.bounds.size;
            piece.tr.localScale = Vector3.one * pitch * 0.88f / Mathf.Max(0.0001f, Mathf.Max(size.x, size.y));
        }

        Color BurstColor(Piece piece) => Palette[piece.color % Palette.Length];

        // ------------------------------------------------------------------ IMatch3Board

        public bool IsCell(Vector2Int c) => c.x >= 0 && c.y >= 0 && c.x < columns && c.y < rows;

        public Vector3 CellToWorld(Vector2Int c) =>
            new Vector3((c.x - (columns - 1) * 0.5f) * pitch, (c.y - (rows - 1) * 0.5f) * pitch, 0f);

        public void HitCell(Vector2Int c, Match3HitSource source)
        {
            var piece = pieces[c.x, c.y];
            if (piece.busy) return;
            StartCoroutine(ClearAndRefill(piece, c, source == Match3HitSource.Rocket ? Match3PopKind.Rocket : Match3PopKind.PowerClear, 0f, 1f));
        }

        public bool TryPickHelicopterTarget(Vector2Int origin, ICollection<Vector2Int> reserved, out Vector2Int target)
        {
            target = default;
            float best = -1f;
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    var c = new Vector2Int(x, y);
                    if (c == origin || reserved.Contains(c) || pieces[x, y].busy) continue;
                    float score = Vector2Int.Distance(c, origin) + Random.value * 3f;
                    if (score > best) { best = score; target = c; }
                }
            }
            return best >= 0f;
        }

        // ------------------------------------------------------------------ flow

        IEnumerator ClearAndRefill(Piece piece, Vector2Int cell, Match3PopKind kind, float stagger, float intensity)
        {
            piece.busy = true;
            Vector3 rest = piece.tr.localScale;
            yield return Match3PieceMotion.Pop(piece.tr, piece.sr, new Match3PopOptions
            {
                kind = kind,
                burstColor = BurstColor(piece),
                colorIndex = piece.color,
                intensity = intensity,
                stagger = stagger,
                restScale = rest
            });
            yield return new WaitForSeconds(0.08f);
            Recolor(piece);
            var home = CellToWorld(cell);
            piece.tr.position = home + Vector3.up * pitch * (rows - cell.y + 1);
            piece.sr.enabled = true;
            yield return Match3PieceMotion.FallAndLand(piece.tr, home, piece.tr.localScale);
            piece.busy = false;
        }

        bool TryFreeLine(out List<Vector2Int> cells)
        {
            cells = new List<Vector2Int>(3);
            for (int attempt = 0; attempt < 20; attempt++)
            {
                cells.Clear();
                int y = Random.Range(0, rows), x = Random.Range(0, columns - 2);
                for (int i = 0; i < 3; i++) if (!pieces[x + i, y].busy) cells.Add(new Vector2Int(x + i, y));
                if (cells.Count == 3) return true;
            }
            return false;
        }

        void PlayMatch(int size)
        {
            if (!TryFreeLine(out var cells)) return;
            if (size > 3 && cells[2].x + 1 < columns && !pieces[cells[2].x + 1, cells[2].y].busy) cells.Add(cells[2] + Vector2Int.right);
            var positions = new List<Vector3>();
            var first = pieces[cells[0].x, cells[0].y];
            foreach (var c in cells)
            {
                var p = pieces[c.x, c.y];
                p.color = first.color;
                p.sr.sprite = first.sr.sprite;
                p.sr.color = first.sr.color;
                positions.Add(p.tr.position);
            }
            float intensity = Match3Vfx.PlayMatchGroup(positions, BurstColor(first), cells.Count, Mathf.Max(1, cascade));
            for (int i = 0; i < cells.Count; i++)
                StartCoroutine(ClearAndRefill(pieces[cells[i].x, cells[i].y], cells[i], Match3PopKind.Match, Match3Vfx.StaggerFor(i), intensity));
        }

        IEnumerator Cascade()
        {
            for (cascade = 1; cascade <= 4; cascade++)
            {
                PlayMatch(3);
                yield return new WaitForSeconds(0.4f);
            }
            Match3Vfx.ShowComboBanner(cascade);
            cascade = 0;
        }

        IEnumerator SwapPair(bool valid)
        {
            int x = Random.Range(0, columns - 1), y = Random.Range(0, rows);
            var a = pieces[x, y];
            var b = pieces[x + 1, y];
            if (a.busy || b.busy) yield break;
            a.busy = b.busy = true;
            Vector3 aHome = a.tr.position, bHome = b.tr.position;
            if (valid)
            {
                yield return Match3PieceMotion.Swap(a.tr, b.tr, bHome, aHome);
                yield return new WaitForSeconds(0.25f);
                yield return Match3PieceMotion.Swap(a.tr, b.tr, aHome, bHome);
            }
            else
            {
                yield return Match3PieceMotion.InvalidSwap(a.tr, b.tr, aHome, bHome);
            }
            a.busy = b.busy = false;
        }

        Vector2Int RandomCell() => new Vector2Int(Random.Range(1, columns - 1), Random.Range(1, rows - 1));

        List<Vector2Int> CellsOfColor(int color)
        {
            var list = new List<Vector2Int>();
            for (int y = 0; y < rows; y++)
                for (int x = 0; x < columns; x++)
                    if (pieces[x, y].color == color && !pieces[x, y].busy) list.Add(new Vector2Int(x, y));
            return list;
        }

        void OnGUI()
        {
            float scale = Mathf.Max(1f, Screen.height / 900f);
            GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
            GUILayout.BeginArea(new Rect(10, 10, 220, Screen.height / scale - 20), GUI.skin.box);
            scroll = GUILayout.BeginScrollView(scroll);
            GUILayout.Label("Match-3 VFX Kit demo");
            if (GUILayout.Button("Match 3")) PlayMatch(3);
            if (GUILayout.Button("Match 4")) { cascade = 1; PlayMatch(4); }
            if (GUILayout.Button("Cascade + banner")) StartCoroutine(Cascade());
            if (GUILayout.Button("Swap")) StartCoroutine(SwapPair(true));
            if (GUILayout.Button("Invalid swap")) StartCoroutine(SwapPair(false));
            GUILayout.Space(6);
            if (GUILayout.Button("Rocket (horizontal)")) Match3Vfx.PlayRocket(RandomCell(), true);
            if (GUILayout.Button("Rocket (vertical)")) Match3Vfx.PlayRocket(RandomCell(), false);
            if (GUILayout.Button("TNT")) Match3Vfx.PlayExplosion(RandomCell(), 2);
            if (GUILayout.Button("Helicopter")) Match3Vfx.PlayHelicopter(RandomCell());
            if (GUILayout.Button("Colour orb")) Match3Vfx.PlayColorOrb(RandomCell(), CellsOfColor(Random.Range(0, ColorCount)));
            GUILayout.Space(6);
            if (GUILayout.Button("Rocket + rocket")) Match3Vfx.PlayCombo(Match3Power.RocketHorizontal, Match3Power.RocketVertical, RandomCell());
            if (GUILayout.Button("Rocket + TNT")) Match3Vfx.PlayCombo(Match3Power.RocketHorizontal, Match3Power.Tnt, RandomCell());
            if (GUILayout.Button("TNT + TNT")) Match3Vfx.PlayCombo(Match3Power.Tnt, Match3Power.Tnt, RandomCell());
            if (GUILayout.Button("Helicopter x3")) Match3Vfx.PlayCombo(Match3Power.Helicopter, Match3Power.Helicopter, RandomCell());
            if (GUILayout.Button("Orb + orb (whole board)"))
            {
                var all = new List<Vector2Int>();
                for (int y = 0; y < rows; y++) for (int x = 0; x < columns; x++) all.Add(new Vector2Int(x, y));
                Match3Vfx.PlayCombo(Match3Power.ColorOrb, Match3Power.ColorOrb, RandomCell(), all);
            }
            GUILayout.Space(6);
            host.isPaused = GUILayout.Toggle(host.isPaused, "Paused");
            host.reducedMotion = GUILayout.Toggle(host.reducedMotion, "Reduced motion");
            host.soundEnabled = GUILayout.Toggle(host.soundEnabled, "Sound");
            GUILayout.Label(Match3Vfx.Busy ? "Sequence running" : "Idle");
            if (GUILayout.Button("Clear all")) { Match3Vfx.Clear(); StopAllCoroutines(); ResetBoard(); }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        void ResetBoard()
        {
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    var p = pieces[x, y];
                    p.busy = false;
                    p.sr.enabled = true;
                    Recolor(p);
                    p.tr.position = CellToWorld(new Vector2Int(x, y));
                }
            }
        }
    }
}
