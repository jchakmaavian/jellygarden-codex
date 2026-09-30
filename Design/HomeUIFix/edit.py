from pathlib import Path
p=Path('Assets/ReferenceGameplay/Home/Scripts/ReferenceHomeLayout.cs')
s=p.read_bytes().decode()
s=s.replace('            ReferenceHomeSky.Attach(home);','            ReferenceHomeSky.Attach(home);\r\n            if(home.logo)home.logo.gameObject.SetActive(false);')
s=s.replace('            Pair(home.resourceButtons[0].transform,16613,0,0,1,1,true);portrait.transform.SetAsLastSibling();Bounds(portrait.rectTransform,.08f,.08f,.92f,.92f);portrait.preserveAspect=true;', '''            // The bust is framed inside the aperture; gold trim is drawn over the portrait.
            var portraitBack=Pic(home.resourceButtons[0].transform,16613,.075f,.085f,.925f,.95f);
            portraitBack.name="Portrait blue inset";portraitBack.sprite=null;portraitBack.color=new Color(.065f,.23f,.52f);portraitBack.transform.SetAsFirstSibling();
            portrait.transform.SetAsLastSibling();Bounds(portrait.rectTransform,.13f,.12f,.87f,.91f);portrait.preserveAspect=true;
            Pair(home.resourceButtons[0].transform,16613,0,0,1,1,true);'''.replace('\n','\r\n'))
s=s.replace('Pair(home.levelButton.transform,16521,0,0,1,1,true);Pair(home.levelButton.transform,21955,.045f,.105f,.955f,.94f,true);','ActionFace(home.levelButton.transform,21955);')
s=s.replace('Pair(home.areaButton.transform,16521,0,0,1,1,true);Pair(home.areaButton.transform,17031,.045f,.105f,.955f,.94f,true);','ActionFace(home.areaButton.transform,17031);')
s=s.replace('        void Build(){', '''        static void ActionFace(Transform parent,int face){
            // Overlap the inset beneath the trim so packed sprite edges never leave a gap.
            Pair(parent,face,.016f,.045f,.984f,.98f,true);
            Pair(parent,16521,0,0,1,1,true);
        }
        void Build(){'''.replace('\n','\r\n'))
p.write_bytes(s.encode())
