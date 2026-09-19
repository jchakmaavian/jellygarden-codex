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
using RoyalThrone.Match3Vfx;
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
        INGREDIENT
    }

    public class Item : MonoBehaviour
    {
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
        // Where the finger went down, in SCREEN pixels. The drag used to be
        // measured between two world points, but the camera is still settling
        // when a level opens, so the same screen pixel maps to a world point
        // that drifts upward by ~0.115 units between the press frame and the
        // next one. That drift alone cleared the old 0.1 world-unit threshold
        // and locked switchDirection to "down" before the finger had moved.
        // Screen pixels do not move when the camera does. STORY: added.
        private Vector2 pressScreenPos;
        private bool hasPressScreenPos;
        /// <summary>How far the finger must travel before a swipe is a swipe.</summary>
        private const float DragThresholdPixels = 25f;
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
        private void Awake()
        {
            _boxCollider2D = GetComponent<BoxCollider2D>();
            JellyGarden.Scripts.Story.StorybookBoardArt.Apply(this);
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

        }

        public void SetColor(int col)
        {
            color = col;
            if (color < items.Length)
                sprRenderer.sprite = items[color];
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
            StartCoroutine(AnimIdleStart());

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
                // STORY: stand aside while a pop or a punch owns this gem.
                //
                // This loop does not merely READ xScale, it drives it: it walks
                // xScale toward a target captured when the coroutine started and
                // rewrites localScale every fixed update. So a story effect that
                // set xScale had it walked straight back at 50Hz, before the
                // effect was on screen for long enough to see. Composing with it
                // was the original approach and it does not work.
                //
                // Skipping the WRITE rather than the whole loop is deliberate:
                // the loop keeps its own phase, so the gem resumes breathing
                // from where it was instead of jumping when the effect ends.
                if (!_popOwned)
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
            hasPressScreenPos = false;
        }

        public void CheckItemDrag()
        {
            if (dragThis)
            {
                if (!hasPressScreenPos)
                    return;

                Vector2 now = InputManager.Instance.TouchPosition;
                Vector2 travelled = pressScreenPos - now;
                if (travelled.magnitude < DragThresholdPixels)
                    return;

                // SwitchDirection reads the kit's convention — (where the finger
                // started - where it is now) — and screen Y and world Y both grow
                // upward, so the screen delta carries the same signs the world one
                // did. Only the scale differs, and nothing here reads the scale:
                // the four branches compare |x| against |y| and test sign.
                deltaPos = new Vector3(travelled.x, travelled.y, 0f);
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

                if ((currentType == ItemsTypes.BOMB || switchItem.currentType == ItemsTypes.BOMB) && (currentType != ItemsTypes.INGREDIENT && switchItem.currentType != ItemsTypes.INGREDIENT))
                    matchesHere++;
                if (currentType > 0 && switchItem.currentType > 0)
                    matchesHere++;
                if (currentType == ItemsTypes.INGREDIENT && switchItem.currentType == ItemsTypes.INGREDIENT)
                    matchesHere = 0;
                float startTime = Time.time;
                Vector3 startPos = transform.position;

                float speed = 5;
                float distCovered = 0;
                while (distCovered < 1)
                {
                    distCovered = (Time.time - startTime) * speed;
                    transform.position = Vector3.Lerp(startPos, neighborSquare.transform.position + Vector3.back * 0.3f, distCovered);
                    switchItem.transform.position = Vector3.Lerp(neighborSquare.transform.position + Vector3.back * 0.2f, startPos, distCovered);
                    yield return new WaitForFixedUpdate();
                }
                // STORY: a rocket or a bomb swiped into a cell fires from the
                // cell it lands in, so a swap carrying one is never rejected —
                // landing IS the move. The kit's own rejection test is kept
                // whole above and simply overruled, so deleting the two lines
                // that mention `landing` restores the kit exactly.
                Item landing = LandingPowerup(this, switchItem);

                bool reject = matchesHere <= 0 && matchesInNeithborItem <= 0 && LevelManager.THIS.ActivatedBoost.type != BoostType.Hand ||
                    ((currentType == ItemsTypes.BOMB || switchItem.currentType == ItemsTypes.BOMB) && (currentType == ItemsTypes.INGREDIENT || switchItem.currentType == ItemsTypes.INGREDIENT) && (matchesHere + matchesInNeithborItem <= 2)) ||
                    (((int)currentType >= 1 || (int)switchItem.currentType >= 1) && (currentType == ItemsTypes.INGREDIENT || switchItem.currentType == ItemsTypes.INGREDIENT) && (matchesHere + matchesInNeithborItem <= 2));

                if (landing != null)
                    reject = false;

                if (reject)
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

                    // STORY: fire it where it now sits. This runs whether or not
                    // the swap also made a match — a swipe on a powerup always
                    // means "use it here" — and charges nothing, because the
                    // swap above has already charged the move that carried it.
                    if (landing != null)
                        landing.LandActivate();
                }
                //Debug.Break();
                //yield return new WaitForSeconds(2);


                startTime = Time.time;
                distCovered = 0;
                while (distCovered < 1 && backMove)
                {
                    distCovered = (Time.time - startTime) * speed;
                    transform.position = Vector3.Lerp(neighborSquare.transform.position + Vector3.back * 0.3f, startPos, distCovered);
                    switchItem.transform.position = Vector3.Lerp(startPos, neighborSquare.transform.position + Vector3.back * 0.2f, distCovered);
                    yield return new WaitForFixedUpdate();
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

        /// <summary>
        /// Anchor a drag that something other than this item's own Update began —
        /// LevelManager starts one when it is the thing that hit-tested the touch.
        /// Without the screen anchor CheckItemDrag has nothing to measure from and
        /// the drag never resolves. STORY: added.
        /// </summary>
        public void BeginDragFromCurrentTouch()
        {
            pressScreenPos = InputManager.Instance.TouchPosition;
            hasPressScreenPos = true;
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
            float startTime = Time.time;
            Vector3 startPos = transform.position;
            float speed = 10;
            if (LevelManager.THIS.gameStatus == GameState.PreWinAnimations)
                speed = 10;
            float distance = Vector3.Distance(startPos, _square.transform.position);
            float fracJourney = 0;
            if (distance > 0.5f)
            {
                while (fracJourney < 1)
                {
                    speed += 0.2f;
                    float distCovered = (Time.time - startTime) * speed;
                    fracJourney = distCovered / distance;
                    transform.position = Vector3.Lerp(startPos, _square.transform.position + Vector3.back * 0.2f, fracJourney);
                    yield return new WaitForFixedUpdate();

                }
            }
            transform.position = _square.transform.position + Vector3.back * 0.2f;
            if (distance > 0.5f && animate)
            {
                anim.SetTrigger("stop");
                SoundBase.Instance.PlaySound(SoundBase.Instance.drop[Random.Range(0, SoundBase.Instance.drop.Length)]);
            }
            falling = false;
            justCreatedItem = false;
            transform.position = _square.transform.position + Vector3.back * 0.2f;//1.6.1
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
        /// STORY: multiply the idle breathing by a pop stage's scale.
        ///
        /// AnimIdleStart owns transform.localScale — it assigns it from xScale
        /// and yScale every WaitForFixedUpdate, forever, and DestroyItem's
        /// StopCoroutine(AnimIdleStart()) does not stop it (Unity matches the
        /// IEnumerator by reference, and that is a fresh enumerator). So a
        /// caller that wrote localScale directly would be erased within 20ms.
        /// This writes the fields the idle loop reads instead, so the two
        /// compose and the loop applies the result for us.
        ///
        /// The base is captured once, on the first call, so repeated stages
        /// multiply against the gem's resting size rather than compounding.
        /// </summary>
        public void PopScale(float x, float y)
        {
            if (!_popBaseTaken)
            {
                _popBaseX = xScale;
                _popBaseY = yScale;
                _popBaseTaken = true;
            }

            // Take ownership of the transform for the length of the effect. The
            // idle loop checks this before writing; see AnimIdleStart. Without
            // it the loop walks xScale back toward its own target at 50Hz and
            // the effect never becomes visible.
            _popOwned = true;

            // Write the transform DIRECTLY rather than through xScale/yScale.
            // Those two fields belong to the idle loop's own state machine —
            // writing them moves the breathing's target and makes the gem
            // resume from the wrong place once the effect releases.
            transform.localScale = new Vector3(_popBaseX * x, _popBaseY * y, 1);
        }

        /// <summary>
        /// STORY: put the gem back to the scale it was resting at before
        /// PopScale first touched it.
        ///
        /// A gem being DESTROYED never needs this — it is gone a frame later.
        /// A gem merely shaken by a clear next to it (StoryPunchVfx) does: the
        /// idle loop keeps asserting xScale/yScale every fixed update, so a
        /// neighbour left mid-spring stays deformed for the rest of the level
        /// rather than drifting back. Every spring calls this on every path out,
        /// including the early ones.
        /// </summary>
        public void PopRelease()
        {
            if (!_popBaseTaken) return;

            xScale = _popBaseX;
            yScale = _popBaseY;
            transform.localScale = new Vector3(xScale, yScale, 1);
            _popBaseTaken = false;

            // Hand the transform back to the idle loop, which resumes writing
            // on its next fixed update from the phase it kept throughout.
            _popOwned = false;
        }

        private float _popBaseX, _popBaseY;
        private bool _popBaseTaken;

        /// <summary>
        /// True while a story effect owns this gem's localScale. Read by
        /// AnimIdleStart, which otherwise overwrites it 50 times a second.
        /// </summary>
        private bool _popOwned;

        /// <summary>
        /// Take the transform for an effect that writes localScale ITSELF,
        /// rather than through PopScale.
        ///
        /// The VFX kit's Match3PieceMotion.Pop animates root.localScale
        /// directly, which is the one thing AnimIdleStart will not tolerate: it
        /// reassigns localScale from xScale/yScale every fixed update, forever,
        /// and DestroyItem's StopCoroutine(AnimIdleStart()) does not stop it
        /// (Unity matches by reference and that is a fresh enumerator). An
        /// ungated pop is therefore erased within 20ms and reads as a one-frame
        /// flicker. This sets the same flag PopScale sets, so the idle loop
        /// stands aside and the kit's curve is what actually reaches the screen.
        ///
        /// Returns the scale the gem was resting at, which is what the kit's
        /// Match3PopOptions.restScale wants to animate around.
        /// </summary>
        public Vector3 PopClaimForKit()
        {
            if (!_popBaseTaken)
            {
                _popBaseX = xScale;
                _popBaseY = yScale;
                _popBaseTaken = true;
            }

            _popOwned = true;
            return new Vector3(_popBaseX, _popBaseY, 1);
        }

        #region Universal powerups   // STORY: added

        /// <summary>
        /// A powerup that belongs to no colour. Under the story's rules these are
        /// fired by tapping them or by landing them, never by being matched, so
        /// they are excluded from the colour search in Square.FindMoreMatches.
        /// The kit already treated the colour bomb this way; the rocket and the
        /// bomb now behave the same.
        /// </summary>
        public static bool IsUniversalPowerup(Item item)
        {
            if (item == null)
                return false;
            return item.currentType == ItemsTypes.HORIZONTAL_STRIPPED
                || item.currentType == ItemsTypes.VERTICAL_STRIPPED
                || item.currentType == ItemsTypes.PACKAGE
                || item.currentType == ItemsTypes.BOMB;
        }

        /// <summary>
        /// The powerup a swap is landing, or null when it lands none.
        ///
        /// At most one side can qualify. Two powerups swapped together are the
        /// kit's own combinations — CheckChocoBomb, CheckPackageAndStripped and
        /// CheckPackageAndPackage own those, and they do more than either
        /// powerup would alone — and a swap involving an ingredient is left to
        /// the kit's ingredient rules. The colour bomb is excluded for the same
        /// reason it cannot be tapped: it needs a colour to eat, and swapping it
        /// onto one is already how it is fired.
        /// </summary>
        private static Item LandingPowerup(Item dragged, Item partner)
        {
            if (dragged == null || partner == null)
                return null;
            if (dragged.currentType == ItemsTypes.INGREDIENT || partner.currentType == ItemsTypes.INGREDIENT)
                return null;
            if (IsUniversalPowerup(dragged) && IsUniversalPowerup(partner))
                return null;

            if (CanLandFire(dragged)) return dragged;
            if (CanLandFire(partner)) return partner;
            return null;
        }

        /// <summary>Rockets and bombs fire on landing; the colour bomb does not.</summary>
        private static bool CanLandFire(Item item)
        {
            return item != null
                && !item.destroying
                && (item.currentType == ItemsTypes.HORIZONTAL_STRIPPED
                    || item.currentType == ItemsTypes.VERTICAL_STRIPPED
                    || item.currentType == ItemsTypes.PACKAGE);
        }

        /// <summary>
        /// Fire this powerup on the square it has just been swapped into.
        /// DestroyCor routes by type to DestroyHorizontal, DestroyVertical or
        /// DestroyPackage against the square it now holds, so the blast comes
        /// from where it landed and the kit's own sound and particles come with
        /// it. Unlike TapActivate this charges no move; Switching already
        /// charged the one that carried it here.
        /// </summary>
        public bool LandActivate()
        {
            if (!CanLandFire(this) || square == null)
                return false;

            DestroyItem(true);
            LevelManager.THIS.FindMatches();
            return true;
        }

        /// <summary>Whether a tap on this item should fire it where it sits.</summary>
        public bool CanTapActivate()
        {
            if (destroying || falling || square == null)
                return false;
            if (LevelManager.THIS == null || LevelManager.THIS.DragBlocked)
                return false;
            if (LevelManager.THIS.gameStatus != GameState.Playing)
                return false;
            // The colour bomb needs a colour to eat, which a tap cannot supply.
            // It keeps the kit's swap-to-activate.
            return currentType == ItemsTypes.HORIZONTAL_STRIPPED
                || currentType == ItemsTypes.VERTICAL_STRIPPED
                || currentType == ItemsTypes.PACKAGE;
        }

        /// <summary>
        /// Fire this powerup on its own square. DestroyCor already routes by type
        /// to DestroyHorizontal, DestroyVertical or DestroyPackage with the kit's
        /// own sounds and particles, so this only has to supply the trigger and
        /// charge a move for it.
        /// </summary>
        public bool TapActivate()
        {
            if (!CanTapActivate())
                return false;

            if (LevelManager.THIS.limitType == LIMIT.MOVES && LevelManager.THIS.Limit > 0)
                LevelManager.THIS.Limit--;

            DestroyItem(true);
            LevelManager.THIS.FindMatches();
            return true;
        }

        #endregion

        #region Destroying

        private float _rocketHitDelay = -1f;

        public void DestroyByRocket(float delay)
        {
            if (destroying) return;
            animationFinished = false;
            _rocketHitDelay = Mathf.Max(0f, delay);
            DestroyItem(true);
        }

        public void DestroyItem(bool showScore = false, string anim_name = "", bool explEffect = false)
        {

            if (destroying)
                return;
            // if (nextType != ItemsTypes.NONE) return;
            if (this == null)
                return;
            StopCoroutine(AnimIdleStart());
            destroying = true;
            square.item = null;
            _boxCollider2D.enabled = false;//2.1.5 prevents to early moving

            if (this == null)
                return;

            StartCoroutine(DestroyCor(showScore, anim_name, explEffect));


        }

        IEnumerator DestroyCor(bool showScore = false, string anim_name = "", bool explEffect = false)
        {
            bool rocketHit = _rocketHitDelay >= 0f;
            if (rocketHit)
            {
                // DestroyItem has already reserved this square and disabled input. Keep the
                // intact piece visible until impact; the existing animationFinished wait
                // prevents gravity from refilling it while this coroutine is still running.
                PopClaimForKit();
                float elapsed = 0f;
                while (elapsed < _rocketHitDelay)
                {
                    yield return null;
                    if (LevelManager.THIS != null && LevelManager.THIS.gameStatus != GameState.Pause)
                        elapsed += Time.deltaTime;
                }
                JellyGarden.Scripts.Story.StoryRocketVfx.Contact(transform.position,
                    JellyGarden.Scripts.Story.StoryBoardScale.Pitch);
            }
            // The gem's clear is the VFX kit's Match3PieceMotion.Pop, and only
            // that. It replaces the four layers that used to stack here —
            // StoryShatterVfx (chips), StoryPopVfx (the squash/snap wind-up),
            // the pooled ItemExpl flipbook with destroy.anim, and StoryBlastVfx
            // (the burst) — because the kit's single Pop does all four itself:
            // anticipation, squash and rebound on the transform, a sparkle
            // burst, a shine, and a sprite shatter into pooled chips. Running
            // both gave every gem two shatters, two squashes and two bursts.
            //
            // StoryPunchVfx below is deliberately KEPT: it springs the
            // surviving NEIGHBOURS, which is the one thing the kit's pop does
            // not do and does not conflict with.
            float popFadeSpeed = anim == null ? 1f : anim.speed;

            //if (anim_name == "")
            //{
            if (currentType == ItemsTypes.HORIZONTAL_STRIPPED)
                PlayDestroyAnimation("destroy");
            else if (currentType == ItemsTypes.VERTICAL_STRIPPED)
                PlayDestroyAnimation("destroy");
            else if (currentType == ItemsTypes.PACKAGE)
            {
                if (JellyGarden.Scripts.Story.StoryRoyalVfx.TntReady)
                {
                    // The ZIP supplies the TNT ignition sprite and explosion.
                    // DestroyPackage below starts it once for the whole blast.
                    sprRenderer.enabled = false;
                }
                else
                {
                    PlayDestroyAnimation("destroy");
                    yield return new WaitForSeconds(0.1f);
                    GameObject partcl = Instantiate(Resources.Load("Prefabs/Effects/Firework"), transform.position, Quaternion.identity) as GameObject;
                    var main = partcl.GetComponent<ParticleSystem>().main;
                    main.startColor = LevelManager.THIS.scoresColors[color];
                    Destroy(partcl, 1f);
                }
            }
            else if (currentType != ItemsTypes.INGREDIENT && currentType != ItemsTypes.BOMB)
            {
                SoundBase.Instance.PlaySound(SoundBase.Instance.destroy[Random.Range(0, SoundBase.Instance.destroy.Length)]);

                // The clear itself. Three things about how this is started
                // matter, and each one is a hang or a dropped frame if changed:
                //
                // 1. It runs on LevelManager, NOT on this gem. The last line of
                //    DestroyCor destroys this GameObject, and a coroutine
                //    started on a destroyed object stops where it stands — the
                //    pop would be cut off partway and onFinished would never
                //    fire, which is the silent truncation that leaves a board
                //    frozen mid-cascade.
                //
                // 2. It is NOT yielded on. onFinished sets animationFinished,
                //    and the wait further down this method is what actually
                //    blocks on it — exactly as it blocked on the ItemExpl
                //    animation event before. Yielding here as well would wait
                //    for a flag that this method is itself responsible for
                //    reaching.
                //
                // 3. restScale comes from PopClaimForKit rather than
                //    transform.localScale, which both reads the gem's RESTING
                //    size (the idle breathing is mid-cycle at any given moment)
                //    and tells AnimIdleStart to stop overwriting the transform.
                LevelManager.THIS.StartCoroutine(Match3PieceMotion.Pop(
                    transform,
                    sprRenderer,
                    new Match3PopOptions
                    {
                        kind = rocketHit ? Match3PopKind.Rocket : Match3PopKind.Match,
                        burstColor = color >= 0 && color < LevelManager.THIS.scoresColors.Length
                            ? LevelManager.THIS.scoresColors[color]
                            : Color.white,
                        colorIndex = color,
                        restScale = PopClaimForKit(),
                        // The cascade's only unblock now that the ItemExpl
                        // clip's animation event is gone.
                        onFinished = SetAnimationDestroyingFinished
                    }));

                // STORY: stage 7 — the board's own reaction. Each surviving
                // neighbour gets a small spring, falling off with distance.
                // Fire-and-forget by contract: it must NOT be yielded on,
                // because everything below this line in the destroy path is
                // synchronous and the cascade waits on it.
                JellyGarden.Scripts.Story.StoryPunchVfx.Punch(this);
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
            if (currentType == ItemsTypes.HORIZONTAL_STRIPPED)
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

            // STORY: put the Animator's speed back. The gem is usually destroyed
            // on the next line, but a gem that carries a NextType is REUSED by
            // square.GenItem above rather than thrown away, and one left at 4x
            // would play every later animation at four times the pace.
            if (anim != null) anim.speed = popFadeSpeed;

            if (destroying)
            {
                Destroy(gameObject);
            }
        }

        public void DestroyHorizontal()
        {
            SoundBase.Instance.PlaySound(SoundBase.Instance.strippedExplosion);
            if (!JellyGarden.Scripts.Story.StoryRocketVfx.Enabled)
                LevelManager.THIS.StrippedShow(gameObject, true);

            // The effect owns the launch picture; each target owns its delayed hit.
            // Reserving every target before returning keeps the refill barrier intact.
            JellyGarden.Scripts.Story.StoryRocketSweep.Show(this, true);

            List<Item> itemsList = LevelManager.THIS.GetRow(square.row);
            foreach (Item item in itemsList)
            {
                if (item != null)
                {
                    if (item.currentType != ItemsTypes.BOMB && item.currentType != ItemsTypes.INGREDIENT)
                        JellyGarden.Scripts.Story.StoryRocketSweep.Hit(item, this, true);
                }
            }
            List<Square> sqList = LevelManager.THIS.GetRowSquaresObstacles(square.row);
            foreach (Square item in sqList)
            {
                if (item != null)
                    JellyGarden.Scripts.Story.StoryRocketSweep.HitObstacle(item, this, true);
            }
        }

        public void DestroyVertical()
        {
            SoundBase.Instance.PlaySound(SoundBase.Instance.strippedExplosion);
            if (!JellyGarden.Scripts.Story.StoryRocketVfx.Enabled)
                LevelManager.THIS.StrippedShow(gameObject, false);

            // Same reservation and impact timing as the horizontal rocket.
            JellyGarden.Scripts.Story.StoryRocketSweep.Show(this, false);

            List<Item> itemsList = LevelManager.THIS.GetColumn(square.col);
            foreach (Item item in itemsList)
            {
                if (item != null)
                {
                    if (item.currentType != ItemsTypes.BOMB && item.currentType != ItemsTypes.INGREDIENT)
                        JellyGarden.Scripts.Story.StoryRocketSweep.Hit(item, this, false);
                }
            }
            List<Square> sqList = LevelManager.THIS.GetColumnSquaresObstacles(square.col);
            foreach (Square item in sqList)
            {
                if (item != null)
                    JellyGarden.Scripts.Story.StoryRocketSweep.HitObstacle(item, this, false);
            }


        }

        public void DestroyPackage()
        {
            SoundBase.Instance.PlaySound(SoundBase.Instance.destroyPackage);

            // STORY: the explosion, fired ONCE here rather than from DestroyCor.
            //
            // Two reasons, both structural. This method sets currentType to NONE
            // below before calling DestroyItem, so by the time the destroy
            // coroutine runs the bomb no longer identifies as a package and a
            // hook there cannot tell a bomb from an ordinary gem. And the clears
            // this method starts are ordinary clears — Glass Burst's job — so an
            // explosion per cleared cell would draw nine of them.
            JellyGarden.Scripts.Story.StoryBombVfx.Blow(
                transform.position,
                JellyGarden.Scripts.Story.StoryBombVfx.TintFor(color, 0f));

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

            // STORY: the charge-mark-wave sequence, started BEFORE the clear.
            //
            // The positions have to be read while the gems are still on the
            // board: the loop below destroys every one of them in this frame,
            // and a transform read afterwards is a transform of something that
            // no longer exists.
            //
            // The wave then pops them one at a time as it reaches them, over
            // about a second. The gems themselves are already gone by then —
            // that mismatch is deliberate, and the alternative is staggering the
            // kit's own destroys, which hung the board when the rocket tried it.
            // See StoryColorBombVfx's header.
            var targets = new List<Vector3>(32);
            for (int i = 0; i < items.Length; i++)
            {
                Item other = items[i].GetComponent<Item>();
                if (other == null || other.color != p) continue;
                // The SQUARE, not the gem: Item.Switching lerps a gem's
                // transform mid-swap and a colour bomb fires from such a swap.
                targets.Add(other.square != null
                    ? other.square.transform.position
                    : other.transform.position);
            }

            JellyGarden.Scripts.Story.StoryColorBombVfx.Fire(
                square != null ? square.transform.position : transform.position,
                JellyGarden.Scripts.Story.StoryBombVfx.TintFor(p, 0f),
                targets,
                sprRenderer != null ? sprRenderer.sprite : null);

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
                            pressScreenPos = InputManager.Instance.TouchPosition;
                            hasPressScreenPos = true;
                        }
                    }
                }
                
                if (InputManager.Instance.WasTouchReleased)
                {
                    if (dragThis)
                    {
                        // A release that never picked a direction is a tap, not a
                        // swipe. On a universal powerup that fires it where it
                        // sits; on anything else TapActivate is a no-op and the
                        // tap does nothing. STORY: added.
                        bool wasTap = switchDirection == Vector3.zero;

                        dragThis = false;
                        hasPressScreenPos = false;
                        if (switchDirection != Vector3.zero)
                            ResetDrag();
                        else if (wasTap)
                            TapActivate();
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

        // OnMouseDown/OnMouseUp/OnMouseDrag used to live here and are gone.
        // ProjectSettings activeInputHandler is 1 — Input System only — and
        // Unity drives those three from the old input manager, so they were
        // never called on device or in the editor. Update() above is the one
        // live input path; the tap branch in it is what OnMouseUp had.
    }

    [Serializable]
    public class StripedItem
    {
        public Sprite horizontal;
        public Sprite vertical;
    }
}
