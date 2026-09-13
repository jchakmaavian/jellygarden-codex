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
        }

        // Use this for initialization
        void Start()
        {
            gameObject.name = "item " + GetEntityId();
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
            for (int i = 0; i < LevelManager.Instance.colorLimit; i++)
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
                if (canGen && i != exceptColor)
                {
                    remainColors.Add(i);
                }
            }

            //       print(remainColors.Count);
            int randColor = Random.Range(0, LevelManager.Instance.colorLimit);
            if (remainColors.Count > 0)
                randColor = remainColors[Random.Range(0, remainColors.Count)];
            if (exceptColor == randColor)
                randColor = (randColor++) % items.Length;
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
                            color = Array.IndexOf(items, sprRenderer.sprite);
                        }
                    }
                }
                else
                {
                    StartCoroutine(FallingCor(square, true));
                    color = Array.IndexOf(items, sprRenderer.sprite);
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
                if (switchItem != null)
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
            if (switchDirection != Vector3.zero && neighborSquare != null)
            {
                bool backMove = false;
                LevelManager.THIS.DragBlocked = true;
                neighborSquare.item = this;
                square.item = switchItem;
                int matchesHere = neighborSquare.FindMatchesAround().Count;
                int matchesInThisItem = matchesHere;
                int matchesInNeithborItem = square.FindMatchesAround().Count;
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
                    float t = 0f;
                    while (t < 1f)
                    {
                        t += Time.deltaTime / Mathf.Max(0.0001f, dur);
                        float e = juiceCfg.EnableJuice ? Easing.OutCubic(Mathf.Clamp01(t)) : Mathf.Clamp01(t);
                        transform.position = Vector3.LerpUnclamped(startPos, myTarget, e);
                        if (switchItem != null)
                            switchItem.transform.position = Vector3.LerpUnclamped(otherStart, startPos, e);
                        yield return null;
                    }
                    transform.position = myTarget;
                    if (switchItem != null)
                        switchItem.transform.position = startPos;
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
                    if (LevelManager.THIS.ActivatedBoost.type != BoostType.Hand)
                    {
                        if (LevelManager.Instance.limitType == LIMIT.MOVES)
                            LevelManager.THIS.Limit--;
                        LevelManager.THIS.moveID++;
                    }
                    if (LevelManager.THIS.ActivatedBoost.type == BoostType.Hand)
                        LevelManager.THIS.ActivatedBoost = null;
                    switchItem.square = square;
                    square = neighborSquare;
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
                    LevelManager.THIS.DragBlocked = false;
                    StartCoroutine(AI.THIS.CheckPossibleCombines());//2.1.5 prevents early move
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
            square.item = null;
            square = _square;   //need to count all falling items and drop them down in the same time
        }

        public void StartFalling()
        {
            if (!falling)
                StartCoroutine(FallingCor(square, true));
        }

        IEnumerator FallingCor(Square _square, bool animate)
        {
            falling = true;
            Vector3 startPos = transform.position;
            Vector3 endPos = _square.transform.position + Vector3.back * 0.2f;
            float distance = Vector3.Distance(startPos, _square.transform.position);
            var cfg = JuiceConfig.Instance;

            if (distance > 0.5f)
            {
                if (cfg.EnableJuice)
                {
                    // Integrate a real velocity instead of ramping a Lerp factor. This
                    // makes a long drop genuinely take longer than a short one and gives
                    // the accelerating weight the old constant-ish speed lacked.
                    float travelled = 0f;
                    float vel = cfg.FallSpeed;
                    while (travelled < distance)
                    {
                        vel = Mathf.Min(vel + cfg.FallAcceleration * Time.deltaTime, cfg.FallMaxSpeed);
                        travelled += vel * Time.deltaTime;
                        float frac = Mathf.Clamp01(travelled / distance);
                        transform.position = Vector3.LerpUnclamped(startPos, endPos, frac);
                        yield return null;
                    }
                }
                else
                {
                    float startTime = Time.time;
                    float speed = 10;
                    float fracJourney = 0;
                    while (fracJourney < 1)
                    {
                        speed += 0.2f;
                        float distCovered = (Time.time - startTime) * speed;
                        fracJourney = distCovered / distance;
                        transform.position = Vector3.LerpUnclamped(startPos, endPos, fracJourney);
                        yield return new WaitForFixedUpdate();
                    }
                }
            }
            transform.position = endPos;
            if (distance > 0.5f && animate)
            {
                if (cfg.EnableJuice)
                {
                    // Code-driven landing squash on the ROOT transform. The old
                    // anim.SetTrigger("stop") clip ran 258ms on the child Sprite, which
                    // is well past the tactile window; this lands in ~100ms and cannot
                    // fight the Animator because they drive different transforms.
                    StartCoroutine(LandingSquashCor());
                }
                else
                {
                    anim.SetTrigger("stop");
                }
                SoundBase.Instance.PlaySound(SoundBase.Instance.drop[Random.Range(0, SoundBase.Instance.drop.Length)]);
            }
            falling = false;
            justCreatedItem = false;
            transform.position = endPos;//1.6.1
        }

        /// <summary>
        /// Subtle impact on landing: Y dips then overshoots slightly and settles.
        /// Runs alongside gameplay - it never blocks the falling/cascade pipeline.
        /// </summary>
        IEnumerator LandingSquashCor()
        {
            if (destroying) yield break;

            var cfg = JuiceConfig.Instance;
            float dur = cfg.LandingSquashDuration;
            float amount = cfg.LandingSquash;
            if (dur <= 0f || amount <= 0f) yield break;

            suppressIdleScale = true;
            float t = 0f;
            while (t < 1f && !destroying)
            {
                t += Time.deltaTime / dur;
                float k = Mathf.Clamp01(t);
                // 1.00 -> 0.94 -> 1.02 -> 1.00 on Y, with X taking the inverse.
                float dip = -Mathf.Sin(k * Mathf.PI * 1.5f) * Mathf.Pow(1f - k, 0.6f);
                float sy = 1f + dip * amount;
                float sx = 1f - dip * amount * 0.75f;
                transform.localScale = new Vector3(juiceBaseScale.x * sx, juiceBaseScale.y * sy, juiceBaseScale.z);
                yield return null;
            }

            if (!destroying)
            {
                transform.localScale = juiceBaseScale;
                xScale = juiceBaseScale.x;
                yScale = juiceBaseScale.y;
                suppressIdleScale = false;
            }
        }

        public bool GetNearEmptySquares()
        {
            bool nearEmptySquareDetected = false;
            if (square.row < LevelManager.Instance.maxRows - 1 && square.col < LevelManager.Instance.maxCols)
            {
                Square checkingSquare = LevelManager.Instance.GetSquare(square.col + 1, square.row + 1, true);
                if (checkingSquare.CanGoInto() && checkingSquare.item == null && !falling)
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
                if (checkingSquare.CanGoInto() && checkingSquare.item == null && !falling)
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
                currentType = NextType; debugType = NextType; NextType = ItemsTypes.NONE;
                if (currentType == ItemsTypes.BOMB) color = 555;
                appeared = true; sprRenderer.sprite = RoyalPieceSkin.For(currentType, color);
                RoyalPowerupDirector.Get?.Creation(transform.position); yield break;
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

            debugType = NextType;
            currentType = NextType;
            NextType = ItemsTypes.NONE;

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

            if (destroying)
                return;
            // if (nextType != ItemsTypes.NONE) return;
            if (this == null)
                return;
            StopIdleAnim();
            destroying = true;
            square.item = null;
            _boxCollider2D.enabled = false;//2.1.5 prevents to early moving

            if (this == null)
                return;

            StartCoroutine(DestroyCor(showScore, anim_name, explEffect));


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
            if (royalSpecial || royalConsumed) { }
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

            square.DestroyBlock();
            if (royalSpecial || royalConsumed) { }
            else if (currentType == ItemsTypes.HORIZONTAL_STRIPPED)
                DestroyHorizontal();
            else if (currentType == ItemsTypes.VERTICAL_STRIPPED)
                DestroyVertical();
            else if (currentType == ItemsTypes.PACKAGE)
                DestroyPackage();
            else if (currentType == ItemsTypes.BOMB && LevelManager.THIS.gameStatus == GameState.PreWinAnimations)
                CheckChocoBomb(this, LevelManager.THIS.GetRandomItems(1)[0]);

            if (NextType != ItemsTypes.NONE)
            {
                Item i = square.GenItem();
                i.NextType = NextType;
                i.SetColor(color);
                i.ChangeType();
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
        IEnumerator JuiceDestroyCor()
        {
            var cfg = JuiceConfig.Instance;

            // Schedule the VFX UP FRONT, before any yield, and let the service play them
            // on its own clock. This coroutine only owns the fruit's squash/stretch.
            //
            // Why: DestroyCor calls LevelManager.CheckCollectedTarget immediately after
            // starting this coroutine, and on levels with a COLLECT target that leads to
            // AnimateCollectedItem -> _item.SetActive(false) on this very GameObject.
            // Deactivating a GameObject stops its coroutines permanently, so anything
            // fired from later in this method would silently never happen for exactly the
            // fruits that count toward the objective.
            {
                var svc = MatchVFXService.Instance;
                if (svc != null)
                {
                    float toShine = juiceStagger + cfg.MatchAnticipation + cfg.MatchSquashDuration;
                    float toBurst = toShine + cfg.MatchReboundDuration;

                    if (sprRenderer != null && sprRenderer.enabled)
                        svc.PlayFruitShine(sprRenderer, 0.09f, 0.85f, toShine);

                    svc.PlayMatchBurst(transform.position, GetJuiceColor(), royalBlastClear?.3f:juiceIntensity, toBurst);
                }
            }

            if (juiceStagger > 0f)
                yield return new WaitForSeconds(juiceStagger);

            // Own the transform outright for the rest of this object's life.
            suppressIdleScale = true;
            StopIdleAnim();
            if (anim != null)
                anim.enabled = false;

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
            float squash = cfg.MatchSquashAmount;

            // --- 1. anticipation ------------------------------------------------
            float t = 0f;
            float dur = Mathf.Max(0.0001f, cfg.MatchAnticipation);
            float anticip = cfg.MatchAnticipationScale;
            while (t < 1f)
            {
                t += Time.deltaTime / dur;
                float e = Easing.OutQuad(Mathf.Clamp01(t));
                float s = 1f - anticip * e;
                transform.localScale = new Vector3(baseScale.x * s, baseScale.y * s, baseScale.z);
                yield return null;
            }

            // --- 2. squash ------------------------------------------------------
            t = 0f;
            dur = Mathf.Max(0.0001f, cfg.MatchSquashDuration);
            float fromS = 1f - anticip;
            while (t < 1f)
            {
                t += Time.deltaTime / dur;
                float e = Easing.OutCubic(Mathf.Clamp01(t));
                float sx = Mathf.Lerp(fromS, 1f + squash, e);
                float sy = Mathf.Lerp(fromS, 1f - squash * 1.15f, e);
                transform.localScale = new Vector3(baseScale.x * sx, baseScale.y * sy, baseScale.z);
                yield return null;
            }

            // --- 3. rebound (the shine was scheduled up front) ------------------
            t = 0f;
            dur = Mathf.Max(0.0001f, cfg.MatchReboundDuration);
            float reb = cfg.MatchReboundAmount;
            while (t < 1f)
            {
                t += Time.deltaTime / dur;
                float e = Easing.OutQuad(Mathf.Clamp01(t));
                float sx = Mathf.Lerp(1f + squash, 1f - reb * 0.35f, e);
                float sy = Mathf.Lerp(1f - squash * 1.15f, 1f + reb, e);
                transform.localScale = new Vector3(baseScale.x * sx, baseScale.y * sy, baseScale.z);
                yield return null;
            }

            // --- 4. break apart -------------------------------------------------
            t = 0f;
            dur = Mathf.Max(0.0001f, cfg.MatchDestroyDuration);
            float startX = 1f - reb * 0.35f;
            float startY = 1f + reb;

            var shatter = cfg.EnableCandyShatter ? CandyShatterService.Instance : null;
            bool didShatter = false;
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
            // at exactly the same moment as before and the cascade's pacing is unchanged.
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
                    if (!LevelManager.THIS.DragBlocked && !falling && !LevelManager.THIS.gameStatus.Equals(GameState.PreWinAnimations))
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
                        if (switchDirection == Vector3.zero && Vector3.Distance(GetMousePosition(), mousePos) < .25f) RoyalPowerupDirector.Tap(this);
                        dragThis = false;
                        if (switchDirection != Vector3.zero)
                            ResetDrag();
                    }
                }
                
                if (InputManager.Instance.IsTouching && dragThis)
                {
                    if (!LevelManager.THIS.DragBlocked && !falling && !LevelManager.THIS.gameStatus.Equals(GameState.PreWinAnimations))
                    {
                        CheckItemDrag();
                    }
                }
            }
        }

        public void OnMouseDown()
        {
            if (!LevelManager.THIS.DragBlocked && !falling && !LevelManager.THIS.gameStatus.Equals(GameState.PreWinAnimations))
            {
                dragThis = true;
                mousePos = GetMousePosition();
            }
        }

        public void OnMouseUp()
        {
            if (dragThis && switchDirection == Vector3.zero && Vector3.Distance(GetMousePosition(), mousePos) < .25f) RoyalPowerupDirector.Tap(this);
            dragThis = false;
            if (switchDirection != Vector3.zero)
                ResetDrag();
        }

        public void OnMouseDrag()
        {
            if (dragThis && !LevelManager.THIS.DragBlocked && !falling && !LevelManager.THIS.gameStatus.Equals(GameState.PreWinAnimations))
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
