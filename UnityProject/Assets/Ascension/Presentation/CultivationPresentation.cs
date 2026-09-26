using System;
using System.Collections.Generic;
using UnityEngine;
using A3Game.EngineAdapters;

namespace Ascension.Presentation
{
    public sealed partial class AscensionView
    {
        readonly Color panelColor=new Color(.025f,.065f,.075f,.97f);
        readonly Color accentColor=new Color(.77f,.65f,.39f);
        A3GameMediaDirector media;
        PortfolioAudio audioEvidence;
        readonly HashSet<string> soundKeys=new HashSet<string>();
        readonly List<AudioClip> spellClips=new List<AudioClip>();
        bool muted;
        Transform robe;
        Vector3 previousHero;

        // Presentation mesh helper. All decorative geometry is non-colliding.
        GameObject Taper(string name,float bottom,float top,float height,int sides,Material material)
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();
            for(int i=0;i<sides;i++)
            {
                float a=i*Mathf.PI*2/sides,b=(i+1)*Mathf.PI*2/sides;
                int n=vertices.Count;
                vertices.Add(new Vector3(Mathf.Cos(a)*bottom,0,Mathf.Sin(a)*bottom));
                vertices.Add(new Vector3(Mathf.Cos(a)*top,height,Mathf.Sin(a)*top));
                vertices.Add(new Vector3(Mathf.Cos(b)*top,height,Mathf.Sin(b)*top));
                vertices.Add(new Vector3(Mathf.Cos(b)*bottom,0,Mathf.Sin(b)*bottom));
                triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});
            }
            var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;return go;
        }
        Transform Detail(string name,PrimitiveType type,Vector3 position,Vector3 scale,Material material)
        {
            var t=Primitive(name,type,Vector3.zero,scale,material).transform;t.SetParent(hero,false);t.localPosition=position;return t;
        }
        void BuildCultivationPresentation()
        {
            hero.GetComponent<Renderer>().enabled=false;nose.GetComponent<Renderer>().enabled=false;
            BuildPortfolioWorld();
            media=gameObject.AddComponent<A3GameMediaDirector>();
            foreach(string key in new[]{"dash","shield","shield_absorbed","qi_collected","damaged","stage_changed","started","won","lost","warning","element","lightning","active_wind","active_fire","active_earth"})
            {
                var clip=Resources.Load<AudioClip>("Audio/"+key);
                if(clip){media.RegisterAudio(key,clip);soundKeys.Add(key);}else Debug.LogError("Missing audio: "+key);
            }
            media.GetComponent<AudioSource>().volume=.65f;
            audioEvidence=gameObject.AddComponent<PortfolioAudio>();
        }
        readonly Dictionary<string,float> lastSound=new Dictionary<string,float>();
        void CultivationFeedback(string key,Vector3 position)
        {
            if(key=="won"||key=="lost"||key=="stage_changed")audioEvidence.Capture(key);
            if(media && !muted)
            {
                string audioKey=key.StartsWith("warning_")?"warning":key.StartsWith("final_cue_")?"element":key;
                if(key.StartsWith("active_bloom"))audioKey="lightning";
                if(soundKeys.Contains(audioKey) && (!lastSound.TryGetValue(audioKey,out var at)||Time.unscaledTime-at>.12f)) {lastSound[audioKey]=Time.unscaledTime;Debug.Log("[AscensionMedia] "+JsonUtility.ToJson(media.TriggerAudio(audioKey)));}
            }
            if(key=="started"){learnedHazards.Clear();hazardTitleTime=0;}
            if(key.StartsWith("final_cue_"))
            {
                foreach(string h in new[]{"wind","fire","earth","bloom"}) if(key.Contains(h)&&learnedHazards.Add(h))
                {hazardTitle=h=="wind"?"风 劫":h=="fire"?"火 劫":h=="earth"?"地 劫":"散 花 雷";hazardTitleTime=2.2f;}
            }
            if(key=="lightning")
            {
                var bolt=new GameObject("A04_ForkedBolt").AddComponent<LineRenderer>();bolt.sharedMaterial=white;bolt.positionCount=9;bolt.widthMultiplier=.10f;
                for(int i=0;i<9;i++)bolt.SetPosition(i,position+new Vector3(i==0?0:Mathf.Sin(i*8.7f)*.65f,i*1.3f,i==0?0:Mathf.Cos(i*6.3f)*.4f));
                flashes.Add(bolt.gameObject);Destroy(bolt.gameObject,.20f);
            }
        }
        void AnimateCultivator()
        {
            if(robe && runtime.State.Status==RunStatus.Playing)
            {
                float moving=Mathf.Clamp01(Vector3.Distance(hero.position,previousHero)/Mathf.Max(.001f,Time.deltaTime));
                robe.localRotation=Quaternion.Euler(Mathf.Sin(Time.time*9)*moving*2,0,Mathf.Sin(Time.time*4.5f)*moving*2);
            }
            previousHero=hero.position;
        }
        string DamageSourceLabel(string value) => value.Replace("Normal","普通雷").Replace("Chain","连环雷").Replace("Tracking","追踪雷").Replace("Divine","九天神雷").Replace("Predictive","预测雷").Replace("Blocking","封路电场");
        void Panel(Rect r)
        {
            Box(r,panelColor);Box(new Rect(r.x,r.y,r.width,2),accentColor);Box(new Rect(r.x,r.y+r.height-2,r.width,2),new Color(.20f,.35f,.35f));
        }
        void DrawCultivationMenu(TribulationModel s)
        {
            bool paused=s.Status==RunStatus.Paused;
            Panel(new Rect(160,105,1280,700));
            string heading=s.Status==RunStatus.Menu?"渡 劫 · ASCENSION":s.Status==RunStatus.Won?"金 丹 初 成":s.Status==RunStatus.Lost?"此 劫 未 渡":"静 心 · 已暂停";
            GUI.Label(new Rect(250,140,1100,80),heading,title);
            string note=s.Status==RunStatus.Menu?"雷辨落点 · 风改站位 · 火封区域 · 地裂断路":s.Status==RunStatus.Won?"九天神雷已渡，灵台重归澄明":s.Status==RunStatus.Lost?"存活 "+s.Elapsed.ToString("F1")+" 秒 · 最后承伤："+DamageSourceLabel(s.LastDamageSource):"时间、危险区域与技能冷却已冻结";
            GUI.Label(new Rect(210,235,1180,50),note,text);
            if(!paused)
            {
                GUI.Label(new Rect(230,300,1140,40),"选择本局功法 · 局内锁定 · 重开保留选择",new GUIStyle(small){alignment=TextAnchor.MiddleCenter});
                for(int i=0;i<runtime.balance.techniques.Length;i++)
                {
                    var t=runtime.balance.techniques[i];float x=220+i*390;
                    Box(new Rect(x,355,370,218),i==runtime.TechniqueIndex?new Color(.13f,.26f,.27f):new Color(.055f,.105f,.12f));
                    if(i==runtime.TechniqueIndex)Box(new Rect(x,355,370,4),accentColor);
                    if(GUI.Button(new Rect(x+12,369,346,45),(i==runtime.TechniqueIndex?"◆ ":"")+t.name,button))runtime.SelectTechnique(i);
                    GUI.Label(new Rect(x+18,429,335,55),t.description,new GUIStyle(small){fontSize=17});
                    GUI.Label(new Rect(x+18,487,335,82),"御剑 "+t.dashCost+" Qi / "+t.dashCooldown+"s 冷却\n护盾 "+t.shieldCost+" Qi · "+t.shieldCapacity+"吸收 / "+t.shieldDuration+"s\n灵气收集 +"+t.qiValue,new GUIStyle(small){fontSize=17});
                }
                if(GUI.Button(new Rect(300,619,470,64),(runtime.FastPlaytest?"快速试炼 · ":"正式渡劫 · ")+runtime.balance.TotalDuration.ToString("0")+" 秒  ⇄",button))runtime.SetFastPlaytest(!runtime.FastPlaytest);
                if(GUI.Button(new Rect(830,619,470,64),s.Status==RunStatus.Menu?"开始渡劫":"再次渡劫",button))runtime.StartRun();
            }
            else
            {
                GUI.Label(new Rect(300,335,1000,80),runtime.SelectedTechnique.name+"\n观察预警、规划路线，再施展术法",text);
                if(GUI.Button(new Rect(560,461,480,65),"继续渡劫",button))runtime.Pause();
                if(GUI.Button(new Rect(560,548,480,65),"重新开始本局",button))runtime.StartRun();
            }
            GUI.Label(new Rect(300,706,1000,35),"WASD 移动    鼠标镜头    Shift 御剑    Q 护盾    P / Esc 暂停",new GUIStyle(small){alignment=TextAnchor.MiddleCenter,fontSize=18});
            if(GUI.Button(new Rect(650,753,300,32),muted?"术法音效：关闭":"术法音效：开启",new GUIStyle(button){fontSize=17})) {muted=!muted;if(media)media.GetComponent<AudioSource>().mute=muted;}
        }
    }
}
