#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RoyalThrone.Editor
{
    // Editor-only host: game logic may stop its own coroutines during a scenario.
    // Flatten child enumerators so a failed assertion still executes every finally.
    public sealed class RoyalRegressionRunner : MonoBehaviour
    {
        readonly Stack<IEnumerator> stack = new Stack<IEnumerator>();
        public static void Run(IEnumerator suite)
        {
            var go = new GameObject("Royal regression runner - editor only");
            go.hideFlags = HideFlags.HideAndDontSave;
            var host = go.AddComponent<RoyalRegressionRunner>();
            host.stack.Push(suite);host.StartCoroutine(host.Execute());
        }
        IEnumerator Execute()
        {
            try
            {
                while(stack.Count > 0)
                {
                    var current = stack.Peek();bool next = false;Exception failure = null;
                    try { next = current.MoveNext(); } catch(Exception ex) { failure = ex; }
                    if(failure != null) { Debug.LogException(failure);yield break; }
                    if(!next) { stack.Pop();(current as IDisposable)?.Dispose();continue; }
                    if(current.Current is IEnumerator child && !(child is CustomYieldInstruction)) stack.Push(child);
                    else yield return current.Current;
                }
            }
            finally { Drain();Destroy(gameObject); }
        }
        void Drain()
        {
            while(stack.Count > 0)
            {
                var iterator = stack.Pop();
                try { (iterator as IDisposable)?.Dispose(); } catch(Exception ex) { Debug.LogException(ex); }
            }
        }
        void OnDisable() { Drain(); }
    }
}
#endif
