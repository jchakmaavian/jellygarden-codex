using UnityEngine;
namespace JellyGarden.Scripts.Juice
{
    [CreateAssetMenu(menuName="Royal Throne/Effects profile")]
    public sealed class RoyalEffectsProfile : ScriptableObject
    {
        [Header("Launch and impact — seconds")]
        [Range(.03f,.3f)] public float rocketAnticipation=.12f;
        [Range(8,50)] public float rocketCellsPerSecond=15;
        [Range(.05f,.5f)] public float tntAnticipation=.16f;
        [Range(.005f,.10f)] public float blastWavePerCell=.025f;
        [Range(.1f,.6f)] public float helicopterLift=.22f;
        [Range(.2f,1)] public float helicopterFlight=.48f;
        [Header("Readability and performance")]
        [Range(.1f,.7f)] public float smokeLifetime=.36f;
        [Range(8,64)] public int projectilePool=32;
        [Range(100,1000)] public int sparkleBudget=480;
        [Range(0,.12f)] public float impactShake=.035f;
        [Header("Sound balance")]
        [Range(0,1)] public float musicVolume=.32f;
        [Range(0,1)] public float effectsVolume=.68f;
        [Range(4,24)] public int soundVoices=12;
        [Range(.2f,2)] public float musicCrossfade=1.2f;
        static RoyalEffectsProfile instance;
        public static RoyalEffectsProfile Get => instance ? instance : instance = Resources.Load<RoyalEffectsProfile>("RoyalGameplay/EffectsProfile");
    }
}
