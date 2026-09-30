from pathlib import Path
p=Path('Assets/ReferenceGameplay/Editor/ReferenceGameplayChecks.cs');s=p.read_bytes().decode();needle='                    case "home-sky-fresh-play":';add='''                    case "tnt-sun-bake":
                        TNTSunArt.Bake();
                        break;
                    case "tnt-sun-checks":
#if !ROYAL_DISABLE_QA
                        RoyalThrone.Editor.RoyalRegressionRunner.Run(TNTSunArt.Run());
#endif
                        break;
'''.replace('\n','\r\n');assert needle in s;p.write_bytes(s.replace(needle,add+needle).encode())
p=Path('Assets/JellyGarden/Scripts/Juice/RoyalPieceSkin.cs');s=p.read_bytes().decode();needle='        public static Sprite CampaignArt(string name)\r\n        {';assert needle in s;s=s.replace(needle,needle+'\r\n            // TNT artwork is shared by its animated board rig, activation sprite and all menu icons.\r\n            if(name=="tnt"){var sun=RoyalThrone.ReferenceGameplay.ReferenceGameplayCatalog.Art("tnt");if(sun)return sun;}');p.write_bytes(s.encode())
