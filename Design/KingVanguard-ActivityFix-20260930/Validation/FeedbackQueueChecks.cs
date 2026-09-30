using System;
using RoyalThrone.Characters;
class Program {
static void Main(){ CheckFeedbackQueue((ok,message)=>{if(!ok)throw new Exception("FAIL "+message);Console.WriteLine("PASS "+message);});}
        static void CheckFeedbackQueue(Action<bool,string> check)
        {
            var queue=new KingVanguardFeedbackQueue();
            queue.Offer(KingVanguardFeedbackQueue.Feedback.Match,0);
            check(queue.TryGet(.1f,out var feedback)&&feedback==KingVanguardFeedbackQueue.Feedback.Match,
                "match waits without being consumed while actor is busy");
            queue.Offer(KingVanguardFeedbackQueue.Feedback.Powerup,.2f);
            queue.Offer(KingVanguardFeedbackQueue.Feedback.Match,.3f);
            check(queue.TryGet(.4f,out feedback)&&feedback==KingVanguardFeedbackQueue.Feedback.Powerup,
                "stronger feedback replaces one pending match and cannot be downgraded");
            queue.Accepted(.4f);
            queue.Offer(KingVanguardFeedbackQueue.Feedback.Match,.5f);
            check(!queue.TryGet(1,out feedback),"accepted power-up applies cooldown");
            check(!queue.TryGet(4.5f,out feedback),"expired feedback is not replayed after a long animation");
            queue.Clear();queue.Offer(KingVanguardFeedbackQueue.Feedback.Match,10);queue.Accepted(10);
            queue.Offer(KingVanguardFeedbackQueue.Feedback.Powerup,10.2f);
            check(!queue.TryGet(11.9f,out feedback)&&queue.TryGet(12,out feedback)&&feedback==KingVanguardFeedbackQueue.Feedback.Powerup,
                "fresh stronger feedback survives short acknowledgement cooldown");
            queue.Clear();queue.Offer(KingVanguardFeedbackQueue.Feedback.Powerup,20);
            queue.Offer(KingVanguardFeedbackQueue.Feedback.Match,22.9f);
            check(!queue.TryGet(23.01f,out feedback),"weaker matches cannot keep an old strong reaction alive");
            queue.Clear();queue.Offer(KingVanguardFeedbackQueue.Feedback.Powerup,30);
            check(queue.TryGet(30.4f,out feedback),"a slow frame cannot decay a fresh power-up event below its threshold");
            queue.Accepted(30.4f);queue.Clear();queue.Offer(KingVanguardFeedbackQueue.Feedback.Match,30.5f);
            check(queue.TryGet(30.5f,out feedback)&&feedback==KingVanguardFeedbackQueue.Feedback.Match,
                "new visible board clears old pending feedback and cooldown");
        }
}
