// // ©2015 - 2026 Candy Smith
// // All rights reserved
// // Redistribution of this software is strictly not allowed.
// // Copy of this software can be obtained from unity asset store only.
// // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// // IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// // FITNESS FOR A PARTICULAR PURPOSE AND NON-INFRINGEMENT. IN NO EVENT SHALL THE
// // AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// // LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// // OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// // THE SOFTWARE.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JellyGarden.Scripts.GUI;
using JellyGarden.Scripts.Juice;
using UnityEngine;
using Random = UnityEngine.Random;

namespace JellyGarden.Scripts
{
    //types of items
    public enum ItemsTypes
    {
        NONE = 0,
        VERTICAL_STRIPPED,
        HORIZONTAL_STRIPPED,
        PACKAGE,
        BOMB,
        INGREDIENT,
        HELICOPTER
    }

    public class Item : MonoBehaviour
    {
        [NonSerialized] public bool royalConsumed;
        [NonSerialized] public bool royalSpecialGather;
        [NonSerialized] public Vector3 royalSpecialGatherTarget;
        // Set when ChangeTypeCor seats a match-made power-up in its cell; Start must not treat it as a refill.
        bool royalSeatedPowerup;
        [NonSerialized] public bool inputSwapLocked, inputMatchPending;
        [NonSerialized] public int inputConversionLocks, inputMoveOrder;
        bool deferredInputHit;
        public bool CanAcceptInput => LevelManager.THIS && LevelManager.THIS.BoardInputAllowed &&
            isActiveAndEnabled && !destroying && !falling && !royalConsumed && !inputSwapLocked &&
            !inputMatchPending && !deferredInputHit && inputConversionLocks == 0 && square && square.item == this &&
            square.CanGoOut() && ((Vector2)(transform.position - square.transform.position)).sqrMagnitude <
            Mathf.Pow(LevelManager.THIS.squareWidth * LevelManager.THIS.GameField.lossyScale.x * .1f, 2);
        //items sprites array
        public Sprite[] items;
        public GameObject[] prefabs;
        //stripe extra items array
        public List<StripedItem> stripedItems = new List<StripedItem>();
        //arrays for extra items
        public Sprite[] packageItems;
        public Sprite[] bombItems;
        public Sprite[] ingredientItems;

        //sprite rendered reference
        public SpriteRenderer sprRenderer;
        //square object reference
        public Square square;
        //is that item dragging
        public bool dragThis;

        public Vector3 mousePos;
        public Vector3 deltaPos;
        public Vector3 switchDirection;
        private Square neighborSquare;
        private Item switchItem;
        public bool falling;

        public int COLORView;
        private int COLOR;

        public int color
        {
            get
            {
                return COLOR;
            }
            set
            {
                // The level's own colour count is kept (D3): no 5-colour remap.
                COLOR = value;
            }
        }

        public ItemsTypes NextType
        {
            get
            {
                return nextType;
            }

            set
            {
                //      print(value);
                nextType = value;
            }
        }

        public Animator anim;
        public bool destroying;
        public bool appeared;
        public bool animationFinished;
        public bool justCreatedItem;
        private float xScale;
        private float yScale;
        public bool boost;
        public Vector2 moveDirection;
        private BoxCollider2D _boxCollider2D;

        private ItemsTypes nextType = ItemsTypes.NONE;
        public ItemsTypes currentType = ItemsTypes.NONE;
        private ItemsTypes _debugType = ItemsTypes.NONE;

        // --- presentation-only state used by the juice layer -----------------
        // Handle to the idle "breathing" coroutine. The original code called
        // StopCoroutine(AnimIdleStart()) which is a no-op (it builds a fresh
        // enumerator rather than referencing the running one), so the idle loop kept
        // writing transform.localScale forever and would fight any scale animation.
        private Coroutine idleAnimCor;
        // While true the idle loop keeps updating its internal values but stops
        // writing to the transform, letting squash/stretch own the scale.
        [HideInInspector] public bool suppressIdleScale;
        // Scale the juice animations return to. Captured once in Start().
        private Vector3 juiceBaseScale = Vector3.one;
        // Per-item delay so neighbouring fruits in one match do not pop on the same frame.
        [HideInInspector] public float juiceStagger;
        // Emphasis multiplier from match size / cascade depth.
        [HideInInspector] public float juiceIntensity = 1f;
        [HideInInspector] public ItemsTypes debugType
        {
            get { return _debugType; }
            set
            {
                _debugType = value;
                // Automatically handle type changes when debugType is set
                if (currentType != _debugType && currentType != ItemsTypes.INGREDIENT && NextType == ItemsTypes.NONE)
                {
                    NextType = _debugType;
                    ChangeType();
                }
            }
        }
        private Juice.RoyalPieceSkin royalSkin;
        private void Awake()
        {
            _boxCollider2D = GetComponent<BoxCollider2D>();
            royalSkin = GetComponent<Juice.RoyalPieceSkin>();
            // The kit prefab is left untouched; the skin (no serialized fields) is added at runtime.
            if (!royalSkin && Juice.RoyalEffectsProfile.Get != null) royalSkin = gameObject.AddComponent<Juice.RoyalPieceSkin>();
        }

        // Use this for initialization
        void Start()
        {
            gameObject.name = "item " + GetEntityId();
            // A Match-4 replacement starts its reveal in the spawn frame. Do not
            // randomize it, make it fall, or cache its temporary pop scale next frame.
            // GenColor would also re-roll its colour, turn it into an ingredient on
            // ingredient levels, or attach the timed levels' +5 sec bonus. This covers
            // the imported reveal too, which RoyalRocketRevealing and TNT3DAnimator never see.
            if (royalSeatedPowerup) { royalSkin?.ApplyNow(); return; }
            falling = true;
            GenColor();
            // sprRenderer = GetComponentInChildren<SpriteRenderer>();
            if (NextType != ItemsTypes.NONE)
            {
                debugType = NextType;
                currentType = NextType;
                NextType = ItemsTypes.NONE;
                transform.position = square.transform.position;
                falling = false;
            }
            else if (LevelManager.THIS.limitType == LIMIT.TIME && Random.Range(0, 28) == 1)
            {
                GameObject fiveTimes = Instantiate(Resources.Load("Prefabs/5sec")) as GameObject;
                fiveTimes.transform.SetParent(transform);
                fiveTimes.name = "5sec";
                fiveTimes.transform.localScale = Vector3.one * 2;
                fiveTimes.transform.localPosition = Vector3.zero;
            }
            xScale = transform.localScale.x;
            yScale = transform.localScale.y;
            juiceBaseScale = transform.localScale;
            royalSkin?.ApplyNow();

            //StartCoroutine(GenRandomSprite());
        }

        public void GenColor(int exceptColor = -1, bool onlyNONEType = false)
        {
            int row = square.row;
            int col = square.col;

            List<int> remainColors = new List<int>();
            // The level's COLOR LIMIT (3-6) is used as is (D3); items.Length is only the index bound.
            int paletteSize = Mathf.Clamp(LevelManager.Instance.colorLimit, 1, items.Length);
            // The win's finale refills without making new lines of three, as in the user's Royal Match Level 13 clip (the board
            // fills once and rests before the card): a chain of cascades only kept the card waiting (6 s measured, 2026-10-06).
            // Normal play keeps its own colouring.
            bool finaleRefill = LevelManager.THIS.gameStatus == GameState.PreWinAnimations;
            int ColorAt(int c, int r)
            {
                var sq = LevelManager.THIS.GetSquare(c, r);
                var other = sq ? sq.item : null;
                return other && other != this && !other.destroying && other.currentType == ItemsTypes.NONE ? other.color : -1;
            }
            bool Line(int c, int dc, int dr)
            {
                int a = ColorAt(col + dc, row + dr), b = ColorAt(col + 2 * dc, row + 2 * dr), o = ColorAt(col - dc, row - dr);
                return (a == c && b == c) || (a == c && o == c);
            }
            for (int i = 0; i < paletteSize; i++)
            {
                bool canGen = true;
                if (col > 1)
                {
                    Square neighbor = LevelManager.Instance.GetSquare(row, col - 1);
                    if (neighbor != null)
                    {
                        if (neighbor.item != null)
                        {
                            if (neighbor.CanGoInto() && neighbor.item.color == i)
                                canGen = false;
                        }
                    }
                }
                if (col < LevelManager.Instance.maxCols - 1)
                {
                    Square neighbor = LevelManager.Instance.GetSquare(row, col + 1);
                    if (neighbor != null)
                    {
                        if (neighbor.item != null)
                        {
                            if (neighbor.CanGoOut() && neighbor.item.color == i)
                                canGen = false;
                        }
                    }
                }
                if (row < LevelManager.Instance.maxRows)
                {
                    Square neighbor = LevelManager.Instance.GetSquare(row + 1, col);
                    if (neighbor != null)
                    {
                        if (neighbor.item != null)
                        {
                            if (neighbor.CanGoOut() && neighbor.item.color == i)
                                canGen = false;
                        }
                    }
                }
                if (canGen && finaleRefill && (Line(i, 1, 0) || Line(i, -1, 0) || Line(i, 0, 1) || Line(i, 0, -1)))
                    canGen = false;
                if (canGen && i != exceptColor)
                {
                    remainColors.Add(i);
                }
            }

            //       print(remainColors.Count);
            int randColor = Random.Range(0, paletteSize);
            if (remainColors.Count > 0)
                randColor = remainColors[Random.Range(0, remainColors.Count)];
            if (exceptColor == randColor)
                randColor = (randColor + 1) % paletteSize;
            LevelManager.THIS.lastRandColor = randColor;
            sprRenderer.sprite = items[randColor];
            if (NextType == ItemsTypes.HORIZONTAL_STRIPPED)
                sprRenderer.sprite = stripedItems[color].horizontal;
            else if (NextType == ItemsTypes.VERTICAL_STRIPPED)
                sprRenderer.sprite = stripedItems[color].vertical;
            else if (NextType == ItemsTypes.PACKAGE)
                sprRenderer.sprite = packageItems[color];
            else if (NextType == ItemsTypes.BOMB)
                sprRenderer.sprite = bombItems[0];
            else
            {
                var ingredients = LevelManager.THIS.targetObject.Where(i=>i.type == Target.INGREDIENT).ToArray();
                if (ingredients.Any())
                {
                    foreach (var targetObject in ingredients)
                    {
                        if( Random.Range(0, LevelManager.THIS.Limit) == 0 && square.row + 1 < LevelManager.THIS.maxRows && !onlyNONEType
                            && !targetObject.Done() && LevelManager.THIS.GetIngredientsCount(targetObject.icon.name) < targetObject.GetCount())
                        {
                            StartCoroutine(FallingCor(square, true));
                            color = 1000;
                            currentType = ItemsTypes.INGREDIENT;
                            sprRenderer.sprite = targetObject.icon;
                            name = "ingredient_" + color;
                        }
                        else
                        {
                            StartCoroutine(FallingCor(square, true));
                            // The chosen logical colour is independent of imported
                            // artwork, sprite replacement and renderer callbacks.
                            color = randColor;
                        }
                    }
                }
                else
                {
                    StartCoroutine(FallingCor(square, true));
                    color = randColor;
                }
            }

            if (prefabs.Length > 0)
            {
                sprRenderer.enabled = false;
                for (int i = 0; i < transform.childCount; i++)
                {
                    if (transform.GetChild(i).name != "Sprite")
                    { Destroy(transform.GetChild(i)); }
                }
                GameObject obj = Instantiate(prefabs[color]);
                obj.transform.SetParent(transform);
                obj.transform.localPosition = Vector3.zero;
            }
            royalSkin?.ApplyNow();
        }

        public void SetColor(int col)
        {
            color = col;
            if (color < items.Length)
                sprRenderer.sprite = items[color];
            royalSkin?.ApplyNow();
        }

        public void SetAppeared()
        {
            appeared = true;
            StartIdleAnim();
            if (currentType == ItemsTypes.PACKAGE)
                anim.SetBool("package_idle", true);

        }

        public void StartIdleAnim()
        {
            StopIdleAnim();
            idleAnimCor = StartCoroutine(AnimIdleStart());
        }

        /// <summary>
        /// Properly stops the idle breathing loop. Needed because the juice
        /// animations drive transform.localScale and must not be overwritten.
        /// </summary>
        public void StopIdleAnim()
        {
            if (idleAnimCor != null)
            {
                StopCoroutine(idleAnimCor);
                idleAnimCor = null;
            }
        }

        IEnumerator AnimIdleStart()
        {
            float xScaleDest1 = xScale - 0.05f;
            float xScaleDest2 = xScale;
            float speed = Random.Range(0.02f, 0.07f);

            bool trigger = false;
            while (true)
            {
                if (!trigger)
                {
                    if (xScale > xScaleDest1)
                    {
                        xScale -= Time.deltaTime * speed;
                        yScale += Time.deltaTime * speed;
                    }
                    else
                        trigger = true;
                }
                else
                {
                    if (xScale < xScaleDest2)
                    {
                        xScale += Time.deltaTime * speed;
                        yScale -= Time.deltaTime * speed;
                    }
                    else
                        trigger = false;
                }
                if (!suppressIdleScale)
                    transform.localScale = new Vector3(xScale, yScale, 1);
                yield return new WaitForFixedUpdate();
            }
            //anim.SetBool("stop", true);
            //if (square.col % 2 == 0 || square.row % 2 == 0)
            //    yield return new WaitForSeconds(1);
            //anim.SetBool("stop", false);
        }

        IEnumerator GenRandomSprite()
        {
            Sprite spr = null;
            while (true)
            {
                spr = items[Random.Range(0, items.Length)];
                yield return new WaitForFixedUpdate();
                break;
            }

            sprRenderer.sprite = spr;
        }

        void ResetDrag()
        {
            dragThis = false;
            switchDirection = Vector3.zero;
        }

        public void CheckItemDrag()
        {
            if (dragThis)
            {
                deltaPos = mousePos - GetMousePosition();
                if (switchDirection == Vector3.zero)
                {
                    SwitchDirection(deltaPos);
                }
            }
        }

        public void SwitchDirection(Vector3 delta)
        {
            if (!CanAcceptInput) { ResetDrag(); return; }
            neighborSquare = null;
            switchItem = null;
            switchDirection = Vector3.zero;
            deltaPos = delta;
            if (Vector3.Magnitude(deltaPos) > 0.1f)
            {
                if (Mathf.Abs(deltaPos.x) > Mathf.Abs(deltaPos.y) && deltaPos.x > 0)
                    switchDirection.x = 1;
                else if (Mathf.Abs(deltaPos.x) > Mathf.Abs(deltaPos.y) && deltaPos.x < 0)
                    switchDirection.x = -1;
                else if (Mathf.Abs(deltaPos.x) < Mathf.Abs(deltaPos.y) && deltaPos.y > 0)
                    switchDirection.y = 1;
                else if (Mathf.Abs(deltaPos.x) < Mathf.Abs(deltaPos.y) && deltaPos.y < 0)
                    switchDirection.y = -1;
                if (switchDirection.x > 0)
                {
                    neighborSquare = square.GetNeighborLeft();
                }
                else if (switchDirection.x < 0)
                {
                    neighborSquare = square.GetNeighborRight();
                }
                else if (switchDirection.y > 0)
                {
                    neighborSquare = square.GetNeighborBottom();
                }
                else if (switchDirection.y < 0)
                {
                    neighborSquare = square.GetNeighborTop();
                }
                if (neighborSquare != null)
                    switchItem = neighborSquare.item;
                if (switchItem != null && switchItem.CanAcceptInput)
                {
                    if (switchItem.square.type != SquareTypes.WIREBLOCK)
                        StartCoroutine(Switching());
                    else if ((currentType != ItemsTypes.NONE || switchItem.currentType != ItemsTypes.NONE) && switchItem.square.type != SquareTypes.WIREBLOCK)   //1.4.1   1.6.1
                        StartCoroutine(Switching());
                    else
                        ResetDrag();//1.6.1

                }
                else
                    ResetDrag();
            }

        }

        IEnumerator Switching()
        {
            var lm = LevelManager.THIS;
            var other = switchItem;
            if (!CanAcceptInput || !other || !other.CanAcceptInput) { ResetDrag(); yield break; }
            inputSwapLocked = other.inputSwapLocked = true;
            lm.BeginInputSwap();
            // Only these two pieces are leased. Hits wait for their atomic swap commit;
            // the resolver waits before scanning/gravity, while other input stays live.
            try { yield return SwitchingCore(); }
            finally
            {
                inputSwapLocked = false;
                if (other) other.inputSwapLocked = false;
                if (lm) lm.EndInputSwap();
                ResetDrag();
            }
        }

        IEnumerator SwitchingCore()
        {
            if (switchDirection != Vector3.zero && neighborSquare != null)
            {
                bool backMove = false, charged = false;
                var origin = square;
                LevelManager.THIS.DragBlocked = true;
                neighborSquare.item = this;
                square.item = switchItem;
                var destinationMatches = neighborSquare.FindMatchesAround();
                var sourceMatches = square.FindMatchesAround();
                int matchesHere = destinationMatches.Count;
                int matchesInThisItem = matchesHere;
                int matchesInNeithborItem = sourceMatches.Count;
                // Validation must not expose provisional ownership during the animation.
                neighborSquare.item = switchItem;
                square.item = this;
                if (destinationMatches.Concat(sourceMatches).Any(i => i && i != this && i != switchItem &&
                    (i.inputSwapLocked || i.inputMatchPending || i.inputConversionLocks > 0 || i.falling))) yield break;
                bool thisItemHaveMatch = false;
                if (matchesInThisItem >= 4)
                    thisItemHaveMatch = true;
                if (matchesInNeithborItem >= 4)
                    thisItemHaveMatch = false;
                int matchesHereOneColor = matchesHere;
                matchesHere += matchesInNeithborItem;
                if (RoyalEffectsProfile.Get && (RoyalPowerupDirector.IsPower(currentType) || RoyalPowerupDirector.IsPower(switchItem.currentType)) && currentType != ItemsTypes.INGREDIENT && switchItem.currentType != ItemsTypes.INGREDIENT) matchesHere++;

                if ((currentType == ItemsTypes.BOMB || switchItem.currentType == ItemsTypes.BOMB) && (currentType != ItemsTypes.INGREDIENT && switchItem.currentType != ItemsTypes.INGREDIENT))
                    matchesHere++;
                if (currentType > 0 && switchItem.currentType > 0)
                    matchesHere++;
                if (currentType == ItemsTypes.INGREDIENT && switchItem.currentType == ItemsTypes.INGREDIENT)
                    matchesHere = 0;
                Vector3 startPos = transform.position;

                // The reject test depends only on values already computed above, so it
                // can be evaluated BEFORE the move animation. Knowing up front that a
                // swap will fail lets the invalid case play as a short push-and-return
                // instead of completing the swap and then undoing it (which read as a
                // slow 400ms round trip). Gameplay outcome is unchanged - the same
                // expression still drives the same branch below.
                bool rejectSwap = matchesHere <= 0 && matchesInNeithborItem <= 0 && LevelManager.THIS.ActivatedBoost.type != BoostType.Hand ||
                    ((currentType == ItemsTypes.BOMB || switchItem.currentType == ItemsTypes.BOMB) && (currentType == ItemsTypes.INGREDIENT || switchItem.currentType == ItemsTypes.INGREDIENT) && (matchesHere + matchesInNeithborItem <= 2)) ||
                    (((int)currentType >= 1 || (int)switchItem.currentType >= 1) && (currentType == ItemsTypes.INGREDIENT || switchItem.currentType == ItemsTypes.INGREDIENT) && (matchesHere + matchesInNeithborItem <= 2));

                if (!rejectSwap)
                {
                    // Charge once before yielding so rapid actions cannot overspend the last move.
                    if (LevelManager.THIS.ActivatedBoost.type != BoostType.Hand)
                    {
                        if (LevelManager.THIS.limitType == LIMIT.MOVES) LevelManager.THIS.Limit--;
                        LevelManager.THIS.moveID++;
                        charged = true;
                    }
                    else LevelManager.THIS.ActivatedBoost = null;
                    inputMoveOrder = LevelManager.THIS.moveID * 2 + 2;
                    switchItem.inputMoveOrder = inputMoveOrder - 1;
                    foreach (var piece in destinationMatches.Concat(sourceMatches)) if (piece) piece.inputMatchPending = true;
                }
                dragThis = false;

                var juiceCfg = JuiceConfig.Instance;
                Vector3 myTarget = neighborSquare.transform.position + Vector3.back * 0.3f;
                Vector3 otherStart = neighborSquare.transform.position + Vector3.back * 0.2f;

                if (juiceCfg.EnableJuice && rejectSwap)
                {
                    // Push only part of the way toward the neighbour, then stop dead.
                    float pushDur = juiceCfg.InvalidSwapDuration * 0.42f;
                    float f = juiceCfg.InvalidSwapPushFraction;
                    float t = 0f;
                    while (t < 1f)
                    {
                        t += Time.deltaTime / Mathf.Max(0.0001f, pushDur);
                        float e = Easing.OutQuad(Mathf.Clamp01(t)) * f;
                        transform.position = Vector3.LerpUnclamped(startPos, myTarget, e);
                        if (switchItem != null)
                            switchItem.transform.position = Vector3.LerpUnclamped(otherStart, startPos, e);
                        yield return null;
                    }
                }
                else
                {
                    // Valid swap: both items travel together, eased, and fast.
                    float dur = juiceCfg.EnableJuice ? juiceCfg.SwapDuration : 0.2f;
                    // Two TNTs merge rather than trade places: the dragged one slides onto its partner, drawn above it,
                    // and the combined TNT then grows out of the cell they met in.
                    bool merge = RoyalEffectsProfile.Get && currentType == ItemsTypes.PACKAGE && switchItem != null && switchItem.currentType == ItemsTypes.PACKAGE;
                    int restOrder = sprRenderer.sortingOrder;
                    if (merge) sprRenderer.sortingOrder = restOrder + 1;
                    float t = 0f;
                    while (t < 1f)
                    {
                        t += Time.deltaTime / Mathf.Max(0.0001f, dur);
                        float e = juiceCfg.EnableJuice ? Easing.OutCubic(Mathf.Clamp01(t)) : Mathf.Clamp01(t);
                        transform.position = Vector3.LerpUnclamped(startPos, myTarget, e);
                        if (switchItem != null && !merge)
                            switchItem.transform.position = Vector3.LerpUnclamped(otherStart, startPos, e);
                        yield return null;
                    }
                    transform.position = myTarget;
                    if (switchItem != null && !merge)
                        switchItem.transform.position = startPos;
                    sprRenderer.sortingOrder = restOrder;
                }

                // Both cells must still hold these two pieces (2026-10-06, the user's overlapping gems). If anything moved either
                // piece on paper during the animation, writing the swap now would cross the cells' records and leave a piece
                // drawn in a cell another piece owns: the swap is dropped, its move refunded, and each piece goes back to its cell.
                if (!switchItem || square != origin || origin.item != this || neighborSquare.item != switchItem || switchItem.square != neighborSquare)
                {
                    if (charged && LevelManager.THIS.limitType == LIMIT.MOVES) LevelManager.THIS.Limit++;
                    foreach (var piece in destinationMatches.Concat(sourceMatches)) if (piece) piece.inputMatchPending = false;
                    Debug.LogWarning("[Royal board] A swap's cells changed during its animation; it was dropped and both pieces went home.");
                    falling = false; StartFalling();
                    if (switchItem && !switchItem.destroying) { switchItem.falling = false; switchItem.StartFalling(); }
                    ResetDrag();
                    yield break;
                }

                if (rejectSwap)
                {//1.6.1

                    neighborSquare.item = switchItem;
                    square.item = this;
                    backMove = true;
                    SoundBase.Instance.PlaySound(SoundBase.Instance.wrongMatch);


                }
                else
                {
                    neighborSquare.item = this;
                    square.item = switchItem;
                    switchItem.square = square;
                    square = neighborSquare;
                    inputSwapLocked = switchItem.inputSwapLocked = false;
                    LevelManager.THIS.lastDraggedItem = this;
                    LevelManager.THIS.lastSwitchedItem = switchItem;
                    if (RoyalPowerupDirector.TrySwap(this, switchItem))
                    { LevelManager.THIS.FindMatches(); ResetDrag(); yield break; }
                    // RoyalPatterns/CombineManager own formation rules, including 2×2 squares.
                    // The legacy count-only prepass would mark a square as a striped piece.
                    if (RoyalEffectsProfile.Get)
                    { LevelManager.THIS.FindMatches(); ResetDrag(); yield break; }

                    if (matchesHereOneColor == 4 || matchesInNeithborItem == 4)
                    {
                        if (thisItemHaveMatch)
                            SetStrippedExtra(startPos - neighborSquare.transform.position);
                        else
                        {
                            LevelManager.THIS.lastDraggedItem = switchItem;
                            LevelManager.THIS.lastSwitchedItem = this;
                            switchItem.SetStrippedExtra(startPos - square.transform.position);
                            if (matchesInThisItem == 4)
                                SetStrippedExtra(startPos - neighborSquare.transform.position);
                        }
                    }

                    if (matchesHere >= 5)
                    {
                        if (thisItemHaveMatch && matchesHereOneColor >= 5)
                            NextType = ItemsTypes.BOMB;
                        else if (!thisItemHaveMatch && matchesInNeithborItem >= 5)
                        {
                            LevelManager.THIS.lastDraggedItem = switchItem;
                            LevelManager.THIS.lastSwitchedItem = this;
                            switchItem.NextType = ItemsTypes.BOMB;
                            if (matchesInThisItem >= 5)
                                NextType = ItemsTypes.BOMB;
                        }

                    }
                    if (currentType != ItemsTypes.INGREDIENT && switchItem.currentType != ItemsTypes.INGREDIENT)
                    {
                        CheckChocoBomb(this, switchItem);
                        if (currentType != ItemsTypes.BOMB || switchItem.currentType != ItemsTypes.BOMB)
                            CheckChocoBomb(switchItem, this);
                    }

                    if ((currentType == ItemsTypes.HORIZONTAL_STRIPPED || currentType == ItemsTypes.VERTICAL_STRIPPED) && (switchItem.currentType == ItemsTypes.HORIZONTAL_STRIPPED || switchItem.currentType == ItemsTypes.VERTICAL_STRIPPED))
                    {
                        //1.6
                        switchItem.currentType = ItemsTypes.NONE; //2.0
                        switchItem.nextType = ItemsTypes.NONE;  //2.0
                        switchItem.debugType = ItemsTypes.NONE; //2.0
                        DestroyHorizontal();
                        DestroyVertical();
                        //					switchItem.DestroyItem ();
                    }

                    CheckPackageAndStripped(this, switchItem);
                    CheckPackageAndStripped(switchItem, this);


                    CheckPackageAndPackage(this, switchItem);
                    CheckPackageAndPackage(switchItem, this);

                    if (currentType != ItemsTypes.BOMB || switchItem.currentType != ItemsTypes.BOMB)
                        LevelManager.THIS.FindMatches();
                }
                //Debug.Break();
                //yield return new WaitForSeconds(2);


                if (backMove)
                {
                    // Return from wherever the push stopped. OutBack gives the small
                    // "nope" settle without shaking the board or adding particles.
                    Vector3 myFrom = transform.position;
                    Vector3 otherFrom = switchItem != null ? switchItem.transform.position : otherStart;
                    float backDur = juiceCfg.EnableJuice
                        ? juiceCfg.InvalidSwapDuration * 0.58f
                        : 0.2f;
                    float t = 0f;
                    while (t < 1f)
                    {
                        t += Time.deltaTime / Mathf.Max(0.0001f, backDur);
                        // Overshoot 1.1 moved the item ~3px past home on a 0.42-cell push -
                        // invisible. 3.0 gives a ~9px settle, which reads as "nope" without
                        // becoming a bounce.
                        float e = juiceCfg.EnableJuice
                            ? Easing.OutBack(Mathf.Clamp01(t), 3.0f)
                            : Mathf.Clamp01(t);
                        transform.position = Vector3.LerpUnclamped(myFrom, startPos, e);
                        if (switchItem != null)
                            switchItem.transform.position = Vector3.LerpUnclamped(otherFrom, otherStart, e);
                        yield return null;
                    }
                    transform.position = startPos;
                    if (switchItem != null)
                        switchItem.transform.position = otherStart;
                }

                if (backMove)
                {
                    if (!LevelManager.THIS.IsResolvingMatches && LevelManager.THIS.InputSwaps == 1)
                        StartCoroutine(AI.THIS.CheckPossibleCombines());
                }
            }
            ResetDrag();
        }

        void CheckPackageAndPackage(Item item1, Item item2)
        {
            if (item1.currentType == ItemsTypes.PACKAGE && item2.currentType == ItemsTypes.PACKAGE)
            {
                List<Item> bigList = new List<Item>();
                List<Item> itemsList = LevelManager.THIS.GetItemsAround(item2.square);
                foreach (Item item in itemsList)
                {
                    if (item != null)
                    {
                        bigList.AddRange(LevelManager.THIS.GetItemsAround(item.square));
                    }
                }
                foreach (Item item in bigList)
                {
                    if (item != null)
                    {
                        if (item.currentType != ItemsTypes.BOMB && item.currentType != ItemsTypes.INGREDIENT)
                            item.DestroyItem(true, "destroy_package");
                    }
                }

                item1.DestroyPackage();
                item2.DestroyPackage();
            }
        }

        void CheckPackageAndStripped(Item item1, Item item2)
        {
            if (((item1.currentType == ItemsTypes.HORIZONTAL_STRIPPED || item1.currentType == ItemsTypes.VERTICAL_STRIPPED) && item2.currentType == ItemsTypes.PACKAGE))
            {
                int i = 0;
                List<Item> itemsList = LevelManager.THIS.GetItemsAround(item2.square);
                int direction = 1;//2.0
                foreach (Item item in itemsList)
                {
                    if (item != null)
                    {
                        if (item.currentType != ItemsTypes.BOMB && item.currentType != ItemsTypes.INGREDIENT)
                        {//1.6.1
                            if (direction > 0)//2.0
                                item.currentType = ItemsTypes.HORIZONTAL_STRIPPED;
                            else
                                item.currentType = ItemsTypes.VERTICAL_STRIPPED;
                            direction *= -1;
                            //						item.currentType = (ItemsTypes)((i) % 2) + 1;
                            i++;
                        }
                    }
                }
                item2.DestroyPackage();
            }
        }

        public void CheckChocoBomb(Item item1, Item item2)
        {
            if (item1.currentType == ItemsTypes.INGREDIENT || item2.currentType == ItemsTypes.INGREDIENT)
                return;
            if (item1.currentType == ItemsTypes.BOMB)
            {
                if (item2.currentType == ItemsTypes.NONE)
                    DestroyColor(item2.color);
                else if (item2.currentType == ItemsTypes.HORIZONTAL_STRIPPED || item2.currentType == ItemsTypes.VERTICAL_STRIPPED)
                    LevelManager.THIS.SetTypeByColor(item2.color, ItemsTypes.HORIZONTAL_STRIPPED);
                else if (item2.currentType == ItemsTypes.PACKAGE)
                    LevelManager.THIS.SetTypeByColor(item2.color, ItemsTypes.PACKAGE);
                else if (item2.currentType == ItemsTypes.BOMB)
                    LevelManager.THIS.DestroyDoubleBomb(square.col);



                item1.DestroyItem();
            }

        }

        void SetStrippedExtra(Vector3 dir)
        {
            moveDirection = dir;//1.6
            //		print ("set striped");
            if (Math.Abs(dir.x) > Math.Abs(dir.y))
                NextType = ItemsTypes.HORIZONTAL_STRIPPED;
            else
                NextType = ItemsTypes.VERTICAL_STRIPPED;
        }

        Vector3 GetDeltaPositionFromSquare(Square sq, Vector3 delta)
        {
            Vector3 newPos = (sq.transform.position - delta) + Vector3.back * 0.3f;
            newPos.x = Mathf.Clamp(newPos.x, sq.GetNeighborLeft(true).transform.position.x, sq.GetNeighborRight(true).transform.position.x);
            newPos.y = Mathf.Clamp(newPos.y, sq.GetNeighborBottom(true).transform.position.y, sq.GetNeighborTop(true).transform.position.y);
            return newPos;
        }


        public Vector3 GetMousePosition()
        {
            return Camera.main.ScreenToWorldPoint(InputManager.Instance.TouchPosition);
        }

        public void CheckNeedToFall(Square _square)
        {
            _square.item = this;
            // Only the cell that really holds this piece is freed (2026-10-06): clearing another piece's cell left it unowned.
            if (square && square.item == this) square.item = null;
            square = _square;   //need to count all falling items and drop them down in the same time
        }

        public void StartFalling()
        {
            if (!falling)
                StartCoroutine(FallingCor(square, true));
        }

        Coroutine landingCor;
        int fallPoseVersion;

        // The refill's entrance (2026-10-04, the user asked for a gentler fill): LevelManager.GenerateNewItems stacks new
        // pieces above the top edge of their column's run of cells, so a column fills as one stream entering from the board's
        // edge, and gives each column a small delay (a soft wave from the centre out). Each piece fades in as it crosses the
        // edge instead of popping into view. Presentation only: the piece already owns its target square.
        internal float royalEntryEdge = float.NaN, royalFallDelay;
        SpriteRenderer[] entryRenderers; float[] entryAlphas;
        void BeginEntryFade()
        {
            if (entryRenderers != null) return;   // a restarted fall keeps the alphas captured first
            var all = GetComponentsInChildren<SpriteRenderer>(true);
            var list = new global::System.Collections.Generic.List<SpriteRenderer>();
            foreach (var r in all) if (r && r.name != "Royal contact shadow") list.Add(r);   // the shadow follows sprRenderer
            entryRenderers = list.ToArray(); entryAlphas = new float[entryRenderers.Length];
            for (int i = 0; i < entryRenderers.Length; i++) entryAlphas[i] = entryRenderers[i].color.a;
        }
        void ApplyEntryFade()
        {
            if (entryRenderers == null || float.IsNaN(royalEntryEdge)) return;
            float cell = LevelManager.THIS ? LevelManager.THIS.squareHeight * Mathf.Abs(LevelManager.THIS.GameField.lossyScale.y) : 1f;
            // Hidden while the piece is wholly above the edge; whole once its centre is a fifth of a cell inside.
            float a = Mathf.Clamp01((royalEntryEdge + cell * .5f - transform.position.y) / (cell * .7f));
            a = a * a * (3f - 2f * a);
            for (int i = 0; i < entryRenderers.Length; i++)
                if (entryRenderers[i]) { var c = entryRenderers[i].color; c.a = entryAlphas[i] * a; entryRenderers[i].color = c; }
        }
        void EndEntryFade()
        {
            if (entryRenderers != null)
                for (int i = 0; i < entryRenderers.Length; i++)
                    if (entryRenderers[i]) { var c = entryRenderers[i].color; c.a = entryAlphas[i]; entryRenderers[i].color = c; }
            entryRenderers = null; entryAlphas = null; royalEntryEdge = float.NaN;
        }

        // Closed-form integration keeps the same speed at 30/60 FPS. Position is
        // sampled by the render frame; it never extrapolates beyond the destination.
        internal static float FallDistanceAt(float elapsed, float initialSpeed, float acceleration, float maxSpeed)
        {
            elapsed = Mathf.Max(0, elapsed);
            maxSpeed = Mathf.Max(.01f, maxSpeed);
            initialSpeed = Mathf.Clamp(initialSpeed, .01f, maxSpeed);
            acceleration = Mathf.Max(0, acceleration);
            if (acceleration <= .0001f) return initialSpeed * elapsed;
            float accelerating = Mathf.Min(elapsed, (maxSpeed - initialSpeed) / acceleration);
            return initialSpeed * accelerating + .5f * acceleration * accelerating * accelerating +
                maxSpeed * (elapsed - accelerating);
        }

        IEnumerator FallingCor(Square _square, bool animate)
        {
            if (!_square || destroying) yield break;
            int version = ++fallPoseVersion;
            if (landingCor != null)
            {
                StopCoroutine(landingCor); landingCor = null;
                transform.localScale = juiceBaseScale;
                suppressIdleScale = false;
            }
            falling = true;
            bool entering = !float.IsNaN(royalEntryEdge);
            if (entering) { BeginEntryFade(); ApplyEntryFade(); }
            if (royalFallDelay > 0f)
            {
                float wait = royalFallDelay; royalFallDelay = 0f;
                while (wait > 0f)
                {
                    if (version != fallPoseVersion || destroying) yield break;
                    if (!(LevelManager.THIS && LevelManager.THIS.gameStatus == GameState.Pause)) wait -= Time.deltaTime;
                    yield return null;
                }
            }
            Vector3 startPos = transform.position;
            Vector3 endPos = _square.transform.position + Vector3.back * 0.2f;
            // Z is render depth, not travel distance. Small/diagonal steps must
            // animate too; the old half-world-unit gate snapped short moves.
            float distance = Vector2.Distance(startPos, endPos);
            var cfg = JuiceConfig.Instance;
            float elapsed = 0;
            float speed = cfg.EnableJuice ? cfg.FallSpeed : 10f;
            float acceleration = cfg.EnableJuice ? cfg.FallAcceleration : 0f;
            float maxSpeed = cfg.EnableJuice ? cfg.FallMaxSpeed : speed;
            float travelled = 0;
            while (distance > .0001f && travelled < distance)
            {
                if (version != fallPoseVersion || destroying) yield break;
                if (square != _square) { falling = false; StartFalling(); yield break; }
                if (LevelManager.THIS && LevelManager.THIS.gameStatus == GameState.Pause) { yield return null; continue; }
                elapsed += Time.deltaTime;
                travelled = FallDistanceAt(elapsed, speed, acceleration, maxSpeed);
                transform.position = Vector3.Lerp(startPos, endPos, Mathf.Clamp01(travelled / distance));
                if (entering) ApplyEntryFade();
                yield return null;
            }
            if (version != fallPoseVersion || destroying) yield break;
            if (square != _square) { falling = false; StartFalling(); yield break; }
            transform.position = endPos;
            if (entering) EndEntryFade();
            falling = false;
            justCreatedItem = false;
            if (distance > .0001f && animate)
            {
                if (cfg.EnableJuice || RoyalPowerupDirector.IsPower(currentType))
                {
                    // The bounce follows how hard the piece lands: its speed against the fall's top speed, and how far it fell.
                    float cell = LevelManager.THIS ? LevelManager.THIS.squareHeight * Mathf.Abs(LevelManager.THIS.GameField.lossyScale.y) : 1f;
                    float impact = Mathf.Min(maxSpeed, speed + acceleration * elapsed);
                    float strength = Mathf.Clamp01(impact / Mathf.Max(.01f, maxSpeed)) * Mathf.Clamp01(distance / Mathf.Max(.01f, cell));
                    landingCor = StartCoroutine(LandingSquashCor(strength));
                }
                else if (anim)
                {
                    anim.SetTrigger("stop");
                }
                var sound = SoundBase.Instance;
                if (sound && sound.drop != null && sound.drop.Length > 0)
                    sound.PlaySound(sound.drop[Random.Range(0, sound.drop.Length)]);
            }
        }

        /// <summary>
        /// Landing bounce (2026-10-04, the user asked for a realistic one): the piece meets its cell and squashes, hops up,
        /// lands again with a hop at 30% of the first, then rests. <paramref name="strength"/> (0-1) is how hard it landed,
        /// so a long drop bounces more than a one-cell step. Gems squash and stretch; boosters only move.
        /// Runs alongside gameplay - it never blocks the falling/cascade pipeline, and a new fall, swap or destroy takes over.
        /// </summary>
        IEnumerator LandingSquashCor(float strength)
        {
            if (destroying) yield break;

            var cfg = JuiceConfig.Instance;
            float dur = cfg.LandingSquashDuration;
            float amount = cfg.LandingSquash;
            strength = Mathf.Clamp01(strength);
            if (dur <= 0f || strength <= 0f || amount <= 0f && cfg.LandingBounce <= 0f) yield break;

            int version = fallPoseVersion;
            var landedSquare = square;
            var landedType = currentType;
            bool rigid = RoyalPowerupDirector.IsPower(landedType);
            Vector3 restPosition = transform.position;
            Vector3 appliedPosition = restPosition;
            float cell = LevelManager.THIS ? LevelManager.THIS.squareHeight * Mathf.Abs(LevelManager.THIS.GameField.lossyScale.y) : 1f;
            float hop1 = cell * cfg.LandingBounce * strength, hop2 = hop1 * .3f;
            if (rigid) StopIdleAnim();
            suppressIdleScale = true;
            float t = 0f;
            while (t < 1f && !destroying && !falling && !inputSwapLocked &&
                version == fallPoseVersion && square == landedSquare && currentType == landedType)
            {
                if (LevelManager.THIS && LevelManager.THIS.gameStatus == GameState.Pause) { yield return null; continue; }
                t += Time.deltaTime / dur;
                float k = Mathf.Clamp01(t);
                // Timeline (fractions of the landing time): contact 0-.16, first hop .16-.68, second hop .68-1. Hops are
                // parabolas, as under gravity. squash > 0 flattens a gem, < 0 stretches it; every term starts and ends at 0.
                float lift = 0f, squash;
                if (k < .16f) squash = Mathf.Sin(k / .16f * Mathf.PI) * strength;
                else if (k < .68f)
                {
                    float u = (k - .16f) / .52f;
                    lift = hop1 * 4f * u * (1f - u);
                    squash = -.3f * strength * Mathf.Sin(Mathf.Min(u / .5f, 1f) * Mathf.PI);     // stretched while it rises
                }
                else
                {
                    float u = (k - .68f) / .32f;
                    lift = hop2 * 4f * u * (1f - u);
                    squash = .35f * strength * Mathf.Sin(Mathf.Min(u / .3f, 1f) * Mathf.PI);    // the second, softer touch
                }
                // Boosters keep their shape (metal shells); both native child rigs and the Color Ball seat follow this root.
                appliedPosition = restPosition + Vector3.up * lift;
                transform.position = appliedPosition;
                if (!rigid)
                {
                    float sy = 1f - squash * amount;
                    float sx = 1f + squash * amount * 0.75f;
                    transform.localScale = new Vector3(juiceBaseScale.x * sx, juiceBaseScale.y * sy, juiceBaseScale.z);
                }
                yield return null;
            }

            if (!destroying && version == fallPoseVersion)
            {
                // Do not restore position over a new swap/fall that took ownership.
                if (!falling && !inputSwapLocked && square == landedSquare &&
                    (transform.position - appliedPosition).sqrMagnitude < .000001f)
                    transform.position = restPosition;
                transform.localScale = juiceBaseScale;
                xScale = juiceBaseScale.x;
                yScale = juiceBaseScale.y;
                suppressIdleScale = false;
                landingCor = null;
            }
        }

        public bool GetNearEmptySquares()
        {
            bool nearEmptySquareDetected = false;
            if (square.row < LevelManager.Instance.maxRows - 1 && square.col < LevelManager.Instance.maxCols)
            {
                Square checkingSquare = LevelManager.Instance.GetSquare(square.col + 1, square.row + 1, true);
                if (checkingSquare.CanGoInto() && checkingSquare.item == null && !checkingSquare.Reserved && !falling && !destroying && !inputSwapLocked)
                {//2.0
                    checkingSquare = LevelManager.Instance.GetSquare(square.col + 1, square.row + 1, true);
                    if (checkingSquare.CanGoInto())
                    {
                        if (checkingSquare.item == null)
                        {
                            square.item = null;
                            checkingSquare.item = this;
                            square = checkingSquare;
                            StartFalling();//2.0
                            nearEmptySquareDetected = true;
                        }
                    }
                }
            }
            if (square.row < LevelManager.Instance.maxRows - 1 && square.col > 0)
            {
                Square checkingSquare = LevelManager.Instance.GetSquare(square.col - 1, square.row + 1, true);
                if (checkingSquare.CanGoInto() && checkingSquare.item == null && !checkingSquare.Reserved && !falling && !destroying && !inputSwapLocked)
                {//2.0
                    checkingSquare = LevelManager.Instance.GetSquare(square.col - 1, square.row + 1, true);
                    if (checkingSquare.CanGoInto())
                    {
                        if (checkingSquare.item == null)
                        {
                            square.item = null;
                            checkingSquare.item = this;
                            square = checkingSquare;
                            StartFalling();//2.0
                            nearEmptySquareDetected = true;
                        }
                    }
                }
            }

            return nearEmptySquareDetected;
        }

        public bool GetNearEmptySquaresLeft()
        {
            bool nearEmptySquareDetected = false;
            if (square.row < LevelManager.Instance.maxRows - 1 && square.col > 0)
            {
                Square checkingSquare = LevelManager.Instance.GetSquare(square.col - 1, square.row + 1, true);
                if (checkingSquare.CanGoInto() && checkingSquare.item == null)
                {
                    checkingSquare = LevelManager.Instance.GetSquare(square.col - 1, square.row + 1, true);
                    if (checkingSquare.CanGoInto())
                    {
                        if (checkingSquare.item == null)
                        {
                            square.item = null;
                            checkingSquare.item = this;
                            square = checkingSquare;
                            StartCoroutine(FallingCor(square, true));
                            nearEmptySquareDetected = true;
                        }
                    }
                }
            }
            return nearEmptySquareDetected;
        }


        public Item GetLeftItem()
        {
            Square sq = square.GetNeighborLeft();
            if (sq != null)
            {
                if (sq.item != null)
                    return sq.item;
            }
            return null;
        }

        public Item GetTopItem()
        {
            Square sq = square.GetNeighborTop();
            if (sq != null)
            {
                if (sq.item != null)
                    return sq.item;
            }
            return null;
        }


        public void ChangeType()
        {
            if (this != null)
                StartCoroutine(ChangeTypeCor());
        }

        IEnumerator ChangeTypeCor()
        {
            if (RoyalEffectsProfile.Get && NextType != ItemsTypes.NONE)
            {
                ItemsTypes previousType = currentType;
                ItemsTypes createdType = NextType;
                currentType = createdType; debugType = createdType; NextType = ItemsTypes.NONE;
                if (currentType == ItemsTypes.BOMB) color = 555;
                appeared = true; sprRenderer.sprite = RoyalPieceSkin.For(currentType, color);
                bool rocket = createdType == ItemsTypes.HORIZONTAL_STRIPPED || createdType == ItemsTypes.VERTICAL_STRIPPED;
                bool tnt = createdType == ItemsTypes.PACKAGE;
                // The Color Ball takes the same seat as a rocket or TNT: its Reveal has to play on the
                // cell it was made in. Without this it began the sequence up to three cells above the
                // board and fell through it, so the reward moment drifted down the screen.
                bool orb = createdType == ItemsTypes.BOMB;
                if (rocket || tnt || orb || createdType == ItemsTypes.HELICOPTER)
                {
                    StopIdleAnim(); if (anim) anim.enabled = false;
                    falling = false; justCreatedItem = false; royalSeatedPowerup = true;
                    if (square) transform.position = square.transform.position + Vector3.back * .2f;
                    juiceBaseScale = transform.localScale; xScale = juiceBaseScale.x; yScale = juiceBaseScale.y;
                    sprRenderer.enabled = true; sprRenderer.color = Color.white;
                    royalSkin?.ApplyNow();
                }
                if (previousType != currentType)
                    AvatarEvents.NotifyPowerupCreated(LevelManager.THIS, this);
                if (RoyalThrone.ReferenceGameplay.ReferencePieceVisual.Attach(this, true)) yield break;
                if (tnt) TNT3DAnimator.RevealSprite(this);
                else
                {
                    // The approved Color Ball Reveal replaces the static orb from its first frame.
                    if (orb) RoyalThrone.Boosters.ColorBall.V4.ColorBallSeatV4.Attach(this, true);
                    if (createdType == ItemsTypes.HELICOPTER) RoyalThrone.Boosters.Propeller.V5.PropellerSeatV5.Attach(this, true);
                    else if (!orb) RoyalPowerupDirector.Get?.Creation(transform.position);
                }
                if (rocket)
                    yield return RoyalRocketCreation();
                yield break;
            }
            if (NextType == ItemsTypes.HORIZONTAL_STRIPPED)
            {
                anim.SetTrigger("appear");
                SoundBase.Instance.PlaySound(SoundBase.Instance.appearStipedColorBomb);
            }
            else if (NextType == ItemsTypes.VERTICAL_STRIPPED)
            {
                anim.SetTrigger("appear");
                SoundBase.Instance.PlaySound(SoundBase.Instance.appearStipedColorBomb);
            }
            else if (NextType == ItemsTypes.PACKAGE)
            {
                anim.SetTrigger("appear");
                SoundBase.Instance.PlaySound(SoundBase.Instance.appearPackage);

            }
            else if (NextType == ItemsTypes.BOMB)
            {
                anim.SetTrigger("appear");
                SoundBase.Instance.PlaySound(SoundBase.Instance.appearStipedColorBomb);
                color = 555;
            }
            while (!appeared)
                yield return new WaitForFixedUpdate();

            if (NextType == ItemsTypes.NONE)
                yield break;
            // sprRenderer.enabled = true;
            if (NextType == ItemsTypes.HORIZONTAL_STRIPPED)
                sprRenderer.sprite = stripedItems[color].horizontal;
            else if (NextType == ItemsTypes.VERTICAL_STRIPPED)
                sprRenderer.sprite = stripedItems[color].vertical;
            else if (NextType == ItemsTypes.PACKAGE)
                sprRenderer.sprite = packageItems[color];
            else if (NextType == ItemsTypes.BOMB)
                sprRenderer.sprite = bombItems[0];

            //     square.DestroyBlock();

            ItemsTypes previousLegacyType = currentType;
            debugType = NextType;
            currentType = NextType;
            NextType = ItemsTypes.NONE;

            if (previousLegacyType != currentType)
                AvatarEvents.NotifyPowerupCreated(LevelManager.THIS, this);

        }

        public bool RoyalRocketRevealing { get; private set; }
        IEnumerator RoyalRocketCreation()
        {
            // Match-4 already routes through ChangeTypeCor. The piece animator poses this item's
            // sprite in place, so no duplicate prefab, gameplay event, or serialized reference exists.
            var reveal = RoyalRocketPieceAnimator.PlayReveal(this);
            if (!reveal) yield break;
            RoyalRocketRevealing = true;
            try
            {
                while (reveal && reveal.Current == RoyalRocketPieceAnimator.Phase.Reveal && !destroying)
                    yield return null;
            }
            finally { RoyalRocketRevealing = false; }
        }

        public void SetAnimationDestroyingFinished()
        {
            LevelManager.THIS.itemsHided = true;
            animationFinished = true;
        }

        /// <summary>
        /// Safety net for the destroy handshake. JuiceDestroyCor normally raises
        /// animationFinished, but a fruit can be deactivated out from under it (a COLLECT
        /// target fires LevelManager.AnimateCollectedItem -> SetActive(false), which stops
        /// the coroutine for good). Without this, a destroying item could sit forever with
        /// animationFinished false. IsAllDestoyFinished happens to skip inactive objects
        /// today, so this is belt-and-braces rather than a live deadlock fix.
        /// </summary>
        private void OnDisable()
        {
            if (destroying && !animationFinished && LevelManager.THIS != null)
                SetAnimationDestroyingFinished();
        }

        #region Destroying

        public void DestroyItem(bool showScore = false, string anim_name = "", bool explEffect = false)
        {

            if (inputSwapLocked)
            {
                if (!deferredInputHit) { deferredInputHit = true; StartCoroutine(DestroyAfterInputSwap(showScore, anim_name, explEffect)); }
                return;
            }

            if (destroying)
                return;
            // if (nextType != ItemsTypes.NONE) return;
            if (this == null)
                return;
            StopIdleAnim();
            destroying = true;
            square.item = null;
            if (NextType != ItemsTypes.NONE) square.Reserve(this);   // a power-up will appear here: see Square.reservedFor
            _boxCollider2D.enabled = false;//2.1.5 prevents to early moving

            if (this == null)
                return;

            StartCoroutine(DestroyCor(showScore, anim_name, explEffect));


        }

        IEnumerator DestroyAfterInputSwap(bool showScore, string animationName, bool effect)
        {
            try
            {
                while (inputSwapLocked) yield return null;
                if (!destroying && square && square.item == this) DestroyItem(showScore, animationName, effect);
            }
            finally { deferredInputHit = false; }
        }

        IEnumerator DestroyCor(bool showScore = false, string anim_name = "", bool explEffect = false)
        {
            bool royalSpecial = RoyalEffectsProfile.Get && RoyalPowerupDirector.IsPower(currentType);
            if (royalSpecial || royalConsumed)
            {
                if (!royalConsumed) RoyalPowerupDirector.Get.Queue(this);
                sprRenderer.enabled = false; if (anim) anim.enabled = false; SetAnimationDestroyingFinished();
            }
            //if (anim_name == "")
            //{
            if (royalSpecialGather) yield return RoyalSpecialFormation.Gather(this);
            else if (royalSpecial || royalConsumed) { }
            else if (currentType == ItemsTypes.HORIZONTAL_STRIPPED)
                PlayDestroyAnimation("destroy");
            else if (currentType == ItemsTypes.VERTICAL_STRIPPED)
                PlayDestroyAnimation("destroy");
            else if (currentType == ItemsTypes.PACKAGE)
            {
                PlayDestroyAnimation("destroy");
                yield return new WaitForSeconds(0.1f);

                GameObject partcl = Instantiate(Resources.Load("Prefabs/Effects/Firework"), transform.position, Quaternion.identity) as GameObject;
                var main = partcl.GetComponent<ParticleSystem>().main;
                main.startColor = LevelManager.THIS.scoresColors[color];
                Destroy(partcl, 1f);
            }
            else if (currentType != ItemsTypes.INGREDIENT && currentType != ItemsTypes.BOMB)
            {
                var juiceCfg = JuiceConfig.Instance;
                if (juiceCfg.EnableJuice)
                {
                    // The layered burst replaces the old per-colour flipbook, which drew
                    // at sorting order 0 (behind the fruits) and was the "too basic" look.
                    // JuiceDestroyCor owns the fruit's disappearance and raises
                    // animationFinished, so the cascade unblocks exactly as before.
                    SoundBase.Instance.PlaySound(SoundBase.Instance.destroy[Random.Range(0, SoundBase.Instance.destroy.Length)]);
                    StartCoroutine(JuiceDestroyCor());
                }
                else
                {
                    PlayDestroyAnimation("destroy");
                    // GameObject partcl = Instantiate(Resources.Load("Prefabs/Effects/ItemExpl"), transform.position, Quaternion.identity) as GameObject;
                    var partcl = LevelManager.THIS.GetExplFromPool();
                    if (partcl != null)
                    {
                        partcl.GetComponent<ItemAnimEvents>().item = this;
                        partcl.transform.localScale = Vector3.one * 0.5f;
                        partcl.transform.position = transform.position;
                        partcl.GetComponent<Animator>().SetInteger("color", color);
                        SoundBase.Instance.PlaySound(SoundBase.Instance.destroy[Random.Range(0, SoundBase.Instance.destroy.Length)]);
                        //   Destroy(partcl, 1f);
                    }
                }
                if (explEffect)
                {
                    GameObject partcl1 = Instantiate(Resources.Load("Prefabs/Effects/Replace"), transform.position, Quaternion.identity) as GameObject;
                    Destroy(partcl1, 1f);

                }
            }
            //}
            //else
            //    PlayDestroyAnimation(anim_name);

            if (LevelManager.THIS.limitType == LIMIT.TIME && transform.Find("5sec") != null)
            {
                GameObject FiveSec = transform.Find("5sec").gameObject;
                FiveSec.transform.SetParent(null);
                #if UNITY_5
			FiveSec.GetComponent<Animation>().clip.legacy = true;
                #endif

                FiveSec.GetComponent<Animation>().Play("5secfly");
                Destroy(FiveSec, 1);
                if (LevelManager.THIS.gameStatus == GameState.Playing)
                    LevelManager.THIS.Limit += 5;
            }

            //Color color = sprRenderer.sprite.texture.GetPixel(sprRenderer.sprite.texture.width / 2 - 10, sprRenderer.sprite.texture.height / 2 - 10);
            if (showScore)
                LevelManager.THIS.PopupScore( currentType != ItemsTypes.INGREDIENT?LevelManager.THIS.scoreForItem: LevelManager.THIS.scoreForIngredients, transform.position, color);

            LevelManager.THIS.CheckCollectedTarget(gameObject);

            while (!animationFinished && currentType == ItemsTypes.NONE)
                yield return new WaitForFixedUpdate();

            bool campaignAdjacentMatch = royalMatchSize >= 3 && !royalBlastClear && !royalRocketClear &&
                currentType == ItemsTypes.NONE;
            square.DestroyBlock(royalRocketClear, royalCampaignHitWave, campaignAdjacentMatch);
            if (royalSpecial || royalConsumed) { }
            else if (currentType == ItemsTypes.HORIZONTAL_STRIPPED)
                DestroyHorizontal();
            else if (currentType == ItemsTypes.VERTICAL_STRIPPED)
                DestroyVertical();
            else if (currentType == ItemsTypes.PACKAGE)
                DestroyPackage();
            else if (currentType == ItemsTypes.BOMB && LevelManager.THIS.gameStatus == GameState.PreWinAnimations)
            {
                var candidates = LevelManager.THIS.GetRandomItems(1);
                if (candidates.Count > 0) CheckChocoBomb(this, candidates[0]);
            }

            if (NextType != ItemsTypes.NONE)
            {
                bool rocket = RoyalEffectsProfile.Get && (NextType == ItemsTypes.HORIZONTAL_STRIPPED || NextType == ItemsTypes.VERTICAL_STRIPPED);
                square.Release(this);
                Item i = square.GenItem(!rocket && !(RoyalEffectsProfile.Get && NextType == ItemsTypes.PACKAGE), campaignRefill: false);
                i.NextType = NextType;
                i.SetColor(color);
                i.ChangeType();
                if (royalSpecialGather) RoyalSpecialCreationFlash.Attach(i);
            }

            if (destroying)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Colour the VFX inherits from this fruit. Uses the game's own per-colour
        /// palette (the same array the score popups and firework already use) so the
        /// sparkles stay consistent with the existing art direction.
        /// </summary>
        /// <summary>
        /// True if this transform belongs to the "5sec" time-bonus flyer, which owns its
        /// own animation and must not be touched by the destroy fade.
        /// </summary>
        private static bool IsBonusFlyer(Transform t)
        {
            while (t != null)
            {
                if (t.name == "5sec") return true;
                t = t.parent;
            }
            return false;
        }

        public Color GetJuiceColor()
        {
            var lm = LevelManager.THIS;
            if (lm != null && lm.scoresColors != null && lm.scoresColors.Length > 0 &&
                color >= 0 && color < lm.scoresColors.Length)
            {
                return lm.scoresColors[color];
            }
            return Color.white;
        }

        /// <summary>
        /// The premium 3-match pop, driven entirely in code so the timing is exact:
        ///
        ///   0ms      stagger offset (so neighbours never pop on the same frame)
        ///   0-25ms   anticipation - a tiny inward pull
        ///   25-60ms  squash        - X 1.07 / Y 0.92
        ///   60-85ms  rebound       - short stretch back up + additive shine
        ///   85ms     impact flash + star burst + micro sparkles + fragments
        ///   85-160ms fruit scales up slightly and fades out
        ///
        /// animationFinished is raised as soon as the fruit is gone, so the board
        /// resolves while the particles are still finishing on their own.
        /// </summary>
        [HideInInspector] public bool royalBlastClear;
        [HideInInspector] public bool royalPropellerV5Clear;
        [HideInInspector] public bool royalTntClear;
        [HideInInspector] public Vector3 royalTntOrigin;
        [HideInInspector] public bool royalRocketClear;
        [HideInInspector] public int royalCampaignHitWave;
        [HideInInspector] public int royalMatchSize;
        IEnumerator JuiceDestroyCor()
        {
            var cfg = JuiceConfig.Instance;

            // Stop legacy Sprite-child scale keys before stagger or shine snapshots.
            // The parent keeps the same timed anticipation/squash/rebound below.
            suppressIdleScale = true;
            StopIdleAnim();
            if (anim != null) anim.enabled = false;
            royalSkin?.ApplyNow();

            if (royalTntClear)
            {
                // Keep the source visible for the local impact, then release painted debris.
                yield return new WaitForSeconds(.018f);
                CandyShatterService.Instance.Shatter(sprRenderer,1f,true,false,true);
                foreach(var renderer in GetComponentsInChildren<SpriteRenderer>(true))
                    if(renderer&&!IsBonusFlyer(renderer.transform))renderer.enabled=false;
                yield return new WaitForSeconds(.035f);
                SetAnimationDestroyingFinished();yield break;
            }

            // The rocket has already arrived and held its local white impact before
            // this call. Snapshot the painted chips before target collection hides it.
            if (royalRocketClear)
            {
                CandyShatterService.Instance.Shatter(sprRenderer, 1f, true, true);
                foreach (var renderer in GetComponentsInChildren<SpriteRenderer>(true))
                    if (renderer && !IsBonusFlyer(renderer.transform)) renderer.enabled = false;
                yield return new WaitForSeconds(.065f);
                SetAnimationDestroyingFinished();
                yield break;
            }

            // Independent V5 cosmetic hit; protected TNT/Rocket handling above remains authoritative.
            if (royalPropellerV5Clear)
            {
                RoyalThrone.Boosters.Propeller.V5.PropellerVfxV5.Break(sprRenderer, color);
                foreach (var renderer in GetComponentsInChildren<SpriteRenderer>(true))
                    if (renderer && !IsBonusFlyer(renderer.transform)) renderer.enabled = false;
                yield return new WaitForSeconds(.065f);
                SetAnimationDestroyingFinished(); yield break;
            }

            // Restore the original sprite-led pop; soften only confirmed matches.
            bool smoothMatch = royalMatchSize >= 3 && !royalBlastClear;
            bool immediateMatch = smoothMatch && royalMatchSize == 3 && NextType == ItemsTypes.NONE;
            float anticipationTime = immediateMatch ? 0f : smoothMatch ? .03f : cfg.MatchAnticipation;
            float squashTime = immediateMatch ? 0f : smoothMatch ? .045f : cfg.MatchSquashDuration;
            float reboundTime = immediateMatch ? 0f : smoothMatch ? .045f : cfg.MatchReboundDuration;
            float destroyTime = smoothMatch ? .09f : cfg.MatchDestroyDuration;

            // Schedule the VFX UP FRONT, before any yield, and let the service play them
            // on its own clock. This coroutine only owns the fruit's squash/stretch.
            //
            // Collection can hide the source renderer immediately after this starts.
            // Schedule its shine and burst before the first yield so those still play.
            {
                var svc = MatchVFXService.Instance;
                if (svc != null)
                {
                    float toShine = juiceStagger + anticipationTime + squashTime;
                    float toBurst = toShine + reboundTime;

                    if (sprRenderer != null && sprRenderer.enabled)
                        svc.PlayFruitShine(sprRenderer, 0.09f, 0.85f, toShine);

                    svc.PlayMatchBurst(transform.position, GetJuiceColor(), royalBlastClear?.3f:juiceIntensity, toBurst);
                }
            }

            if (juiceStagger > 0f)
                yield return new WaitForSeconds(juiceStagger);

            // Fade this fruit's own renderers, but NEVER the "5sec" time-bonus flyer:
            // on TIME levels DestroyCor unparents that child moments later so it can play
            // its own 1-second fly-away animation, and fading it here would blank it out.
            var renderers = GetComponentsInChildren<SpriteRenderer>(true);
            int rendererCount = 0;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null || IsBonusFlyer(renderers[i].transform)) continue;
                renderers[rendererCount++] = renderers[i];
            }

            Vector3 baseScale = juiceBaseScale;
            float squash = immediateMatch ? 0f : smoothMatch ? .045f : cfg.MatchSquashAmount;

            // --- 1. anticipation ------------------------------------------------
            float t = 0f;
            float dur = Mathf.Max(0.0001f, anticipationTime);
            float anticip = immediateMatch ? 0f : cfg.MatchAnticipationScale;
            while (t < 1f && !immediateMatch)
            {
                t += Time.deltaTime / dur;
                float e = smoothMatch ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t)) : Easing.OutQuad(Mathf.Clamp01(t));
                float s = 1f - anticip * e;
                transform.localScale = new Vector3(baseScale.x * s, baseScale.y * s, baseScale.z);
                yield return null;
            }

            // --- 2. squash ------------------------------------------------------
            t = 0f;
            dur = Mathf.Max(0.0001f, squashTime);
            float fromS = 1f - anticip;
            while (t < 1f && !immediateMatch)
            {
                t += Time.deltaTime / dur;
                float e = smoothMatch ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t)) : Easing.OutCubic(Mathf.Clamp01(t));
                float sx = Mathf.Lerp(fromS, 1f + squash, e);
                float sy = Mathf.Lerp(fromS, 1f - squash * 1.15f, e);
                transform.localScale = new Vector3(baseScale.x * sx, baseScale.y * sy, baseScale.z);
                yield return null;
            }

            // --- 3. rebound (the shine was scheduled up front) ------------------
            t = 0f;
            dur = Mathf.Max(0.0001f, reboundTime);
            float reb = immediateMatch ? 0f : smoothMatch ? .065f : cfg.MatchReboundAmount;
            while (t < 1f && !immediateMatch)
            {
                t += Time.deltaTime / dur;
                float e = smoothMatch ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t)) : Easing.OutQuad(Mathf.Clamp01(t));
                float sx = Mathf.Lerp(1f + squash, 1f - reb * 0.35f, e);
                float sy = Mathf.Lerp(1f - squash * 1.15f, 1f + reb, e);
                transform.localScale = new Vector3(baseScale.x * sx, baseScale.y * sy, baseScale.z);
                yield return null;
            }

            // --- 4. break apart -------------------------------------------------
            t = 0f;
            dur = Mathf.Max(0.0001f, destroyTime);
            float startX = 1f - reb * 0.35f;
            float startY = 1f + reb;

            var shatter = cfg.EnableCandyShatter ? CandyShatterService.Instance : null;
            bool didShatter = false;
            RoyalIceBreak.Early(square);
            if (shatter != null && sprRenderer != null && sprRenderer.enabled && sprRenderer.sprite != null)
            {
                // Hand the candy's own SpriteRenderer to the shatter service and hide the
                // intact sprite on the SAME frame, so the chunks take over its silhouette
                // exactly. That swap is what makes it read as breaking rather than as the
                // sprite vanishing and debris appearing afterwards.
                shatter.Shatter(sprRenderer, juiceIntensity,royalBlastClear);
                didShatter = true;
                for (int i = 0; i < rendererCount; i++)
                {
                    if (renderers[i] != null)
                        renderers[i].enabled = false;
                }
            }

            // Run out the remaining destroy time either way, so animationFinished is raised
            // after the visible pop; pooled fragments continue independently.
            while (t < 1f)
            {
                t += Time.deltaTime / dur;
                float k = Mathf.Clamp01(t);

                if (!didShatter)
                {
                    float e = Easing.InQuad(k);
                    // A touch of growth as it dissolves reads as "popped", not "shrunk away".
                    float s = Mathf.Lerp(1f, 1.25f, e);
                    transform.localScale = new Vector3(baseScale.x * startX * s, baseScale.y * startY * s, baseScale.z);

                    float a = 1f - Easing.OutQuad(k);
                    for (int i = 0; i < rendererCount; i++)
                    {
                        if (renderers[i] == null) continue;
                        Color c = renderers[i].color;
                        c.a = a;
                        renderers[i].color = c;
                    }
                }
                yield return null;
            }

            for (int i = 0; i < rendererCount; i++)
            {
                if (renderers[i] != null)
                    renderers[i].enabled = false;
            }

            // Unblock the cascade. Particles keep playing independently from the pool.
            SetAnimationDestroyingFinished();
        }

        public void DestroyHorizontal()
        {
            SoundBase.Instance.PlaySound(SoundBase.Instance.strippedExplosion);
            LevelManager.THIS.StrippedShow(gameObject, true);
            List<Item> itemsList = LevelManager.THIS.GetRow(square.row);
            foreach (Item item in itemsList)
            {
                if (item != null)
                {
                    if (item.currentType != ItemsTypes.BOMB && item.currentType != ItemsTypes.INGREDIENT)
                        item.DestroyItem(true);
                }
            }
            List<Square> sqList = LevelManager.THIS.GetRowSquaresObstacles(square.row);
            foreach (Square item in sqList)
            {
                if (item != null)
                    item.DestroyBlock();
            }
        }

        public void DestroyVertical()
        {
            SoundBase.Instance.PlaySound(SoundBase.Instance.strippedExplosion);
            LevelManager.THIS.StrippedShow(gameObject, false);
            List<Item> itemsList = LevelManager.THIS.GetColumn(square.col);
            foreach (Item item in itemsList)
            {
                if (item != null)
                {
                    if (item.currentType != ItemsTypes.BOMB && item.currentType != ItemsTypes.INGREDIENT)
                        item.DestroyItem(true);
                }
            }
            List<Square> sqList = LevelManager.THIS.GetColumnSquaresObstacles(square.col);
            foreach (Square item in sqList)
            {
                if (item != null)
                    item.DestroyBlock();
            }


        }

        public void DestroyPackage()
        {
            SoundBase.Instance.PlaySound(SoundBase.Instance.destroyPackage);

            List<Item> itemsList = LevelManager.THIS.GetItemsAround(square);
            foreach (Item item in itemsList)
            {
                if (item != null)
                    if (item != null)
                    {
                        if (item.currentType != ItemsTypes.BOMB && item.currentType != ItemsTypes.INGREDIENT)
                            item.DestroyItem(true, "destroy_package");
                    }
            }
            SoundBase.Instance.PlaySound(SoundBase.Instance.explosion);
            currentType = ItemsTypes.NONE;
            DestroyItem(true);
        }

        public void DestroyColor(int p)
        {
            SoundBase.Instance.PlaySound(SoundBase.Instance.colorBombExpl);

            GameObject[] items = GameObject.FindGameObjectsWithTag("Item");
            foreach (GameObject item in items)
            {
                if (item.GetComponent<Item>().color == p)
                    item.GetComponent<Item>().DestroyItem(true, "", true);
            }
        }

        void PlayDestroyAnimation(string anim_name)
        {
            anim.SetTrigger(anim_name);

        }

        public void SmoothDestroy()
        {
            StartCoroutine(SmoothDestroyCor());
        }

        IEnumerator SmoothDestroyCor()
        {
            square.item = null;
            _boxCollider2D.enabled = false;//2.1.5 prevents to early moving
            anim.SetTrigger("disAppear");
            yield return new WaitForSeconds(1);
            Destroy(gameObject);
        }

        #endregion

        private void Update()
        {
            if (InputManager.Instance != null)
            {
                if (InputManager.Instance.WasTouchPressed)
                {
                    if (CanAcceptInput)
                    {
                        Collider2D hit = Physics2D.OverlapPoint(Camera.main.ScreenToWorldPoint(InputManager.Instance.TouchPosition));
                        if (hit != null && hit.gameObject == gameObject)
                        {
                            dragThis = true;
                            mousePos = GetMousePosition();
                        }
                    }
                }

                if (InputManager.Instance.WasTouchReleased)
                {
                    if (dragThis)
                    {
                        if (CanAcceptInput && switchDirection == Vector3.zero && Vector3.Distance(GetMousePosition(), mousePos) < .25f) RoyalPowerupDirector.Tap(this);
                        dragThis = false;
                        if (switchDirection != Vector3.zero)
                            ResetDrag();
                    }
                }

                if (InputManager.Instance.IsTouching && dragThis)
                {
                    if (CanAcceptInput)
                    {
                        CheckItemDrag();
                    }
                    else if (LevelManager.THIS.gameStatus != GameState.Playing ||
                             (MenuManager.Instance != null && MenuManager.Instance.BlocksBoardInput))
                    {
                        // A popup opened or the level ended mid-drag: drop the drag instead of
                        // leaving it armed to complete once play resumes. A drag held while the
                        // board resolves its own move is left alone, as before.
                        ResetDrag();
                    }
                }
            }
        }

        public void OnMouseDown()
        {
            if (RoyalThrone.Testing.RoyalSave.BlockGameplayInput) return;
            if (CanAcceptInput)
            {
                dragThis = true;
                mousePos = GetMousePosition();
            }
        }

        public void OnMouseUp()
        {
            if (RoyalThrone.Testing.RoyalSave.BlockGameplayInput) { dragThis = false; return; }
            if (dragThis && CanAcceptInput && switchDirection == Vector3.zero && Vector3.Distance(GetMousePosition(), mousePos) < .25f) RoyalPowerupDirector.Tap(this);
            dragThis = false;
            if (switchDirection != Vector3.zero)
                ResetDrag();
        }

        public void OnMouseDrag()
        {
            if (RoyalThrone.Testing.RoyalSave.BlockGameplayInput) return;
            if (dragThis && CanAcceptInput)
            {
                CheckItemDrag();
            }
        }
    }

    [Serializable]
    public class StripedItem
    {
        public Sprite horizontal;
        public Sprite vertical;
    }
}
