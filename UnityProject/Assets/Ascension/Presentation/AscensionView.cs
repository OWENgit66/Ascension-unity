using System.Collections.Generic;
using UnityEngine;

namespace Ascension.Presentation
{
    public sealed partial class AscensionView : MonoBehaviour
    {
        public AscensionRuntime runtime;
        Transform hero, nose;
        Camera view;
        Material stone, white, orange, ink, jade, violet, gold, lightningBlue;
        readonly Dictionary<int, LineRenderer> rings = new Dictionary<int, LineRenderer>();
        readonly List<GameObject> flashes = new List<GameObject>();
        readonly Dictionary<int,Transform> qiVisuals=new Dictionary<int,Transform>();
        LineRenderer shieldRing;
        TrailRenderer dashTrail;
        float damageFlash, phaseFlash;
        float toastTime;
        string toast="";
        static readonly string[] stageNames={"第一重 · 识雷","第二重 · 连劫","第三重 · 追命","终劫 · 九天神雷"};
        GUIStyle title, text, small, button;
        Font font;

        void Start()
        {
            stone = Mat(new Color(.13f,.22f,.24f)); white = Mat(new Color(.85f,.91f,.95f));
            orange = Mat(new Color(1,.48f,.15f)); ink = Mat(new Color(.08f,.13f,.20f)); jade = Mat(new Color(.3f,.9f,.95f));
            violet=Mat(new Color(.85f,.4f,1)); gold=Mat(new Color(1,.82f,.3f));
            foreach(var m in new[]{white,orange,jade,violet,gold})m.shader=Resources.Load<Shader>("Telegraph");
            lightningBlue=Flat(new Color(.38f,.60f,1f));
            RenderSettings.ambientLight = new Color(.53f,.61f,.73f);
            RenderSettings.fog = true; RenderSettings.fogColor = new Color(.08f,.14f,.23f); RenderSettings.fogDensity = .008f;
            Primitive("Altar", PrimitiveType.Cylinder, new Vector3(0,-.4f,0), new Vector3(34,.4f,34), stone);
            Ring("Boundary", Vector3.zero, 16.6f, Mat(new Color(.30f,.52f,.52f)), .10f);
            Ring("Formation", Vector3.zero, 5, Mat(new Color(.20f,.33f,.34f)), .045f);
            hero = Primitive("Cultivator", PrimitiveType.Capsule, new Vector3(0,1,0),new Vector3(.8f,1,.8f),white).transform;
            nose = Primitive("Forward", PrimitiveType.Cube, Vector3.zero,new Vector3(.22f,.2f,.6f),jade).transform;
            dashTrail=hero.gameObject.AddComponent<TrailRenderer>(); dashTrail.sharedMaterial=jade; dashTrail.time=.3f; dashTrail.startWidth=.45f; dashTrail.endWidth=.02f; dashTrail.emitting=false;
            shieldRing=Ring("Shield",Vector3.zero,1.1f,jade,.14f); shieldRing.useWorldSpace=false;
            view = new GameObject("Ascension Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            view.tag = "MainCamera"; view.fieldOfView = 60; view.farClipPlane = 180;
            view.clearFlags = CameraClearFlags.SolidColor; view.backgroundColor = new Color(.055f,.09f,.16f);
            runtime.GameplayEvent += Feedback;
            font = Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","SimHei","Arial"},24);
            BuildCultivationPresentation();
        }
        Material Mat(Color color) { var m = new Material(Resources.Load<Shader>("Whitebox")); m.color = color; return m; }
        GameObject Primitive(string name, PrimitiveType type, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name; go.transform.position = pos; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat; Destroy(go.GetComponent<Collider>()); return go;
        }
        LineRenderer Ring(string name, Vector3 pos, float radius, Material mat, float width)
        {
            var line = new GameObject(name).AddComponent<LineRenderer>(); line.sharedMaterial = mat;
            line.widthMultiplier = width; line.loop = true; line.positionCount = 64;
            for(int i=0;i<64;i++) { float a = i*Mathf.PI*2/64; line.SetPosition(i,pos+new Vector3(Mathf.Cos(a)*radius,.045f,Mathf.Sin(a)*radius)); }
            return line;
        }
        void Feedback(string key, Vector3 pos)
        {
            CultivationFeedback(key,pos);
            if(key=="damaged") damageFlash= .4f;
            if(key=="started")
            {
                foreach(var o in flashes) if(o) Destroy(o); flashes.Clear();
                foreach(var r in rings.Values) Destroy(r.gameObject); rings.Clear();
                foreach(var q in qiVisuals.Values) Destroy(q.gameObject); qiVisuals.Clear();
                dashTrail.Clear(); phaseFlash=4; toastTime=0;
            }
            if(key=="stage_changed") phaseFlash=4;
            if(key=="qi_collected") { toast="灵气 +"+runtime.balance.qiValue; toastTime=1.2f; }
            if(key=="shield_absorbed") { toast="护盾抵挡！"; toastTime=1.2f; }
            if(key=="dash_evaded") { toast="御剑避雷！"; toastTime=1.2f; }
            if(key.EndsWith("unavailable")) { toast="技能未就绪：检查冷却与灵气"; toastTime=1.5f; }
            if(key=="lightning")
            {
                var bolt = Primitive("Lightning",PrimitiveType.Cylinder,pos+Vector3.up*8,new Vector3(.2f,8,.2f),white);
                flashes.Add(bolt); Destroy(bolt,.22f);
                float radius=0;foreach(var strike in runtime.State.Strikes)if(strike.impacted && (strike.position-pos).sqrMagnitude<.001f)radius=Mathf.Max(radius,strike.radius);
                if(radius>0){var impact=Ring("Impact",pos,radius,white,.2f);flashes.Add(impact.gameObject);Destroy(impact.gameObject,.22f);}
            }
        }
        void LateUpdate()
        {
            if(!view) return;
            DrawElements();
            DrawElementArt();
            var state = runtime.State;
            AudioListener.pause=state.Status==RunStatus.Paused;
            hero.position = state.Position + Vector3.up;
            hero.rotation = Quaternion.LookRotation(runtime.Facing);
            UpdatePortfolioWorld();
            AnimateCultivator();
            nose.position = hero.position + runtime.Facing*.55f; nose.rotation=hero.rotation;
            dashTrail.emitting=state.DashRemaining>0;
            shieldRing.gameObject.SetActive(state.ShieldRemaining>0);
            shieldRing.transform.position=hero.position;
            shieldRing.transform.rotation=Quaternion.Euler(60,Time.time*120,0);
            view.backgroundColor=state.Status==RunStatus.Won?new Color(.42f,.61f,.72f):Color.Lerp(new Color(.055f,.09f,.16f),new Color(.065f,.025f,.11f),state.Stage/3f);
            Quaternion orbit = Quaternion.Euler(runtime.Pitch,runtime.Yaw,0);
            view.transform.position = hero.position + Vector3.up*.6f - orbit*Vector3.forward*11;
            view.transform.rotation = orbit;
            var alive = new HashSet<int>();
            foreach(var s in state.Strikes)
            {
                alive.Add(s.id);
                Material color=s.kind==StrikeKind.Blocking?(s.impacted?lightningBlue:violet):s.kind==StrikeKind.Predictive?violet:s.kind==StrikeKind.Divine?lightningBlue:s.trackTime>0&&!s.locked?violet:lightningBlue;
                if(!rings.TryGetValue(s.id,out var ring)) { ring = Ring("Warning",s.position,s.radius,color,.12f); rings.Add(s.id,ring); }
                ring.sharedMaterial=color;
                for(int j=0;j<64;j++) { float a=j*Mathf.PI*2/64; ring.SetPosition(j,s.position+new Vector3(Mathf.Cos(a)*s.radius,.045f,Mathf.Sin(a)*s.radius)); }
                ring.widthMultiplier = .16f + .14f * Mathf.Clamp01(s.age/s.warning);
            }
            var stale = new List<int>(); foreach(var p in rings) if(!alive.Contains(p.Key)) stale.Add(p.Key);
            foreach(var id in stale) { Destroy(rings[id].gameObject); rings.Remove(id); }
            var liveQi=new HashSet<int>();
            foreach(var q in state.Orbs)
            {
                liveQi.Add(q.id);
                if(!qiVisuals.TryGetValue(q.id,out var tr)) { tr=MakeQi(q.position); qiVisuals.Add(q.id,tr); }
                tr.position=q.position+Vector3.up*(.65f+Mathf.Sin(Time.time*3+q.id)*.12f);
                tr.rotation=Quaternion.Euler(45,Time.time*70,45);
            }
            stale.Clear(); foreach(var q in qiVisuals) if(!liveQi.Contains(q.Key)) stale.Add(q.Key);
            foreach(var id in stale) { Destroy(qiVisuals[id].gameObject); qiVisuals.Remove(id); }
            flashes.RemoveAll(x=>x==null);
            damageFlash = Mathf.Max(0,damageFlash-Time.unscaledDeltaTime); phaseFlash = Mathf.Max(0,phaseFlash-Time.unscaledDeltaTime);
            toastTime=Mathf.Max(0,toastTime-Time.unscaledDeltaTime);
        }
        void Styles()
        {
            if(title != null) return;
            title = new GUIStyle(GUI.skin.label){font=font,fontSize=52,alignment=TextAnchor.MiddleCenter}; title.normal.textColor = new Color(.86f,.91f,.9f);
            text = new GUIStyle(title){fontSize=26}; small = new GUIStyle(text){fontSize=20,alignment=TextAnchor.MiddleLeft};
            button = new GUIStyle(GUI.skin.button){font=font,fontSize=24};
            var normal=new Texture2D(1,1);normal.SetPixel(0,0,new Color(.12f,.23f,.25f));normal.Apply();
            var hover=new Texture2D(1,1);hover.SetPixel(0,0,new Color(.23f,.37f,.37f));hover.Apply();
            button.normal.background=normal;button.hover.background=hover;button.active.background=hover;
            button.normal.textColor=new Color(.90f,.86f,.73f);button.hover.textColor=Color.white;
        }
        void Box(Rect r, Color color) { var old=GUI.color; GUI.color=color; GUI.DrawTexture(r,Texture2D.whiteTexture); GUI.color=old; }
        void OnGUI()
        {
            if(runtime==null || runtime.State==null) return;
            Styles(); GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(Screen.width/1600f,Screen.height/900f,1));
            var s=runtime.State;
            warningLabels.Clear();
            DrawElementHUD();
            if(hazardTitleTime>0 && s.Status==RunStatus.Playing){Panel(new Rect(625,155,350,65));GUI.Label(new Rect(625,155,350,65),hazardTitle,text);}
            if(s.Status==RunStatus.Playing)GUI.Label(new Rect(52,32,300,40),runtime.SelectedTechnique.name,small);
            if(damageFlash>0) Box(new Rect(0,0,1600,900),new Color(.9f,.12f,.15f,damageFlash*.35f));
            if(s.Status==RunStatus.Playing || s.Status==RunStatus.Paused)
            {
                int seconds=Mathf.CeilToInt(s.Remaining);
                GUI.Label(new Rect(350,20,900,50),(runtime.FastPlaytest?"[FAST] ":"")+stageNames[s.Stage]+"    "+(seconds/60).ToString("00")+":"+(seconds%60).ToString("00"),text);
                if(s.Stage==3) GUI.Label(new Rect(500,70,600,35),"九天神雷  "+s.FinalWaves+" / "+runtime.balance.finalWaves,text);
                Panel(new Rect(48,733,400,128));
                GUI.Label(new Rect(64,739,370,32),"生命 HP   "+Mathf.CeilToInt(s.HP)+" / "+runtime.balance.maxHP,small);
                Box(new Rect(64,774,368,13),new Color(.2f,.18f,.23f)); Box(new Rect(64,774,368*s.HP/runtime.balance.maxHP,13),new Color(.94f,.42f,.4f));
                GUI.Label(new Rect(64,791,370,32),"灵气 QI   "+Mathf.FloorToInt(s.Qi)+" / "+runtime.balance.maxQi,small);
                Box(new Rect(64,826,368,13),new Color(.13f,.21f,.25f)); Box(new Rect(64,826,368*s.Qi/runtime.balance.maxQi,13),new Color(.3f,.9f,.95f));
                Panel(new Rect(1060,733,492,128));
                GUI.Label(new Rect(1078,740,460,38),"Shift 御剑 · "+runtime.balance.dashCost+" Qi   "+SkillState(s.CanDash,s.DashCooldown,s.DashRemaining),small);
                GUI.Label(new Rect(1078,781,460,38),"Q 护盾 · "+runtime.balance.shieldCost+" Qi   "+(s.ShieldRemaining>0?"吸收余量 "+Mathf.CeilToInt(s.ShieldAmount):SkillState(s.CanShield,s.ShieldCooldown,0)),small);
                GUI.Label(new Rect(1078,824,460,28),"WASD 移动 · 鼠标镜头 · Esc/P 暂停",small);
                if(phaseFlash>0) GUI.Label(new Rect(260,120,1080,60),s.Stage==0?"走出预警 · 拾取青色灵气 · 为技能留有余量":s.Stage==1?"连环雷来袭 · 保持移动，雷后补气":s.Stage==2?"预测圈须转向 · 封路电场须绕行 · 珍惜灵气":"最后九道神雷 · 活下来，突破金丹",text);
                if(toastTime>0) GUI.Label(new Rect(380,642,840,48),toast,text);
            }
            if(s.Status==RunStatus.Playing) return;
            DrawCultivationMenu(s);
        }
        string SkillState(bool ready,float cooldown,float active) => runtime.State.Status==RunStatus.Paused?"已暂停":active>0?"施展中":cooldown>0?cooldown.ToString("F1")+"s":ready?"就绪":"灵气不足";
        void OnDestroy() { if(runtime) runtime.GameplayEvent-=Feedback; }
    }
}
