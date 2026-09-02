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

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace JellyGarden.Scripts
{
    public enum FindSeparating
    {
        NONE = 0,
        HORIZONTAL,
        VERTICAL
    }

    public enum SquareTypes
    {
        NONE = 0,
        EMPTY,
        BLOCK,
        WIREBLOCK,
        SOLIDBLOCK,
        DOUBLEBLOCK,
        UNDESTROYABLE,
        THRIVING
    }



    public class Square : MonoBehaviour
    {
        public Item item;
        public int row;
        public int col;
        public SquareTypes type;

        public List<GameObject> block = new List<GameObject>();
        private int ccc;

        // Use this for initialization
        void Start()
        {
            // GenItem();
            if (row == LevelManager.THIS.maxRows - 1)
            {
                if(LevelManager.Instance.targetObject.Any(i=>i.type==Target.INGREDIENT))
                {
                    GameObject obj = Instantiate(Resources.Load("Prefabs/arrow_ingredients")) as GameObject;
                    obj.transform.SetParent(transform);
                    obj.transform.localPosition = Vector3.zero + Vector3.down * 0.8f;
                }
            }

        }

        public Item GenItem(bool falling = true)
        {
            if (IsNone() && !CanGoInto())
                return null;
            var itemInstance = Instantiate(LevelManager.THIS.itemPrefab);
            itemInstance.transform.localScale = Vector2.one * 0.6f;
            itemInstance.square = this;
            //if (!falling)
            //    item.GetComponent<Item>().anim.SetTrigger("reAppear");

            itemInstance.transform.SetParent(transform.parent);
            if (falling)
            {
                itemInstance.transform.position = transform.position + Vector3.back * 0.2f + Vector3.up * 3f;
                itemInstance.justCreatedItem = true;
            }
            else
                itemInstance.transform.position = transform.position + Vector3.back * 0.2f;

            this.item = itemInstance;
            return this.item;
        }

        public Square GetNeighborLeft(bool safe = false)
        {
            if (col == 0 && !safe)
                return null;
            return LevelManager.THIS.GetSquare(col - 1, row, safe);
        }

        public Square GetNeighborRight(bool safe = false)
        {
            if (col >= LevelManager.THIS.maxCols && !safe)
                return null;
            return LevelManager.THIS.GetSquare(col + 1, row, safe);
        }

        public Square GetNeighborTop(bool safe = false)
        {
            if (row == 0 && !safe)
                return null;
            return LevelManager.THIS.GetSquare(col, row - 1, safe);
        }

        public Square GetNeighborBottom(bool safe = false)
        {
            if (row >= LevelManager.THIS.maxRows && !safe)
                return null;
            return LevelManager.THIS.GetSquare(col, row + 1, safe);
        }

        Hashtable FindMoreMatches(int spr_COLOR, Hashtable countedSquares, FindSeparating separating, Hashtable countedSquaresGlobal = null)
        {
            bool globalCounter = true;
            if (countedSquaresGlobal == null)
            {
                globalCounter = false;
                countedSquaresGlobal = new Hashtable();
            }
            ccc++;
            if (this.item == null)
                return countedSquares;
            if (this.item.destroying)
                return countedSquares;
            //    if (LevelManager.THIS.countedSquares.ContainsValue(this.item) && globalCounter) return countedSquares;
            // Universal powerups are colour independent: they are fired by tapping or
            // by landing, never by being matched. The kit already excluded the colour
            // bomb here; the rocket and the bomb now join it. STORY: added.
            if (this.item.color == spr_COLOR && !countedSquares.ContainsValue(this.item) && this.item.currentType != ItemsTypes.INGREDIENT && item.currentType != ItemsTypes.BOMB && !Item.IsUniversalPowerup(this.item))
            {  //2.0
                if (LevelManager.THIS.onlyFalling && this.item.justCreatedItem)
                    countedSquares.Add(countedSquares.Count - 1, this.item);
                else if (!LevelManager.THIS.onlyFalling)
                    countedSquares.Add(countedSquares.Count - 1, this.item);
                else
                    return countedSquares;

                if (separating == FindSeparating.VERTICAL)
                {
                    if (GetNeighborTop() != null)
                        countedSquares = GetNeighborTop().FindMoreMatches(spr_COLOR, countedSquares, FindSeparating.VERTICAL);
                    if (GetNeighborBottom() != null)
                        countedSquares = GetNeighborBottom().FindMoreMatches(spr_COLOR, countedSquares, FindSeparating.VERTICAL);
                }
                else if (separating == FindSeparating.HORIZONTAL)
                {
                    if (GetNeighborLeft() != null)
                        countedSquares = GetNeighborLeft().FindMoreMatches(spr_COLOR, countedSquares, FindSeparating.HORIZONTAL);
                    if (GetNeighborRight() != null)
                        countedSquares = GetNeighborRight().FindMoreMatches(spr_COLOR, countedSquares, FindSeparating.HORIZONTAL);
                }
            }
            return countedSquares;
        }

        public List<Item> FindMatchesAround(FindSeparating separating = FindSeparating.NONE, int matches = 3, Hashtable countedSquaresGlobal = null)
        {
            bool globalCounter = true;
            List<Item> newList = new List<Item>();
            if (countedSquaresGlobal == null)
            {
                globalCounter = false;
                countedSquaresGlobal = new Hashtable();
            }
            Hashtable countedSquares = new Hashtable();
            countedSquares.Clear();
            if (this.item == null)
                return newList;

            if (separating != FindSeparating.HORIZONTAL)
            {
                countedSquares = this.FindMoreMatches(this.item.color, countedSquares, FindSeparating.VERTICAL, countedSquaresGlobal);
            }

            foreach (DictionaryEntry de in countedSquares)
            {
                LevelManager.THIS.countedSquares.Add(LevelManager.THIS.countedSquares.Count - 1, de.Value);
            }

            if (countedSquares.Count < matches)
                countedSquares.Clear();

            if (separating != FindSeparating.VERTICAL)
            {
                countedSquares = this.FindMoreMatches(this.item.color, countedSquares, FindSeparating.HORIZONTAL, countedSquaresGlobal);
            }

            foreach (DictionaryEntry de in countedSquares)
            {
                LevelManager.THIS.countedSquares.Add(LevelManager.THIS.countedSquares.Count - 1, de.Value);
            }

            if (countedSquares.Count < matches)
                countedSquares.Clear();

            foreach (DictionaryEntry de in countedSquares)
            {
                newList.Add((Item)de.Value);
            }
            // print(countedSquares.Count);
            return newList;
        }

        public void FallInto()
        {
            //if(item == null)
        }

        public void FallOut()
        {
            if (item != null)
            {
                Square nextSquare = GetNeighborBottom();
                if (nextSquare != null)
                {
                    if (nextSquare.IsNone())
                    {
                        for (int i = row + 1; i < LevelManager.THIS.maxRows; i++)
                        {
                            if (LevelManager.THIS.GetSquare(col, i) != null)
                            {
                                if (!LevelManager.THIS.GetSquare(col, i).IsNone())
                                {
                                    nextSquare = LevelManager.THIS.GetSquare(col, i);
                                    break;
                                }
                            }
                        }
                    }
                    if (nextSquare.CanGoInto())
                    {
                        if (nextSquare.item == null)
                        {
                            item.CheckNeedToFall(nextSquare);
                        }
                    }
                }
            }
        }

        public bool IsNone()
        {
            return type == SquareTypes.NONE;
        }

        public bool IsHaveDestroybleObstacle()
        {
            return type == SquareTypes.SOLIDBLOCK || type == SquareTypes.THRIVING;

        }

        public bool CanGoOut()
        {
            return type != SquareTypes.WIREBLOCK;
        }

        public bool CanGoInto()
        {
            return type != SquareTypes.SOLIDBLOCK && type != SquareTypes.UNDESTROYABLE && type != SquareTypes.NONE && type != SquareTypes.THRIVING;
        }

        public void DestroyBlock()
        {
            if (type == SquareTypes.UNDESTROYABLE)
                return;
            if (type != SquareTypes.SOLIDBLOCK && type != SquareTypes.THRIVING)
            {
                List<Square> sqList = GetAllNeghbors();
                foreach (Square sq in sqList)
                {
                    if (sq.type == SquareTypes.SOLIDBLOCK || sq.type == SquareTypes.THRIVING)
                        sq.DestroyBlock();
                }
            }
            if (block.Count > 0)
            {
                var currentBlock = block[block.Count - 1];
                if (type == SquareTypes.WIREBLOCK)
                {
                    LevelManager.THIS.PopupScore(LevelManager.THIS.scoreForWireBlock, transform.position, 0);
                }
                if (type == SquareTypes.SOLIDBLOCK)
                {
                    LevelManager.THIS.PopupScore(LevelManager.THIS.scoreForSolidBlock, transform.position, 0);
                }
                if (type == SquareTypes.THRIVING)
                {
                    LevelManager.THIS.PopupScore(LevelManager.THIS.scoreForThrivingBlock, transform.position, 0);
                    LevelManager.Instance.thrivingBlockDestroyed = true;
                }

                if (type == SquareTypes.BLOCK && LevelManager.Instance.targetObject.Any(i=>i.type==Target.BLOCKS))
                {
                    LevelManager.THIS.CheckCollectedTarget(currentBlock);
                    LevelManager.THIS.PopupScore(LevelManager.THIS.scoreForBlock, transform.position, 0);
                }
                else
                {
                    //Destroy( block[block.Count-1]);
                    if (LevelManager.Instance.targetObject.Any(i => i.blockSprites.Any(x=>x.name == currentBlock.GetComponent<SpriteRenderer>().sprite.name)))
                    {
                        currentBlock.GetComponent<SpriteRenderer>().enabled = false;
                        LevelManager.THIS.CheckCollectedTarget(currentBlock);
                    }
                    else
                    {
                        SoundBase.Instance.PlaySound(SoundBase.Instance.block_destroy);

                        currentBlock.GetComponent<Animation>().Play("BrickRotate");
                        currentBlock.GetComponent<SpriteRenderer>().sortingOrder = 4;
                        currentBlock.AddComponent<Rigidbody2D>();
                        currentBlock.GetComponent<Rigidbody2D>()
                            .AddRelativeForce(new Vector2(Random.insideUnitCircle.x * Random.Range(30, 200), Random.Range(100, 150)), ForceMode2D.Force);
                    }
                }
                GameObject.Destroy(currentBlock, 1.5f);
                if (block.Count > 1)
                    type = SquareTypes.BLOCK;
                block.Remove(currentBlock);

                if (block.Count == 0)
                    type = SquareTypes.EMPTY;
            }

        }

        public void GenThriveBlock(Square newSquare)
        {

        }

        public List<Square> GetAllNeghbors()
        {
            List<Square> sqList = new List<Square>();
            Square nextSquare = null;
            nextSquare = GetNeighborBottom();
            if (nextSquare != null)
                sqList.Add(nextSquare);
            nextSquare = GetNeighborTop();
            if (nextSquare != null)
                sqList.Add(nextSquare);
            nextSquare = GetNeighborLeft();
            if (nextSquare != null)
                sqList.Add(nextSquare);
            nextSquare = GetNeighborRight();
            if (nextSquare != null)
                sqList.Add(nextSquare);
            return sqList;
        }

        public bool IsHaveSolidAbove()
        {
            for (int i = row; i >= 0; i--)
            {
                if (LevelManager.THIS.GetSquare(col, i).type == SquareTypes.SOLIDBLOCK || LevelManager.THIS.GetSquare(col, i).type == SquareTypes.UNDESTROYABLE || LevelManager.THIS.GetSquare(col, i).type == SquareTypes.THRIVING)
                    return true;
            }
            return false;
        }
    }
}