using System;
using System.Collections.Generic;
using UnityEngine;
namespace Ascension
{
    [Serializable] public sealed class FinalBalance
    {
        public float windWarning=1.5f, windDuration=1.2f, windDistanceR=.19f;
        public float fireWarning=1.2f, fireDuration=3.5f, fireRadiusR=.10f, fireTick=.5f, fireDamage=5;
        public int fireLimit=3, maxSystems=2;
        public float earthWarning=1.35f, earthDuration=.65f, earthLengthR=.45f, earthWidthR=.05f, earthDamage=25;
        public float bloomWarning=1.3f, bloomCenterRadiusR=.085f, bloomCenterDamage=20, bloomDelay=.25f;
        public float bloomLengthR=.45f, bloomWidthR=.04f, bloomDuration=.35f, bloomRayDamage=18;
        public int bloomRays=6, lateBloomRays=8;
        public float centerLimitR=.48f, placementLead=.35f, pairOffsetR=.17f, earthOffsetR=.12f;
        public float introEnd=18, comboEnd=34, divineStart=34.5f, divineInterval=2.5f;
        public float[] cueTimes={.8f,4,9.2f,12.8f,18,24,28,35,41,49};
        public string[] cues={"Wind","Fire","Earth","Bloom","WindFire","EarthLightning","BloomFire","Wind","Fire","Earth"};
        public float familyCooldown=3, retryInterval=.3f, safetyStep=.1f, safetyMarginR=.025f, safeAreaFraction=.55f, instantWindow=.15f;
        public int safetyDirections=32, areaSamples=160;
    }
    public enum ElementKind { Wind, Fire, Earth, BloomCenter, BloomRay }
    [Serializable] public sealed class ElementHazard
    {
        public int id, group; public ElementKind kind; public Vector3 position,end,direction;
        public float age,warning,duration,radius,damage; public bool active;
    }
    public sealed partial class TribulationModel
    {
        readonly List<ElementHazard> elements=new List<ElementHazard>();
        readonly HashSet<int> hitGroups=new HashSet<int>();
        readonly Dictionary<string,float> familyTime=new Dictionary<string,float>();
        public IReadOnlyList<ElementHazard> Elements => elements;
        public string LastDamageSource {get;private set;}="";
        public string FinalCue {get;private set;}="";
        public Vector3 WindDisplacement {get;private set;}
        public int FinalSection => Stage!=3?0:Elapsed-finalStarted<Config.final.introEnd?1:Elapsed-finalStarted<Config.final.comboEnd?2:3;
        float finalStarted,fireNext,retryAt; int cueIndex; string lastRecipe="";
        void ResetFinal() { elements.Clear();hitGroups.Clear();familyTime.Clear();finalStarted=0;cueIndex=0;fireNext=0;retryAt=0;lastRecipe="";LastDamageSource="";FinalCue="";WindDisplacement=Vector3.zero; }
        void TakeDamage(float damage,string source)
        {
            if(DashRemaining>0) {Event?.Invoke("dash_evaded",Position);return;}
            LastDamageSource=source;
            if(ShieldRemaining>0 && ShieldAmount>0)
            {
                float absorbed=Mathf.Min(damage,ShieldAmount);ShieldAmount-=absorbed;damage-=absorbed;
                Event?.Invoke("shield_absorbed",Position);
                if(ShieldAmount<=0) {ShieldRemaining=0;Event?.Invoke("shield_broken",Position);}
            }
            if(damage>0) { HP=Mathf.Max(0,HP-damage);Event?.Invoke("damaged",Position); }
        }
        void ApplyWind(float dt,float dashStep)
        {
            Vector3 before=Position;
            foreach(var h in elements) if(h.kind==ElementKind.Wind && h.age>=h.warning && h.age<h.warning+h.duration)
                Position=Vector3.ClampMagnitude(Position+h.direction*(Config.arenaRadius*Config.final.windDistanceR/h.duration)*Mathf.Max(0,Mathf.Min(dt,h.warning+h.duration-h.age)-dashStep),Config.arenaRadius);
            WindDisplacement+=Position-before;
        }
        public static float SegmentDistance(Vector3 p,Vector3 a,Vector3 b)
        {var ab=b-a;float t=ab.sqrMagnitude<.0001f?0:Mathf.Clamp01(Vector3.Dot(p-a,ab)/ab.sqrMagnitude);return Vector3.Distance(p,a+ab*t);}
        bool Contains(ElementHazard h,Vector3 p,float margin)
        {return h.kind==ElementKind.Wind?false:(h.kind==ElementKind.Earth||h.kind==ElementKind.BloomRay?SegmentDistance(p,h.position,h.end):Vector3.Distance(p,h.position))<=h.radius+margin;}
        void TickElements(float dt)
        {
            bool fire=false;
            for(int i=elements.Count-1;i>=0;i--)
            {
                var h=elements[i];h.age+=dt;
                if(h.age>=h.warning && !h.active) {h.active=true;Event?.Invoke("active_"+h.kind.ToString().ToLowerInvariant(),h.position);}
                if(h.active && h.age<h.warning+h.duration && Contains(h,Position,Config.hitRadius))
                {
                    if(h.kind==ElementKind.Fire) fire=true;
                    else if(!hitGroups.Contains(h.group) && grace<=0)
                    {
                        if(DashRemaining>0) Event?.Invoke("dash_evaded",Position);
                        else {hitGroups.Add(h.group);TakeDamage(h.damage,h.kind==ElementKind.Earth?"地劫 · 裂缝": "散花雷 · "+(h.kind==ElementKind.BloomCenter?"中心":"雷带"));grace=Config.hitGrace;}
                    }
                }
                if(h.age>=h.warning+h.duration) {elements.RemoveAt(i);Event?.Invoke("ended_"+h.kind.ToString().ToLowerInvariant(),h.position);}
            }
            if(fire && Elapsed>=fireNext) {fireNext=Elapsed+Config.final.fireTick;TakeDamage(Config.final.fireDamage,"火劫 · 持续火区");Event?.Invoke("fire_tick",Position);}
        }
        string Family(ElementKind kind) => kind==ElementKind.BloomCenter||kind==ElementKind.BloomRay?"Bloom":kind.ToString();
        void TickFinalDirector()
        {
            float t=Elapsed-finalStarted;var f=Config.final;
            if(Elapsed<retryAt)return;
            if(cueIndex<f.cueTimes.Length && t>=f.cueTimes[cueIndex])
            {
                string recipe=f.cues[cueIndex];
                if(TryRecipe(recipe)) {cueIndex++;} else {retryAt=Elapsed+f.retryInterval;return;}
            }
            if(t>=f.divineStart+FinalWaves*f.divineInterval && FinalWaves<Config.finalWaves)
            {
                int before=strikes.Count;
                SpawnStrike(StrikeKind.Divine,Config.stages[3],false,FinalWaves%2==0);
                if(strikes.Count>before && !FinalSafety()) {strikes.RemoveAt(strikes.Count-1);retryAt=Elapsed+f.retryInterval;Event?.Invoke("final_safety_deferred",Position);return;}
                if(strikes.Count>before) {FinalWaves++;Event?.Invoke("divine_wave",Position);}
                else retryAt=Elapsed+f.retryInterval;
            }
        }
        bool TryRecipe(string recipe)
        {
            var f=Config.final;var families=new HashSet<string>();foreach(var h in elements)families.Add(Family(h.kind));if(strikes.Count>0)families.Add("Lightning");
            foreach(string name in new[]{"Wind","Fire","Earth","Bloom","Lightning"}) if(recipe.Contains(name))
            {
                families.Add(name);
                if(familyTime.TryGetValue(name,out float when) && Elapsed-when<f.familyCooldown)return false;
            }
            if(families.Count>f.maxSystems || recipe==lastRecipe)return false;
            // Reject capped recipes before adding any member (WindFire must be atomic).
            int fireCount=0;foreach(var h in elements)if(h.kind==ElementKind.Fire)fireCount++;
            if(recipe.Contains("Fire") && fireCount>=f.fireLimit)return false;
            int count=elements.Count,lightningCount=strikes.Count;int group=++nextId;
            var forward=velocity.sqrMagnitude>.1f?velocity.normalized:Vector3.forward;
            var side=new Vector3(-forward.z,0,forward.x);float R=Config.arenaRadius;
            var center=Vector3.ClampMagnitude(Position+velocity*f.placementLead,R*f.centerLimitR);
            if(recipe.Contains("Wind")) AddElement(ElementKind.Wind,group,Position,Position,side,f.windWarning,f.windDuration,0,0);
            if(recipe.Contains("Fire"))
            {
                var p=recipe.Contains("Wind")?Vector3.ClampMagnitude(Position+side*R*f.pairOffsetR,R*(1-f.fireRadiusR)):center;
                AddElement(ElementKind.Fire,group,p,p,Vector3.zero,f.fireWarning,f.fireDuration,R*f.fireRadiusR,f.fireDamage);
            }
            if(recipe.Contains("Earth"))
            {
                var p=Vector3.ClampMagnitude(Position+forward*R*f.earthOffsetR,R*f.centerLimitR);
                AddElement(ElementKind.Earth,group,p-side*R*f.earthLengthR/2,p+side*R*f.earthLengthR/2,Vector3.zero,f.earthWarning,f.earthDuration,R*f.earthWidthR/2,f.earthDamage);
            }
            if(recipe.Contains("Bloom"))
            {
                int bloomGroup=++nextId;
                AddElement(ElementKind.BloomCenter,bloomGroup,center,center,Vector3.zero,f.bloomWarning,f.instantWindow,R*f.bloomCenterRadiusR,f.bloomCenterDamage);
                int rays=FinalSection==3?f.lateBloomRays:f.bloomRays;
                for(int i=0;i<rays;i++) {float a=i*Mathf.PI*2/rays;var end=center+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*R*f.bloomLengthR;AddElement(ElementKind.BloomRay,bloomGroup,center,end,Vector3.zero,f.bloomWarning+f.bloomDelay,f.bloomDuration,R*f.bloomWidthR/2,f.bloomRayDamage);}
            }
            if(recipe.Contains("Lightning")) SpawnStrike(StrikeKind.Normal,Config.stages[3],false);
            if(!FinalSafety()) {elements.RemoveRange(count,elements.Count-count);if(strikes.Count>lightningCount)strikes.RemoveRange(lightningCount,strikes.Count-lightningCount);Event?.Invoke("final_safety_deferred",Position);return false;}
            lastRecipe=recipe;FinalCue=recipe;
            foreach(string name in new[]{"Wind","Fire","Earth","Bloom","Lightning"})if(recipe.Contains(name))familyTime[name]=Elapsed;
            Event?.Invoke("final_cue_"+recipe.ToLowerInvariant(),Position);return true;
        }
        void AddElement(ElementKind kind,int group,Vector3 p,Vector3 end,Vector3 dir,float warning,float duration,float radius,float damage)
        {elements.Add(new ElementHazard{id=++nextId,group=group,kind=kind,position=p,end=end,direction=dir,warning=warning,duration=duration,radius=radius,damage=damage});}
        bool FinalSafety()
        {
            var f=Config.final;var families=new HashSet<string>();foreach(var h in elements)families.Add(Family(h.kind));if(strikes.Count>0)families.Add("Lightning");if(families.Count>f.maxSystems)return false;
            int safe=0;float R=Config.arenaRadius,margin=Config.hitRadius+R*f.safetyMarginR;
            for(int i=0;i<f.areaSamples;i++)
            {
                float a=i*2.399963f,r=R*Mathf.Sqrt((i+.5f)/f.areaSamples);var p=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*r;bool blocked=false;
                foreach(var h in elements)if(Contains(h,p,margin)){blocked=true;break;}
                foreach(var h in strikes)if(Vector3.Distance(p,h.position)<h.radius+margin)blocked=true;
                if(!blocked)safe++;
            }
            if((float)safe/f.areaSamples<f.safeAreaFraction)return false;
            for(int i=0;i<f.safetyDirections;i++)
            {
                float a=i*Mathf.PI*2/f.safetyDirections;var v=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*Config.moveSpeed;var p=Position;bool clear=true;
                float horizon=0;foreach(var h in elements)horizon=Mathf.Max(horizon,h.warning+h.duration-h.age);foreach(var h in strikes)horizon=Mathf.Max(horizon,h.warning+h.lingering-h.age);
                for(float t=f.safetyStep;t<=horizon+f.safetyStep;t+=f.safetyStep)
                {
                    var push=Vector3.zero;foreach(var h in elements)if(h.kind==ElementKind.Wind && h.age+t>=h.warning && h.age+t<h.warning+h.duration)push+=h.direction*R*f.windDistanceR/h.duration;
                    p=Vector3.ClampMagnitude(p+(v+push)*f.safetyStep,R);
                    foreach(var h in elements)if(h.age+t>=h.warning-f.safetyStep && h.age+t<=h.warning+h.duration && Contains(h,p,margin)){clear=false;break;}
                    foreach(var h in strikes)if(h.age+t>=h.warning-f.safetyStep && h.age+t<=h.warning+Mathf.Max(f.instantWindow,h.lingering) && Vector3.Distance(p,h.position)<h.radius+margin)clear=false;
                    if(!clear)break;
                }
                if(clear)return true;
            }
            return false;
        }
    }
}
