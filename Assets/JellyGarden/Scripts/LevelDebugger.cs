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
using System.IO;
using UnityEngine;

//using UnityEditor;

namespace JellyGarden.Scripts
{
    public class LevelDebugger {

        public static void SaveMap(int[] items, int maxCols, int maxRows) {
            string saveString = "";
            string filename = "levelstate.txt";


            //set map data
            for (int row = 0; row < maxRows; row++) {
                for (int col = 0; col < maxCols; col++) {
                    saveString += (int)items[row * maxCols + col];
                    saveString += " ";
                }
            }
            //Write to file
            string activeDir = Application.dataPath + @"/JellyGarden/Resources/";
            string newPath = global::System.IO.Path.Combine(activeDir, filename + ".txt");
            StreamWriter sw = new StreamWriter(newPath);
            sw.Write(saveString);
            sw.Close();
            //AssetDatabase.Refresh();

        }

        public static int[] LoadMap(int maxCols, int maxRows) {
            string filename = "levelstate.txt";
            int[] items = new int[99];
            int row = 0;
            TextAsset mapText = Resources.Load(filename) as TextAsset;
            string filetext = mapText.text;
            string[] lines = filetext.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines) {
                string[] st = line.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < st.Length; i++) {
                    items[row * maxCols + i] = int.Parse(st[i].ToString());
                }
                row++;
            }
            return items;

        }
    }
}
