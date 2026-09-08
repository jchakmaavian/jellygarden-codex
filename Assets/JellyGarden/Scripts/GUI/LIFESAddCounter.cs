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
using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.GUI
{
    public class LIFESAddCounter : MonoBehaviour
    {
        Text text;
        static float TimeLeft;
        float TotalTimeForRestLife = 15f * 60;  //8 minutes for restore life
        bool startTimer;
        DateTime templateTime;
        // Use this for initialization
        void Start()
        {
            text = GetComponent<Text>();
            TotalTimeForRestLife = InitScript.Instance.TotalTimeForRestLifeHours * 60 * 60 + InitScript.Instance.TotalTimeForRestLifeMin * 60 + InitScript.Instance.TotalTimeForRestLifeSec;
            //if (TotalTimeForRestLife != InitScript.RestLifeTimer) //1.4
            //{
            //    print("reset!!!!!" + TotalTimeForRestLife);
            //    InitScript.RestLifeTimer = TotalTimeForRestLife;
            //}
        }

        bool CheckPassedTime()
        {
            //print(InitScript.DateOfExit);
            if (InitScript.DateOfExit == "" || InitScript.DateOfExit == default(DateTime).ToString())
                InitScript.DateOfExit = DateTime.Now.ToString();

            DateTime dateOfExit = DateTime.Parse(InitScript.DateOfExit);
            if (DateTime.Now.Subtract(dateOfExit).TotalSeconds > TotalTimeForRestLife * (InitScript.Instance.CapOfLife - InitScript.lifes))
            {
                //Debug.Log(dateOfExit + " " + InitScript.today);
                InitScript.Instance.RestoreLifes();
                InitScript.RestLifeTimer = 0;
                return false;    ///we dont need lifes
            }
            else
            {
                TimeCount((float)DateTime.Now.Subtract(dateOfExit).TotalSeconds);
                //Debug.Log((float)DateTime.Now.Subtract(dateOfExit).TotalSeconds + " " + dateOfExit + " " + DateTime.Now);
                return true;     ///we need lifes
            }
        }

        void TimeCount(float tick)
        {
            if (InitScript.RestLifeTimer <= 0)
                ResetTimer();

            //print(InitScript.RestLifeTimer + " " + tick);
            InitScript.RestLifeTimer -= tick;
            //print(InitScript.RestLifeTimer + " " + tick);
            int hours = Mathf.FloorToInt(InitScript.RestLifeTimer / 3600);
            int minutes = Mathf.FloorToInt((InitScript.RestLifeTimer - hours * 3600) / 60);
            int seconds = Mathf.FloorToInt((InitScript.RestLifeTimer - hours * 3600) - minutes * 60);
            //print(hours + " :" + minutes + " :" + seconds);
            if (InitScript.RestLifeTimer <= 1 && InitScript.lifes < InitScript.Instance.CapOfLife)
            {
                InitScript.Instance.AddLife(1);
                ResetTimer();
            }
            //		}
        }

        void ResetTimer()
        {
            InitScript.RestLifeTimer = TotalTimeForRestLife;
        }

        // Update is called once per frame
        void Update()
        {
            if (!startTimer && DateTime.Now.Subtract(DateTime.Now).Days == 0)
            {
                InitScript.DateOfRestLife = DateTime.Now;
                if (InitScript.lifes < InitScript.Instance.CapOfLife)
                {
                    if (CheckPassedTime())
                        startTimer = true;
                    //	StartCoroutine(TimeCount());
                }
            }

            if (startTimer)
                TimeCount(Time.deltaTime);

            if (gameObject.activeSelf)
            {
                if (InitScript.lifes < InitScript.Instance.CapOfLife)
                {
                    if (InitScript.Instance.TotalTimeForRestLifeHours > 0)
                    {
                        int hours = Mathf.FloorToInt(InitScript.RestLifeTimer / 3600);
                        int minutes = Mathf.FloorToInt((InitScript.RestLifeTimer - hours * 3600) / 60);
                        int seconds = Mathf.FloorToInt((InitScript.RestLifeTimer - hours * 3600) - minutes * 60);

                        text.enabled = true;
                        text.text = "" + string.Format("{0:00}:{1:00}:{2:00}", hours, minutes, seconds);
                    }
                    else
                    {
                        int minutes = Mathf.FloorToInt(InitScript.RestLifeTimer / 60F);
                        int seconds = Mathf.FloorToInt(InitScript.RestLifeTimer - minutes * 60);

                        text.enabled = true;
                        text.text = "" + string.Format("{0:00}:{1:00}", minutes, seconds);

                    }
                    InitScript.timeForReps = text.text;
                    //				//	text.text = "+1 in \n " + Mathf.FloorToInt( MainMenu.RestLifeTimer/60f) + ":" + Mathf.RoundToInt( (MainMenu.RestLifeTimer/60f - Mathf.FloorToInt( MainMenu.RestLifeTimer/60f))*60f);
                }
                else
                {
                    text.text = "   Full";
                }
            }
        }

        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                //	StopCoroutine("TimeCount");
                InitScript.DateOfExit = DateTime.Now.ToString();
                //print(InitScript.DateOfExit);

                //			PlayerPrefs.SetString("DateOfExit",DateTime.Now.ToString());
                //			PlayerPrefs.Save();
            }
            else
            {
                startTimer = false;
                //MainMenu.today = DateTime.Now; 
                //		MainMenu.DateOfExit = PlayerPrefs.GetString("DateOfExit");
            }
        }

        void OnEnable()
        {
            startTimer = false;
        }

        //void OnDisable()  //1.4    
        //{
        //    InitScript.DateOfExit = DateTime.Now.ToString();
        //    //print(InitScript.DateOfExit);

        //}


        void OnApplicationQuit()  //1.4  
        {
            InitScript.DateOfExit = DateTime.Now.ToString();
            //print(InitScript.DateOfExit);

        }
    }
}
