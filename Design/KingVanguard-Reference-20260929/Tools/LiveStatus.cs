var qa=RoyalThrone.Testing.RoyalQaService.Instance;
var kings=UnityEngine.Object.FindObjectsByType<RoyalThrone.Characters.KingVanguardCharacter>(UnityEngine.FindObjectsInactive.Include,UnityEngine.FindObjectsSortMode.None);
var text="playing="+UnityEditor.EditorApplication.isPlaying+" scene="+UnityEngine.SceneManagement.SceneManager.GetActiveScene().name+" qa="+(qa!=null?qa.Ready+" "+qa.Status:"missing")+"\n";
foreach(var k in kings) text+=k.name+" active="+k.gameObject.activeInHierarchy+" clip="+k.CurrentAnimation+" activity="+k.CurrentActivity+"\n";
return text;
