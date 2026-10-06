using System;
using UnityEngine;

namespace RoyalThrone.Presentation.AreaMotion
{
    // Compact, engine-independent extraction format. Original IDs/curves remain in Design/AreaMotionImport.
    [Serializable] public sealed class SceneData { public int area; public float[] camera; public NodeData[] nodes; public SpriteData[] sprites; public ClipData[] clips; public TaskData[] tasks; public AttachedParticle[] particles; }
    [Serializable] public sealed class NodeData { public string name; public int parent, sprite, order, maskInteraction, maskSprite, maskBack, maskFront, sortingGroupOrder; public Vector3 position, scale; public Quaternion rotation; public Color color; public bool active, enabled, flipX, flipY, flag; public int[] bones; }
    [Serializable] public sealed class SpriteData { public string name, texture; public Vector3[] positions; public Vector2[] uv; public int[] indices; public Vector4[] weights, boneIndices; public BindPose[] bindposes; public bool water; public int flow; }
    [Serializable] public sealed class BindPose { public float e00,e01,e02,e03,e10,e11,e12,e13,e20,e21,e22,e23,e30,e31,e32,e33; public Matrix4x4 Matrix => new Matrix4x4(new Vector4(e00,e10,e20,e30),new Vector4(e01,e11,e21,e31),new Vector4(e02,e12,e22,e32),new Vector4(e03,e13,e23,e33)); }
    [Serializable] public sealed class ClipData { public string name; public float duration; public bool loop; public CurveData[] curves; }
    [Serializable] public sealed class CurveData { public int node, property; public MotionKey[] keys; }
    [Serializable] public struct MotionKey { public float t,v,i,o; }
    [Serializable] public sealed class TaskData { public string name,audio,effect; public int root; public float duration; public int[] clips; public float[] offsets; public bool reconstructed; }
    [Serializable] public sealed class FlagData { public float duration,fps; public FlagFrame[] frames; }
    [Serializable] public sealed class FlagFrame { public FlagMesh[] meshes; }
    [Serializable] public sealed class FlagMesh { public Vector3[] positions; public Vector2[] uv; public int[] indices; }
    [Serializable] public sealed class EffectLibrary { public EffectData[] effects; public ParticleSprite[] sprites; }
    [Serializable] public sealed class EffectData { public string name; public EffectNode[] nodes; }
    [Serializable] public sealed class EffectNode { public string name; public int parent; public bool active; public Vector3 position,scale; public Quaternion rotation; public ParticleData data; }
    [Serializable] public sealed class ParticleSprite { public string id; public int x,y,w,h; public float ppu; }
    [Serializable] public sealed class AttachedParticle { public int node; public ParticleData data; }
    [Serializable] public sealed class NumberCurve { public int mode; public float max,min; public MotionKey[] keys,lower; }
    [Serializable] public sealed class ColorCurve { public ColorKey[] colors; public AlphaKey[] alphas; }
    [Serializable] public struct ColorKey { public float t; public Color c; }
    [Serializable] public struct AlphaKey { public float t,a; }
    [Serializable] public sealed class ParticleBurst { public float time; public NumberCurve count; }
    [Serializable] public sealed class ParticleData
    {
        public bool loop,shapeEnabled,sizeEnabled,rotationEnabled,velocityEnabled,velocityWorld,colorEnabled,additive;
        public float duration,radius,angle,radiusThickness,lengthScale,velocityScale;
        public int space,scaling,shape,renderMode,order,sortingLayer,startColorMode;
        public NumberCurve delay,lifetime,speed,size,rotation,gravity,rate,sizeCurve,rotationCurve,vx,vy,vz;
        public Color startColor,startColorMin; public ColorCurve color;
        public Vector3 shapePosition,shapeRotation,shapeScale;
        public ParticleBurst[] bursts; public string[] sprites;
    }
    public static class MotionCurves
    {
        public static float Sample(MotionKey[] k,float t,bool step=false)
        {
            if(k==null||k.Length==0)return 0;
            if(t<=k[0].t)return k[0].v;
            int l=0,h=k.Length-1;if(t>=k[h].t)return k[h].v;
            while(h-l>1){int m=(l+h)/2;if(k[m].t<=t)l=m;else h=m;}
            var a=k[l];var b=k[h];if(step)return a.v;
            float d=b.t-a.t;if(d<.000001f)return b.v;
            float f=(t-a.t)/d,f2=f*f,f3=f2*f;
            return (2*f3-3*f2+1)*a.v+(f3-2*f2+f)*d*a.o+(-2*f3+3*f2)*b.v+(f3-f2)*d*b.i;
        }
        public static AnimationCurve UnityCurve(MotionKey[] keys)
        {
            if(keys==null||keys.Length==0)return AnimationCurve.Linear(0,1,1,1);
            var result=new Keyframe[keys.Length];for(int i=0;i<keys.Length;i++){var k=keys[i];result[i]=new Keyframe(k.t,k.v,k.i,k.o);}
            return new AnimationCurve(result);
        }
        public static ParticleSystem.MinMaxCurve Particle(NumberCurve c,float multiply=1)
        {
            if(c==null)return new ParticleSystem.MinMaxCurve(0);
            switch(c.mode){case 1:return new ParticleSystem.MinMaxCurve(c.max*multiply,UnityCurve(c.keys));case 2:return new ParticleSystem.MinMaxCurve(c.max*multiply,UnityCurve(c.lower),UnityCurve(c.keys));case 3:return new ParticleSystem.MinMaxCurve(Mathf.Min(c.min,c.max)*multiply,Mathf.Max(c.min,c.max)*multiply);default:return new ParticleSystem.MinMaxCurve(c.max*multiply);}
        }
    }
    public static class AreaProgressMigration
    {
        public static int Map(int area,int mask)
        {
            int oldCount=area==2?6:area==3?7:0,newCount=area==2?7:area==3?9:0;
            if(oldCount==0)return mask;
            int count=0;for(int i=0;i<oldCount;i++)if((mask&(1<<i))!=0)count++;
            return count==oldCount?(1<<newCount)-1:(1<<count)-1;
        }
        public static int Credit(int area,int mask)
        {
            int[] oldCosts=area==2?new[]{1,2,2,2,2,3}:area==3?new[]{2,2,2,2,2,2,3}:null;
            int[] newCosts=area==2?new[]{1,2,1,2,2,2,2}:area==3?new[]{1,2,1,2,2,2,2,1,2}:null;
            if(oldCosts==null)return 0;int oldPaid=0,newPaid=0,mapped=Map(area,mask);
            for(int i=0;i<oldCosts.Length;i++)if((mask&(1<<i))!=0)oldPaid+=oldCosts[i];
            for(int i=0;i<newCosts.Length;i++)if((mapped&(1<<i))!=0)newPaid+=newCosts[i];
            return Math.Max(0,oldPaid-newPaid);
        }
    }
}
