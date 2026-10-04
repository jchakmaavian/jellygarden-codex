using System;
using System.Collections.Generic;
using Spine;
using Spine.Unity;
using Spine.Unity.AttachmentTools;
using UnityEngine;

namespace RoyalThrone.Characters
{
    // The original pointed crown polygon clips the approved five-finial silhouette.
    // Rebind a complete UV grid to its existing head/crown bone influences. Original
    // skeleton binaries, bones and animation timelines remain untouched. The old
    // crown's shape-specific FFD intentionally does not deform this new metal crown.
    static class KingVanguardCrownSkin
    {
        const string Crown = "Taç";
        const int PackedWidth = 496, PackedHeight = 288, Padding = 8, AddedHeight = 320;
        // Calibrated in the original crown's upright UV frame. The lower band
        // seats around the repaired head at both temples. The wider, taller fit
        // restores the crown proportions and keeps its center over the face.
        const float FitLeft = .015f, FitTop = -.10f, FitWidth = 1.10f, FitHeight = .98f;

        public static void Apply(SkeletonData data)
        {
            var setup = new Skeleton(data);
            setup.SetToSetupPose(); setup.UpdateWorldTransform();
            foreach (var skin in data.Skins)
            {
                var entries = new List<Skin.SkinEntry>(skin.GetAttachments());
                foreach (var entry in entries)
                {
                    if (entry.Name != Crown) continue;
                    var source = skin.GetAttachment(entry.SlotIndex, entry.Name) as MeshAttachment;
                    var region = source == null ? null : source.RendererObject as AtlasRegion;
                    if (region == null || region.name == "Vanguard Crown") continue;
                    int oldHeight = region.page.name.StartsWith("King_") ? 1872 :
                        region.page.name.StartsWith("AreaCompleted_") ? 1714 : 0;
                    if (oldHeight == 0 || region.page.height < oldHeight + AddedHeight) continue;
                    var page = region.page;
                    var replacementRegion = new AtlasRegion {
                        name = "Vanguard Crown", page = page, x = Padding, y = oldHeight + Padding,
                        width = PackedWidth, height = PackedHeight, originalWidth = PackedWidth,
                        originalHeight = PackedHeight, index = -1,
                        u = (float)Padding / page.width, u2 = (float)(Padding + PackedWidth) / page.width,
                        // Spine's Unity atlas loader flips V after parsing.
                        v = 1f - (float)(oldHeight + Padding) / page.height,
                        v2 = 1f - (float)(oldHeight + Padding + PackedHeight) / page.height
                    };
                    var replacement = Rebind(source, setup.Slots.Items[entry.SlotIndex], setup, replacementRegion);
                    // Spine 3.8 stores the attachment in both the dictionary key
                    // and value. Replace the key as well so skin enumeration and
                    // lookup agree, including subsequent modifier applications.
                    skin.RemoveAttachment(entry.SlotIndex, entry.Name);
                    skin.SetAttachment(entry.SlotIndex, entry.Name, replacement);
                }
            }
        }

        static MeshAttachment Rebind(MeshAttachment source, Slot slot, Skeleton setup, AtlasRegion region)
        { return RebindArt(source,slot,setup,region,"Vanguard Crown",new Rect(FitLeft,FitTop,FitWidth,FitHeight),5,4); }

        internal static MeshAttachment RebindArt(MeshAttachment source, Slot slot, Skeleton setup, AtlasRegion region,string path,Rect fit,int columns,int rows)
        {
            var world = new float[source.WorldVerticesLength]; source.ComputeWorldVertices(slot, world);
            var uv = source.RegionUVs;
            int ia=0, ib=0, ic=0; float largest=0;
            for (int t=0; t<source.Triangles.Length; t+=3)
            {
                int a=source.Triangles[t],b=source.Triangles[t+1],c=source.Triangles[t+2];
                float area=Mathf.Abs(Cross(Uv(uv,b)-Uv(uv,a),Uv(uv,c)-Uv(uv,a)));
                if(area>largest){largest=area;ia=a;ib=b;ic=c;}
            }
            if(largest<1e-6f)throw new InvalidOperationException("Crown calibration is degenerate.");
            var weights=new Dictionary<int,float>[world.Length/2]; int boneCursor=0,vertexCursor=0;
            for(int i=0;i<weights.Length;i++)
            {
                weights[i]=new Dictionary<int,float>();
                if(source.Bones==null){weights[i][slot.Bone.Data.Index]=1;continue;}
                int count=source.Bones[boneCursor++];
                for(int j=0;j<count;j++)
                {
                    int bone=source.Bones[boneCursor++]; float weight=source.Vertices[vertexCursor+2];vertexCursor+=3;
                    weights[i][bone]=weight;
                }
            }
            var targetUv=new float[columns*rows*2];var bones=new List<int>();var vertices=new List<float>();var triangles=new List<int>();
            for(int y=0;y<rows;y++)for(int x=0;x<columns;x++)
            {
                float u=(float)x/(columns-1),v=(float)y/(rows-1);int id=y*columns+x;
                targetUv[id*2]=u;targetUv[id*2+1]=v;
                var point=new Vector2(fit.x+u*fit.width,fit.y+v*fit.height);
                Vector3 affine=Bary(point,Uv(uv,ia),Uv(uv,ib),Uv(uv,ic));
                Vector2 position=Uv(world,ia)*affine.x+Uv(world,ib)*affine.y+Uv(world,ic)*affine.z;
                int aBest=ia,bBest=ib,cBest=ic;Vector3 blend=Vector3.zero;float distance=float.MaxValue;
                for(int t=0;t<source.Triangles.Length;t+=3)
                {
                    int a=source.Triangles[t],b=source.Triangles[t+1],c=source.Triangles[t+2];
                    Vector3 bcs=Bary(point,Uv(uv,a),Uv(uv,b),Uv(uv,c));
                    bcs=new Vector3(Mathf.Max(0,bcs.x),Mathf.Max(0,bcs.y),Mathf.Max(0,bcs.z));
                    float sum=bcs.x+bcs.y+bcs.z;if(sum<1e-8f)continue;bcs/=sum;
                    var near=Uv(uv,a)*bcs.x+Uv(uv,b)*bcs.y+Uv(uv,c)*bcs.z;float d=(point-near).sqrMagnitude;
                    if(d<distance){distance=d;aBest=a;bBest=b;cBest=c;blend=bcs;}
                }
                var mixed=new SortedDictionary<int,float>();
                Accumulate(mixed,weights[aBest],blend.x);Accumulate(mixed,weights[bBest],blend.y);Accumulate(mixed,weights[cBest],blend.z);
                float total=0;int used=0;foreach(var pair in mixed)if(pair.Value>1e-6f){total+=pair.Value;used++;}
                bones.Add(used);
                foreach(var pair in mixed)
                {
                    if(pair.Value<=1e-6f)continue;float localX,localY;
                    setup.Bones.Items[pair.Key].WorldToLocal(position.x,position.y,out localX,out localY);
                    bones.Add(pair.Key);vertices.Add(localX);vertices.Add(localY);vertices.Add(pair.Value/total);
                }
                if(x<columns-1&&y<rows-1){triangles.Add(id);triangles.Add(id+1);triangles.Add(id+columns);triangles.Add(id+1);triangles.Add(id+columns+1);triangles.Add(id+columns);}
            }
            var mesh=new MeshAttachment(source.Name){Path=path,WorldVerticesLength=targetUv.Length,
                Bones=bones.ToArray(),Vertices=vertices.ToArray(),RegionUVs=targetUv,Triangles=triangles.ToArray(),
                Width=source.Width*fit.width,Height=source.Height*fit.height,R=source.R,G=source.G,B=source.B,A=source.A};
            mesh.SetRegion(region);return mesh;
        }
        static void Accumulate(SortedDictionary<int,float> target,Dictionary<int,float> source,float scale)
        {foreach(var pair in source){float old;target.TryGetValue(pair.Key,out old);target[pair.Key]=old+pair.Value*scale;}}
        static Vector2 Uv(float[] values,int i){return new Vector2(values[i*2],values[i*2+1]);}
        static float Cross(Vector2 a,Vector2 b){return a.x*b.y-a.y*b.x;}
        static Vector3 Bary(Vector2 p,Vector2 a,Vector2 b,Vector2 c)
        {float d=Cross(b-a,c-a);if(Mathf.Abs(d)<1e-10f)return new Vector3(-1,-1,-1);float v=Cross(p-a,c-a)/d,w=Cross(b-a,p-a)/d;return new Vector3(1-v-w,v,w);}
    }
}
