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
using UnityEngine;

namespace JellyGarden.Scripts
{
	public enum CombineType
	{
		LShape,
		VShape
	}

	public class AI : MonoBehaviour
	{
		/// <summary>
		/// The reference to this object
		/// </summary>
		public static AI THIS;
		/// <summary>
		/// have got a tip
		/// </summary>
		public bool gotTip;
		/// <summary>
		/// The allow show tip
		/// </summary>
		public bool allowShowTip;
		/// <summary>
		/// The tip identifier
		/// </summary>
		int tipID;
		/// <summary>
		/// The count of coroutines
		/// </summary>
		public int corCount;
		/// <summary>
		/// The tip items
		/// </summary>
		private List<Item> nextMoveItems;
		readonly List<Item> visibleHintItems = new List<Item>();
		Coroutine hintRoutine;
		int visibleHintMove;
		Juice.RoyalHintPresenter hintPresenter;
		public int VisibleHintCount => hintPresenter && hintPresenter.IsVisible ? hintPresenter.VisibleSwapCount : 0;
		// The hint always shows the best move (2026-10-04): it appears once the board has been still for a second and stays
		// until the player touches the board or the board changes.
		public const float HintIdleSeconds = 1f;
		void OnEnable() { Juice.RoyalHintSettings.Changed += HintPreferenceChanged; }
		void OnDisable()
		{
			Juice.RoyalHintSettings.Changed -= HintPreferenceChanged;
			CancelVisibleHint();
			if (hintRoutine != null) StopCoroutine(hintRoutine);
			hintRoutine = null; corCount = 0;
		}
		void HintPreferenceChanged(bool enabled) { if (!enabled) CancelVisibleHint(); }
		void Update()
		{
			var level = LevelManager.THIS;
			if (visibleHintItems.Count > 0 && (!Juice.RoyalHintSettings.Enabled || !level ||
				level.gameStatus != GameState.Playing || level.DragBlocked || level.moveID != visibleHintMove ||
				InputManager.Instance.IsTouching || !HintCandidatesPresent(visibleHintItems))) CancelVisibleHint();
		}
		public void CancelVisibleHint()
		{
			if (hintPresenter) hintPresenter.Hide();
			foreach (var item in visibleHintItems)
			{
				if (!item || !item.anim) continue;
				item.anim.ResetTrigger("tip");
				var state = item.anim.GetCurrentAnimatorStateInfo(0);
				bool nextTip = item.anim.IsInTransition(0) && item.anim.GetNextAnimatorStateInfo(0).IsName("tip");
				if (!item.destroying && !item.falling && (state.IsName("tip") || nextTip))
				{
					item.anim.Play("stop", 0, 1f);
					if (item.sprRenderer) item.sprRenderer.transform.localPosition = Vector3.zero;
				}
			}
			visibleHintItems.Clear();
		}
		// Use this for initialization
		void Start()
		{
			THIS = this;
		}

		public Vector3 vDirection;
		public CombineType combineType;
		public Item tipItem;

		/// <summary>
		/// Gets the square. Return square by row and column
		/// </summary>
		/// <param name="row">The row.</param>
		/// <param name="col">The column.</param>
		/// <returns></returns>
		Square GetSquare(int row, int col)
		{
			return LevelManager.THIS.GetSquare(col, row);
		}

		/// <summary>
		/// Checks the square. Is the color of item of this square is equal to desired color. If so we add the item to nextMoveItems array.
		/// </summary>
		/// <param name="square">The square.</param>
		/// <param name="COLOR">The color.</param>
		/// <param name="moveThis">is the item should be movable?</param>
		void CheckSquare(Square square, int COLOR, bool moveThis = false)
		{
			if (square == null)
				return;
			if (RoyalThrone.Campaign.RoyalCampaignBlockers.IsSpecialItem(square.item)) return;
			if (square.item != null)
			{
				if (square.item.color == COLOR)
				{
					if (moveThis && square.type != SquareTypes.WIREBLOCK)
					{
						nextMoveItems.Add(square.item);
					}
					else if (!moveThis)
						nextMoveItems.Add(square.item);
				}
			}

		}

		public List<Item> GetCombine()
		{
			// A level restart can retire these pieces before the next solver pass.
			// Tutorial consumers must wait for a live candidate from the current board.
			return nextMoveItems != null && HintCandidatesPresent(nextMoveItems) ? nextMoveItems : null;
		}

		/// <summary>
		/// Loop of searching possible combines
		/// </summary>
		/// <returns></returns>
		public IEnumerator CheckPossibleCombines()
		{
			//waiting for 1 second just in case to be sure that field was built
			yield return new WaitForSeconds(1);

			//allow to show tips
			allowShowTip = true;

			//variable to check: are we got tip or not
			gotTip = false;

			//break, if the main scripts have not ready yet
			while (LevelManager.THIS == null)
			{
				yield return new WaitForEndOfFrame();
			}
			//if game is not in Playing status - wait
			// Input can remain responsive during combos. DragBlocked alone no longer
			// means the board is ready for hints or a destructive no-moves shuffle.
			while (LevelManager.THIS.gameStatus != GameState.Playing || BoardSettling(LevelManager.THIS))
			{
				yield return null;
			}
			int maxRow = LevelManager.THIS.maxRows;
			int maxCol = LevelManager.THIS.maxCols;

			//if drag have not blocked and game status Playing - continue
			if (!LevelManager.THIS.DragBlocked && LevelManager.THIS.gameStatus == GameState.Playing)
			{
				nextMoveItems = new List<Item>();

				if (LevelManager.THIS.gameStatus != GameState.Playing)
					yield break;

				// The hint solver decides (2026-10-04): the best colour match is the tip; with no colour match the board
				// shuffles (LevelManager.NoMatches -> RoyalShuffle), even when power-ups could be tapped. The legacy pattern
				// scan below is kept for reference but no longer runs.
				var royalPlan = Juice.RoyalHintSolver.Find(LevelManager.THIS);
				bool royalDecided = true;
				if (royalPlan != null)
				{
					nextMoveItems.Add(royalPlan.First); if (royalPlan.Second) nextMoveItems.Add(royalPlan.Second);
					tipItem = royalPlan.First; showTip(nextMoveItems); yield break;
				}

				// Get item component from the prefab
				Item it = LevelManager.THIS.itemPrefab.GetComponent<Item>();

				//Iteration for search possible combination
				if (!royalDecided)
				for (int COLOR = 0; COLOR < it.items.Length; COLOR++)
				{
					for (int col = 0; col < LevelManager.THIS.maxCols; col++)
					{
						for (int row = 0; row < LevelManager.THIS.maxRows; row++)
						{
							Square square = LevelManager.THIS.GetSquare(col, row);
							if (square.type == SquareTypes.WIREBLOCK || square.item == null || RoyalThrone.Campaign.RoyalCampaignBlockers.IsSpecialItem(square.item))
								continue;
							//current square called x
							//o-o-x
							//	  o
							vDirection = Vector3.zero;
							combineType = CombineType.LShape;
							if (col > 1 && row < maxRow - 1)
							{
								CheckSquare(GetSquare(row + 1, col), COLOR, true);
								CheckSquare(GetSquare(row, col - 1), COLOR);
								CheckSquare(GetSquare(row, col - 2), COLOR);
							}
							if (nextMoveItems.Count == 3 && GetSquare(row, col).CanGoInto())
							{
								// StartCoroutine(showTip(nextMoveItems[0], Vector3.up));
								showTip(nextMoveItems);
								tipItem = nextMoveItems[0];
								vDirection = Vector3.up;
								yield break;
							}
							else
								nextMoveItems.Clear();

							//    o
							//o-o x
							if (col > 1 && row > 0)
							{
								CheckSquare(GetSquare(row - 1, col), COLOR, true);
								CheckSquare(GetSquare(row, col - 1), COLOR);
								CheckSquare(GetSquare(row, col - 2), COLOR);
							}
							if (nextMoveItems.Count == 3 && GetSquare(row, col).CanGoInto())
							{
								// StartCoroutine(showTip(nextMoveItems[0], Vector3.down));
								vDirection = Vector3.down;
								tipItem = nextMoveItems[0];
								showTip(nextMoveItems);
								yield break;
							}
							else
								nextMoveItems.Clear();

							//x o o
							//o
							if (col < maxCol - 2 && row < maxRow - 1)
							{
								CheckSquare(GetSquare(row + 1, col), COLOR, true);
								CheckSquare(GetSquare(row, col + 1), COLOR);
								CheckSquare(GetSquare(row, col + 2), COLOR);
							}
							if (nextMoveItems.Count == 3 && GetSquare(row, col).CanGoInto())
							{
								// StartCoroutine(showTip(nextMoveItems[0], Vector3.up));
								vDirection = Vector3.up;
								tipItem = nextMoveItems[0];
								showTip(nextMoveItems);
								yield break;
							}
							else
								nextMoveItems.Clear();

							//o
							//x o o
							if (col < maxCol - 2 && row > 0)
							{
								CheckSquare(GetSquare(row - 1, col), COLOR, true);
								CheckSquare(GetSquare(row, col + 1), COLOR);
								CheckSquare(GetSquare(row, col + 2), COLOR);
							}
							if (nextMoveItems.Count == 3 && GetSquare(row, col).CanGoInto())
							{
								//  StartCoroutine(showTip(nextMoveItems[0], Vector3.down));
								vDirection = Vector3.down;
								tipItem = nextMoveItems[0];
								showTip(nextMoveItems);
								yield break;
							}
							else
								nextMoveItems.Clear();

							//o
							//o
							//x o
							if (col < maxCol - 1 && row > 1)
							{
								CheckSquare(GetSquare(row, col + 1), COLOR, true);
								CheckSquare(GetSquare(row - 1, col), COLOR);
								CheckSquare(GetSquare(row - 2, col), COLOR);
							}
							if (nextMoveItems.Count == 3 && GetSquare(row, col).CanGoInto())
							{
								// StartCoroutine(showTip(nextMoveItems[0], Vector3.left));
								vDirection = Vector3.left;
								tipItem = nextMoveItems[0];
								showTip(nextMoveItems);
								yield break;
							}
							else
								nextMoveItems.Clear();

							//x o
							//o
							//o
							if (col < maxCol - 1 && row < maxRow - 2)
							{
								CheckSquare(GetSquare(row, col + 1), COLOR, true);
								CheckSquare(GetSquare(row + 1, col), COLOR);
								CheckSquare(GetSquare(row + 2, col), COLOR);
							}
							if (nextMoveItems.Count == 3 && GetSquare(row, col).CanGoInto())
							{
								//  StartCoroutine(showTip(nextMoveItems[0], Vector3.left));
								vDirection = Vector3.left;
								tipItem = nextMoveItems[0];
								showTip(nextMoveItems);
								yield break;
							}
							else
								nextMoveItems.Clear();

							//	o
							//  o
							//o x
							if (col > 0 && row > 1)
							{
								CheckSquare(GetSquare(row, col - 1), COLOR, true);
								CheckSquare(GetSquare(row - 1, col), COLOR);
								CheckSquare(GetSquare(row - 2, col), COLOR);
							}
							if (nextMoveItems.Count == 3 && GetSquare(row, col).CanGoInto())
							{
								//  StartCoroutine(showTip(nextMoveItems[0], Vector3.right));
								vDirection = Vector3.right;
								tipItem = nextMoveItems[0];
								showTip(nextMoveItems);
								yield break;
							}
							else
								nextMoveItems.Clear();

							//o x
							//  o
							//  o
							if (col > 0 && row < maxRow - 2)
							{
								CheckSquare(GetSquare(row, col - 1), COLOR, true);
								CheckSquare(GetSquare(row + 1, col), COLOR);
								CheckSquare(GetSquare(row + 2, col), COLOR);
							}
							if (nextMoveItems.Count == 3 && GetSquare(row, col).CanGoInto())
							{
								//  StartCoroutine(showTip(nextMoveItems[0], Vector3.right));
								vDirection = Vector3.right;
								tipItem = nextMoveItems[0];
								showTip(nextMoveItems);
								yield break;
							}
							else
								nextMoveItems.Clear();

							//o-x-o-o
							if (col < maxCol - 2 && col > 0)
							{
								CheckSquare(GetSquare(row, col - 1), COLOR, true);
								CheckSquare(GetSquare(row, col + 1), COLOR);
								CheckSquare(GetSquare(row, col + 2), COLOR);
							}
							if (nextMoveItems.Count == 3 && GetSquare(row, col).CanGoInto())
							{
								//   StartCoroutine(showTip(nextMoveItems[0], Vector3.right));
								vDirection = Vector3.right;
								tipItem = nextMoveItems[0];
								showTip(nextMoveItems);
								yield break;
							}
							else
								nextMoveItems.Clear();
							//o-o-x-o
							if (col < maxCol - 1 && col > 1)
							{
								CheckSquare(GetSquare(row, col + 1), COLOR, true);
								CheckSquare(GetSquare(row, col - 1), COLOR);
								CheckSquare(GetSquare(row, col - 2), COLOR);
							}
							if (nextMoveItems.Count == 3 && GetSquare(row, col).CanGoInto())
							{
								//   StartCoroutine(showTip(nextMoveItems[0], Vector3.left));
								vDirection = Vector3.left;
								tipItem = nextMoveItems[0];
								showTip(nextMoveItems);
								yield break;
							}
							else
								nextMoveItems.Clear();
							//o
							//x
							//o
							//o
							if (row < maxRow - 2 && row > 0)
							{
								CheckSquare(GetSquare(row - 1, col), COLOR, true);
								CheckSquare(GetSquare(row + 1, col), COLOR);
								CheckSquare(GetSquare(row + 2, col), COLOR);
							}
							if (nextMoveItems.Count == 3 && GetSquare(row, col).CanGoInto())
							{
								//  StartCoroutine(showTip(nextMoveItems[0], Vector3.down));
								vDirection = Vector3.down;
								tipItem = nextMoveItems[0];
								showTip(nextMoveItems);
								yield break;
							}
							else
								nextMoveItems.Clear();

							//o
							//o
							//x
							//o
							if (row < maxRow - 2 && row > 1)
							{
								CheckSquare(GetSquare(row + 1, col), COLOR, true);
								CheckSquare(GetSquare(row - 1, col), COLOR);
								CheckSquare(GetSquare(row - 2, col), COLOR);
							}
							if (nextMoveItems.Count == 3 && GetSquare(row, col).CanGoInto())
							{
								//   StartCoroutine(showTip(nextMoveItems[0], Vector3.up));
								vDirection = Vector3.up;
								tipItem = nextMoveItems[0];
								showTip(nextMoveItems);
								yield break;
							}
							else
								nextMoveItems.Clear();

							//  o
							//o x o
							//  o
							int h = 0;
							int v = 0;
							combineType = CombineType.VShape;

							if (row < maxRow - 1)
							{
								square = GetSquare(row + 1, col);
								if (square)
								{//1.6
									if (square.item != null && !RoyalThrone.Campaign.RoyalCampaignBlockers.IsSpecialItem(square.item))
									{
										if (square.item.color == COLOR)
										{
											//vDirection = Vector3.up;
											nextMoveItems.Add(square.item);
											v++;
										}
									}
								}
							}
							if (row > 0)
							{
								square = GetSquare(row - 1, col);
								if (square)
								{//1.6
									if (square.item != null && !RoyalThrone.Campaign.RoyalCampaignBlockers.IsSpecialItem(square.item))
									{
										if (square.item.color == COLOR)
										{
											//vDirection = Vector3.down;
											nextMoveItems.Add(square.item);
											v++;
										}
									}
								}
							}
							if (col > 0)
							{
								square = GetSquare(row, col - 1);
								if (square)
								{//1.6
									if (square.item != null && !RoyalThrone.Campaign.RoyalCampaignBlockers.IsSpecialItem(square.item))
									{
										if (square.item.color == COLOR)
										{
											//vDirection = Vector3.right;
											nextMoveItems.Add(square.item);
											h++;
										}
									}
								}
							}
							if (col < maxCol - 1)
							{
								square = GetSquare(row, col + 1);
								if (square)
								{//1.6
									if (square.item != null && !RoyalThrone.Campaign.RoyalCampaignBlockers.IsSpecialItem(square.item))
									{
										if (square.item.color == COLOR)
										{
											//vDirection = Vector3.left;
											nextMoveItems.Add(square.item);
											h++;
										}
									}
								}
							}

							//if we found 3or more items and they not lock show tip
							if (nextMoveItems.Count >= 3 && GetSquare(row, col).CanGoInto() && GetSquare(row, col).type != SquareTypes.WIREBLOCK)
							{
								if (v >= h && nextMoveItems[2].square.type != SquareTypes.WIREBLOCK)
								{ //StartCoroutine(showTip(nextMoveItems[2], new Vector3(Random.Range(-1f, 1f), 0, 0)));
									tipItem = nextMoveItems[2];
									if (tipItem.transform.position.x > nextMoveItems[0].transform.position.x)
										vDirection = Vector3.left;
									else
										vDirection = Vector3.right;
									showTip(nextMoveItems);
									yield break;

								}
								else if (v <= h && nextMoveItems[0].square.type != SquareTypes.WIREBLOCK)
								{ // StartCoroutine(showTip(nextMoveItems[0], new Vector3(0, Random.Range(-1f, 1f), 0)));
									tipItem = nextMoveItems[0];
									if (tipItem.transform.position.y > nextMoveItems[0].transform.position.y)
										vDirection = Vector3.down;
									else
										vDirection = Vector3.up;

									showTip(nextMoveItems);
									yield break;

								}
								else
									nextMoveItems.Clear();
							}
							else
								nextMoveItems.Clear();

						}
					}


				}
				if (!royalDecided && Juice.RoyalEffectsProfile.Get)
				{
					var royalHint = Juice.RoyalPatterns.ExtraHint();
					if (royalHint != null) { nextMoveItems = royalHint; tipItem = royalHint[0]; showTip(royalHint); yield break; }
				}
				//if we don't get any tip.  call nomatches to regenerate level
				if (!LevelManager.THIS.DragBlocked)
				{
					if (!gotTip)
						LevelManager.THIS.NoMatches();
				}

			}
			yield return new WaitForEndOfFrame();
			//find possible combination again
			if (!LevelManager.THIS.DragBlocked)
				StartCoroutine(CheckPossibleCombines());

			// }
		}

		static bool BoardSettling(LevelManager level)
		{
			if (level.IsResolvingMatches || level.InputSwaps > 0 || Juice.RoyalPowerupDirector.Busy) return true;
			foreach (var item in level.GetItems())
				if (item && (item.falling || item.destroying)) return true;
			return false;
		}

		//show tip function calls coroutine for
		void showTip(List<Item> nextMoveItems)
		{
			// Discovery must remain true even when visual hints are disabled: otherwise
			// the no-moves solver would incorrectly shuffle a perfectly playable board.
			gotTip = true;
			// A hint already showing for this board stays: several finders start at once (entering Playing, the level start,
			// the end of a cascade) and each used to cancel and restart it, so it blinked.
			if (hintRoutine != null && hintPresenter && hintPresenter.IsVisible && LevelManager.THIS && visibleHintMove == LevelManager.THIS.moveID) return;
			if (hintRoutine != null) StopCoroutine(hintRoutine);
			CancelVisibleHint();
			hintRoutine = StartCoroutine(showTipCor(new List<Item>(nextMoveItems)));
		}

		static bool HintCandidatesPresent(List<Item> items)
		{
			foreach (var item in items)
				if (!item || item.destroying || !item.gameObject.activeInHierarchy || !item.square || item.square.item != item) return false;
			return items.Count > 0;
		}

		//show tip coroutine
		IEnumerator showTipCor(List<Item> nextMoveItems)
		{
			corCount = 1;
			tipID = LevelManager.THIS.moveID;
			float idle = 0;
			while (idle < HintIdleSeconds)
			{
				var level = LevelManager.THIS;
				if (!level || level.moveID != tipID || !HintCandidatesPresent(nextMoveItems)) break;
				bool settled = level.gameStatus == GameState.Playing && !level.DragBlocked && allowShowTip && !InputManager.Instance.IsTouching;
				foreach (var item in nextMoveItems) if (!item || item.destroying || item.falling) settled = false;
				idle = settled ? idle + Time.deltaTime : 0;
				yield return null;
			}
			var lm = LevelManager.THIS;
			if (lm && lm.moveID == tipID && lm.gameStatus == GameState.Playing && !lm.DragBlocked &&
				allowShowTip && Juice.RoyalHintSettings.Enabled && idle >= HintIdleSeconds &&
				!InputManager.Instance.IsTouching && HintCandidatesPresent(nextMoveItems))
			{
				// The legacy discovery sample omits the swap partner and longer runs.
				// Preview the complete result without changing real cells or match state.
				var plan = Juice.RoyalHintSolver.Find(lm);
				if (!hintPresenter) hintPresenter = GetComponent<Juice.RoyalHintPresenter>() ?? gameObject.AddComponent<Juice.RoyalHintPresenter>();
				if (hintPresenter.Show(plan))
				{
					visibleHintMove = tipID;
					visibleHintItems.Add(plan.First);
					if (plan.Second) visibleHintItems.Add(plan.Second);
					while (hintPresenter.IsVisible) yield return null;
				}
			}
			CancelVisibleHint();
			corCount = 0; hintRoutine = null;
			if (lm && lm.gameStatus == GameState.Playing) StartCoroutine(CheckPossibleCombines());
		}


	}
}
