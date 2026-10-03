using System.Collections.Generic;
using UnityEngine;
namespace Ascension.Presentation
{
    public sealed partial class AscensionView
    {
        Animation cultivatorAnimation;
        GameObject importedActor; bool actorAudited;
        string idleClip,runClip,currentClip;
        Transform shieldShell;
        Material fireFill,earthFill,bloomFill,quietJade,waterFill,waterEdge;
        readonly Dictionary<int,GameObject> elementArt=new Dictionary<int,GameObject>();
        readonly HashSet<string> learnedHazards=new HashSet<string>();
        string hazardTitle="";float hazardTitleTime;
        readonly List<Rect> warningLabels=new List<Rect>();
        void WorldWarning(Vector3 anchor,string label,int lines=1)
        {
            var p=view.WorldToViewportPoint(anchor);if(p.z<=0||p.x<0||p.x>1||p.y<0||p.y>1)return;
            float width=lines==1?220:330,height=lines==1?34:58;
            var box=new Rect(Mathf.Clamp(p.x*1600-width/2,24,1576-width),Mathf.Clamp((1-p.y)*900-height-12,225,670-height),width,height);
            // Only label placement changes: the exact telegraph geometry remains at its damage location.
            for(int attempt=0;attempt<12;attempt++)
            {
                bool collision=false;foreach(var used in warningLabels)if(box.Overlaps(used)){collision=true;break;}
                if(!collision)break;
                box.y-=height+6;if(box.y<225){box.y=600-height;box.x=Mathf.Clamp(box.x+width+8,24,1576-width);}
            }
            warningLabels.Add(box);Box(box,new Color(.02f,.045f,.065f,.82f));
            GUI.Label(box,label,new GUIStyle(small){alignment=TextAnchor.MiddleCenter,fontSize=18});
        }
        Material Flat(Color c){var m=new Material(Resources.Load<Shader>("Telegraph"));m.color=c;return m;}
        Material Translucent(Color c){var m=new Material(Resources.Load<Shader>("Aura"));m.color=c;return m;}
        void BuildPortfolioWorld()
        {
            var floor=GameObject.Find("Altar");var floorMat=new Material(Resources.Load<Shader>("PortfolioSurface"));floorMat.color=new Color(.25f,.32f,.33f);floorMat.SetFloat("_Floor",1);floor.GetComponent<Renderer>().sharedMaterial=floorMat;
            Primitive("Floating altar lower tier",PrimitiveType.Cylinder,new Vector3(0,-1,0),new Vector3(35,.25f,35),stone);
            Primitive("Floating altar foundation",PrimitiveType.Cylinder,new Vector3(0,-1.9f,0),new Vector3(32,.65f,32),ink);
            var inlay=Mat(new Color(.38f,.37f,.26f));
            foreach(float r in new[]{3.3f,3.65f,5.4f,15.1f,16.4f})Ring("Bronze formation inlay",Vector3.zero,r,inlay,.035f);
            for(int i=0;i<8;i++)
            {
                float a=i*Mathf.PI/4;Vector3 p=new Vector3(Mathf.Sin(a)*17.7f,0,Mathf.Cos(a)*17.7f);
                Primitive("Lantern foot",PrimitiveType.Cylinder,p+Vector3.up*.15f,new Vector3(1.5f,.15f,1.5f),stone);
                var column=Taper("Octagonal bronze lantern",.48f,.32f,1.4f,8,stone);column.transform.position=p;
                Primitive("Jade lantern light",PrimitiveType.Sphere,p+Vector3.up*1.65f,new Vector3(.4f,.6f,.4f),jade);
                var roof=Taper("Pagoda cap",.8f,0,.6f,4,inlay);roof.transform.position=p+Vector3.up*2;
                for(int j=0;j<3;j++)
                {
                    var mark=Primitive("Trigram inlay",PrimitiveType.Cube,new Vector3(Mathf.Sin(a)*(4.3f+j*.25f),.013f,Mathf.Cos(a)*(4.3f+j*.25f)),new Vector3(.85f,.015f,.055f),inlay);mark.transform.rotation=Quaternion.Euler(0,a*Mathf.Rad2Deg,0);
                }
            }
            // Finished panoramic environment matte, behind all gameplay and without colliders.
            var backdrop=Primitive("Cloud sea and thunder sky",PrimitiveType.Quad,Vector3.zero,Vector3.one,new Material(Resources.Load<Shader>("CloudBackdrop")));
            backdrop.GetComponent<Renderer>().sharedMaterial.mainTexture=Resources.Load<Texture2D>("Art/CloudSea");
            backdrop.transform.SetParent(view.transform,false);backdrop.transform.localPosition=new Vector3(0,0,145);backdrop.transform.localScale=new Vector3(310,174,1);
            var prefab=Resources.Load<GameObject>("Art/Cultivator");
            if(prefab)
            {
                var pivot=new GameObject("Cultivator scale and origin").transform;pivot.SetParent(hero,false);
                var actor=Instantiate(prefab);actor.name="Cultivator · Quaternius Monk";actor.transform.SetParent(pivot,false);
                importedActor=actor;
                var renderers=actor.GetComponentsInChildren<Renderer>();Bounds bounds=new Bounds(actor.transform.position,Vector3.zero);bool first=true;int triangles=0;
                var cloth=new Material(Resources.Load<Shader>("PortfolioSurface"));cloth.mainTexture=Resources.Load<Texture2D>("Art/Cultivator");cloth.color=new Color(1.3f,1.45f,1.4f);
                foreach(var r in renderers){if(first){bounds=r.bounds;first=false;}else bounds.Encapsulate(r.bounds);r.sharedMaterial=cloth;var sk=r as SkinnedMeshRenderer;if(sk)triangles+=sk.sharedMesh.triangles.Length/3;}
                cultivatorAnimation=actor.GetComponent<Animation>();
                if(cultivatorAnimation)
                {
                    foreach(AnimationState a in cultivatorAnimation){a.wrapMode=WrapMode.Loop;if(a.name.EndsWith("|Idle"))idleClip=a.name;if(a.name.ToLower().Contains("run"))runClip=a.name;Debug.Log("[AscensionAsset] animation="+a.name);}
                    if(idleClip!=null){cultivatorAnimation.Play(idleClip);cultivatorAnimation.Sample();}
                }
                // Imported skinned bounds can include the entire animation range. Fit actual posed vertices.
                first=true;
                foreach(var sk in actor.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    var baked=new Mesh();sk.BakeMesh(baked);
                    foreach(var v in baked.vertices){var world=sk.transform.TransformPoint(v);if(first){bounds=new Bounds(world,Vector3.zero);first=false;}else bounds.Encapsulate(world);}Destroy(baked);
                    sk.updateWhenOffscreen=true;
                }
                // FBX bind-pose bounds are in centimeters until the first animation update.
                // Final fit happens after native skinning has evaluated, on the independent pivot.
                pivot.localPosition=new Vector3(0,-1,0);
                Debug.Log("[AscensionAsset] cultivator triangles="+triangles+" bindBounds="+bounds);
            }
            else Debug.LogError("[AscensionAsset] Cultivator resource missing");
            quietJade=Translucent(new Color(.25f,.85f,.88f,.13f));
            shieldShell=Primitive("Protective Qi shell",PrimitiveType.Sphere,Vector3.zero,Vector3.one*2.35f,quietJade).transform;
            fireFill=Translucent(new Color(1,.22f,.025f,.28f));earthFill=Flat(new Color(.28f,.18f,.12f));bloomFill=Translucent(new Color(.55f,.35f,1,.16f));
            waterFill=Translucent(new Color(.04f,.4f,1,.24f));waterEdge=Flat(new Color(.15f,.65f,1));
        }
        Transform MakeQi(Vector3 p)
        {
            var root=Primitive("Qi pearl",PrimitiveType.Sphere,p,Vector3.one*.34f,jade).transform;
            var halo=Ring("Qi orbit",Vector3.zero,.78f,jade,.065f);halo.useWorldSpace=false;halo.transform.SetParent(root,false);halo.transform.localRotation=Quaternion.Euler(55,0,20);
            var glimmer=Primitive("Qi pearl glow",PrimitiveType.Sphere,Vector3.zero,Vector3.one*1.5f,quietJade);glimmer.transform.SetParent(root,false);
            return root;
        }
        void UpdatePortfolioWorld()
        {
            var s=runtime.State;shieldShell.gameObject.SetActive(s.ShieldRemaining>0);shieldShell.position=hero.position;
            if(importedActor && !actorAudited && Time.frameCount>30)
            {
                actorAudited=true;
                var rs=importedActor.GetComponentsInChildren<SkinnedMeshRenderer>();var liveBounds=rs[0].bounds;
                foreach(var r in rs)liveBounds.Encapsulate(r.bounds);
                var pivot=importedActor.transform.parent;float correction=1.95f/Mathf.Max(.001f,liveBounds.size.y);
                var center=hero.InverseTransformPoint(liveBounds.center)-pivot.localPosition;
                var bottom=hero.InverseTransformPoint(liveBounds.min)-pivot.localPosition;
                pivot.localScale*=correction;pivot.localPosition=new Vector3(-center.x*correction,-1-bottom.y*correction,-center.z*correction);
                Debug.Log("[AscensionAssetQA] liveFit height=1.95 source="+liveBounds+" correction="+correction+" pivot="+pivot.localPosition);
                foreach(var sk in importedActor.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    var m=new Mesh();sk.BakeMesh(m);var b=m.bounds;
                    Debug.Log("[AscensionAssetQA] renderer="+sk.name+" enabled="+sk.enabled+" active="+sk.gameObject.activeInHierarchy+" worldBounds="+sk.bounds+" baked="+b+" lossy="+sk.transform.lossyScale+" root="+importedActor.transform.localScale+" camera="+view.WorldToViewportPoint(sk.bounds.center));Destroy(m);
                }
            }
            if(cultivatorAnimation)
            {
                string clip=Vector3.Distance(hero.position,previousHero)>.003f?runClip:idleClip;
                if(clip!=null&&clip!=currentClip){cultivatorAnimation.CrossFade(clip,.12f);currentClip=clip;}
                cultivatorAnimation.enabled=s.Status!=RunStatus.Paused;
            }
            hazardTitleTime=Mathf.Max(0,hazardTitleTime-Time.unscaledDeltaTime);
        }
        GameObject Disc(string name,Vector3 p,float radius,Material m)
        {
            var vertices=new List<Vector3>{Vector3.zero};var tris=new List<int>();
            for(int i=0;i<=64;i++){float a=i*Mathf.PI/32;vertices.Add(new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*radius);if(i>0){tris.Add(0);tris.Add(i+1);tris.Add(i);}}
            var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();
            var o=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));o.transform.position=p+Vector3.up*.025f;o.GetComponent<MeshFilter>().sharedMesh=mesh;o.GetComponent<Renderer>().sharedMaterial=m;return o;
        }
        void DrawElementArt()
        {
            var alive=new HashSet<int>();
            foreach(var h in runtime.State.Elements)
            {
                if(!h.active)continue;alive.Add(h.id);
                if(!elementArt.TryGetValue(h.id,out var o))
                {
                    o=new GameObject("Active art "+h.kind);elementArt.Add(h.id,o);
                    if(h.kind==ElementKind.Water)
                    {
                        Disc("Water slow zone",h.position,h.radius,waterFill).transform.SetParent(o.transform,true);
                        for(int j=1;j<=3;j++)Ring("Water ripple",h.position+Vector3.up*.02f,h.radius*j/4,waterEdge,.035f).transform.SetParent(o.transform,true);
                    }
                    if(h.kind==ElementKind.Ball)
                    {
                        Primitive("Lightning sphere",PrimitiveType.Sphere,Vector3.zero,Vector3.one*h.radius*1.6f,white).transform.SetParent(o.transform,false);
                        Primitive("Lightning corona",PrimitiveType.Sphere,Vector3.zero,Vector3.one*h.radius*2.3f,bloomFill).transform.SetParent(o.transform,false);
                        var orbit=Ring("Ball electric orbit",Vector3.zero,h.radius,violet,.09f);orbit.useWorldSpace=false;orbit.transform.SetParent(o.transform,false);orbit.transform.localRotation=Quaternion.Euler(65,0,25);
                    }
                    if(h.kind==ElementKind.Fire)
                    {
                        Disc("Persistent fire ground",h.position,h.radius,fireFill).transform.SetParent(o.transform,true);
                        for(int j=0;j<12;j++){float a=j*2.39996f,r=h.radius*.8f*Mathf.Sqrt((j+1)/12f);var flame=Taper("Low flame",.09f,0,.18f+(j%3)*.12f,5,orange);flame.transform.position=h.position+new Vector3(Mathf.Sin(a)*r,.08f,Mathf.Cos(a)*r);flame.transform.SetParent(o.transform,true);}
                    }
                    if(h.kind==ElementKind.Earth)
                    {
                        var crack=new GameObject("Fracture seam").AddComponent<LineRenderer>();crack.sharedMaterial=earthFill;crack.positionCount=17;crack.widthMultiplier=.12f;crack.transform.SetParent(o.transform);
                        var d=(h.end-h.position).normalized;var side=new Vector3(-d.z,0,d.x);
                        for(int j=0;j<17;j++){var p=Vector3.Lerp(h.position,h.end,j/16f)+side*Mathf.Sin(j*5.3f)*h.radius*.4f+Vector3.up*.09f;crack.SetPosition(j,p);if(j%3==0){var rock=Taper("Broken stone",.18f,.055f,.22f,5,stone);rock.transform.position=p;rock.transform.SetParent(o.transform,true);}}
                    }
                    if(h.kind==ElementKind.BloomCenter)Disc("Scatter core",h.position,h.radius,bloomFill).transform.SetParent(o.transform,true);
                    if(h.kind==ElementKind.Wind)
                    {
                        var side=new Vector3(-h.direction.z,0,h.direction.x);
                        for(int j=-3;j<=3;j++){var l=new GameObject("Directional wind ribbon").AddComponent<LineRenderer>();l.sharedMaterial=jade;l.positionCount=3;l.widthMultiplier=.055f;l.transform.SetParent(o.transform);var p=runtime.State.Position+side*j*1.5f;l.SetPositions(new[]{p-h.direction*4+Vector3.up*.3f,p+Vector3.up*.65f,p+h.direction*4+Vector3.up*.3f});}
                    }
                }
                if(h.kind==ElementKind.Ball)
                {
                    o.transform.position=h.position+Vector3.up*(h.radius+.12f);
                    o.transform.localScale=Vector3.one*Mathf.Clamp01((h.warning+h.duration-h.age)/1.2f);
                    o.transform.rotation=Quaternion.Euler(0,h.age*90,0);
                }
            }
            var stale=new List<int>();foreach(var p in elementArt)if(!alive.Contains(p.Key))stale.Add(p.Key);foreach(int id in stale){Destroy(elementArt[id]);elementArt.Remove(id);}
        }
    }
}
