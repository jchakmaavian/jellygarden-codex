using UnityEngine;
namespace RoyalThrone.Boosters.ColorBall.V5 {
 public enum ColorBallPresentationProfile { ReferenceMatch, Enhanced }
 // Presentation only: neither profile controls the physical pose or sequence clock.
 public sealed class ColorBallPresentationSettings : ScriptableObject {
  [Range(0,1)] public float detail;
  [Range(0,2)] public float aura=1,discharge=1,contact=1,release=1;
 }
}
