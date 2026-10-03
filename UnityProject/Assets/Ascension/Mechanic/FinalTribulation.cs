using System;
using System.Collections.Generic;
using UnityEngine;
namespace Ascension
{
    [Serializable] public sealed class FinalBalance
    {
        public float windWarning=1.5f, windDuration=1.2f, windDistanceR=.19f;
        public float fireWarning=1.2f, fireDuration=3.5f, fireRadiusR=.10f, fireTick=.5f, fireDamage=5;
        public int fireLimit=5, waterLimit=4, ballLimit=3, maxSystems=2;
        public int[] fireCounts={0,3,4,5}, waterCounts={0,2,3,3}, ballCounts={0,1,2,3};
        public float waterWarning=.9f, waterDuration=5, waterRadiusR=.1f, waterSpeed=.7f;
        public float ballWarning=.9f, ballDuration=15, ballRadiusR=.05f, ballSpeed=.45f, ballDamage=13, ballHitCooldown=.9f, ballTurnRate=2, areaInterval=8;
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
    public enum ElementKind { Wind, Fire, Earth, BloomCenter, BloomRay, Water, Ball }
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
        // Cue time is normalized; warnings, movement and damage always use real seconds.
        float FinalTime => (Elapsed-finalStarted)*60/Config.stages[3].duration;
        public int FinalSection => Stage!=3?0:FinalTime<Config.final.introEnd?1:FinalTime<Config.final.comboEnd?2:3;
        public float MovementMultiplier => WaterMultiplier(Position,0);
        float finalStarted,fireNext,ballNext,retryAt,nextArea,nextBall; int cueIndex,areaIndex; string lastRecipe="";
        void ResetFinal() { elements.Clear();hitGroups.Clear();familyTime.Clear();finalStarted=0;cueIndex=0;fireNext=0;ballNext=0;retryAt=0;nextArea=0;nextBall=0;areaIndex=0;lastRecipe="";LastDamageSource="";FinalCue="";WindDisplacement=Vector3.zero; }
        float WaterMultiplier(Vector3 p,float future)
        {
            foreach(var h in elements)if(h.kind==ElementKind.Water && h.age+future>=h.warning && h.age+future<h.warning+h.duration && Contains(h,p,Config.hitRadius))return Config.final.waterSpeed;
            return 1;
        }
        int ElementCount(ElementKind kind) {int n=0;foreach(var h in elements)if(h.kind==kind)n++;return n;}
        Vector3 BallWaypoint()
        {
            float a=(float)random.NextDouble()*Mathf.PI*2,r=Config.arenaRadius*(.2f+.65f*(float)random.NextDouble());
            return new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*r;
        }
        void TickBallDirector()
        {
            if(Stage==0 || Elapsed<nextBall)return;
            var f=Config.final;
            if(ElementCount(ElementKind.Ball)>=Mathf.Min(3,Mathf.Min(f.ballLimit,f.ballCounts[Stage])))return;
            nextBall=Elapsed+1.2f;
            for(int attempt=0;attempt<16;attempt++)
            {
                var p=BallWaypoint();float radius=Config.arenaRadius*f.ballRadiusR;
                if(Vector3.Distance(p,Position)<radius+Config.hitRadius+3)continue;
                bool clear=true;foreach(var h in elements)if(Contains(h,p,radius+1))clear=false;
                if(!clear)continue;
                var target=BallWaypoint();
                AddElement(ElementKind.Ball,++nextId,p,target,(target-p).normalized,f.ballWarning,f.ballDuration,radius,f.ballDamage);
                Event?.Invoke("final_cue_ball",p);return;
            }
        }
        void MoveBall(ElementHazard h,float dt)
        {
            if(Vector3.Distance(h.position,h.end)<1.5f)h.end=BallWaypoint();
            h.direction=Vector3.RotateTowards(h.direction,(h.end-h.position).normalized,Config.final.ballTurnRate*dt,0).normalized;
            var next=h.position+h.direction*Config.moveSpeed*Config.final.ballSpeed*dt;
            float edge=Config.arenaRadius-h.radius;
            if(next.magnitude>edge)
            {
                h.direction=Vector3.Reflect(h.direction,next.normalized).normalized;
                h.end=-next.normalized*edge*.5f;
                next=h.position+h.direction*Config.moveSpeed*Config.final.ballSpeed*dt;
            }
            h.position=Vector3.ClampMagnitude(next,edge);
        }
        void TickAreaDirector()
        {
            if(Stage<1 || Stage==3 || Elapsed<nextArea)return;
            string[] recipes=Stage==1?new[]{"WaterLightning","FireLightning"}:new[]{"WindLightning","WaterLightning","FireLightning"};
            if(TryRecipe(recipes[areaIndex%recipes.Length])){areaIndex++;nextArea=Elapsed+Config.final.areaInterval;}
            else nextArea=Elapsed+Config.final.retryInterval;
        }
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
            bool fire=false,ball=false;
            for(int i=elements.Count-1;i>=0;i--)
            {
                var h=elements[i];h.age+=dt;
                if(h.age>=h.warning && !h.active) {h.active=true;Event?.Invoke("active_"+h.kind.ToString().ToLowerInvariant(),h.position);}
                if(h.active && h.kind==ElementKind.Ball)MoveBall(h,Mathf.Min(dt,h.age-h.warning));
                if(h.active && h.age<h.warning+h.duration && Contains(h,Position,Config.hitRadius))
                {
                    if(h.kind==ElementKind.Fire) fire=true;
                    else if(h.kind==ElementKind.Water) { /* Slow only, never damage or shield consumption. */ }
                    else if(h.kind==ElementKind.Ball) ball=true;
                    else if(!hitGroups.Contains(h.group) && grace<=0)
                    {
                        if(DashRemaining>0) Event?.Invoke("dash_evaded",Position);
                        else {hitGroups.Add(h.group);TakeDamage(h.damage,h.kind==ElementKind.Earth?"地劫 · 裂缝": "散花雷 · "+(h.kind==ElementKind.BloomCenter?"中心":"雷带"));grace=Config.hitGrace;}
                    }
                }
                if(h.age>=h.warning+h.duration) {elements.RemoveAt(i);Event?.Invoke("ended_"+h.kind.ToString().ToLowerInvariant(),h.position);}
            }
            if(fire && Elapsed>=fireNext) {fireNext=Elapsed+Config.final.fireTick;TakeDamage(Config.final.fireDamage,"火劫 · 持续火区");Event?.Invoke("fire_tick",Position);}
            if(ball && Elapsed>=ballNext) {ballNext=Elapsed+Config.final.ballHitCooldown;TakeDamage(Config.final.ballDamage,"球形闪电 · 接触");Event?.Invoke("ball_contact",Position);}
        }
        string Family(ElementKind kind) => kind==ElementKind.BloomCenter||kind==ElementKind.BloomRay?"Bloom":kind.ToString();
        void TickFinalDirector()
        {
            float t=FinalTime;var f=Config.final;
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
            // A wind recipe is indivisible: every wind has a locked, visible lightning partner.
            if(recipe.Contains("Wind") && !recipe.Contains("Lightning"))recipe+="Lightning";
            var f=Config.final;var families=new HashSet<string>();foreach(var h in elements)if(h.kind!=ElementKind.Ball)families.Add(Family(h.kind));if(strikes.Count>0)families.Add("Lightning");
            foreach(string name in new[]{"Wind","Fire","Water","Earth","Bloom","Lightning"}) if(recipe.Contains(name))
            {
                families.Add(name);
                if(familyTime.TryGetValue(name,out float when) && Elapsed-when<f.familyCooldown)return false;
            }
            if(families.Count>f.maxSystems || recipe==lastRecipe)return false;
            // Reject capped recipes before adding any member (WindFire must be atomic).
            int fires=Mathf.Min(5,Mathf.Min(f.fireLimit,f.fireCounts[Stage]));
            int waters=Mathf.Min(4,Mathf.Min(f.waterLimit,f.waterCounts[Stage]));
            if(recipe.Contains("Fire") && ElementCount(ElementKind.Fire)+fires>Mathf.Min(5,f.fireLimit))return false;
            if(recipe.Contains("Water") && ElementCount(ElementKind.Water)+waters>Mathf.Min(4,f.waterLimit))return false;
            int count=elements.Count,lightningCount=strikes.Count;int group=++nextId;
            var forward=velocity.sqrMagnitude>.1f?velocity.normalized:Vector3.forward;
            var side=new Vector3(-forward.z,0,forward.x);float R=Config.arenaRadius;
            var center=Vector3.ClampMagnitude(Position+velocity*f.placementLead,R*f.centerLimitR);
            if(recipe.Contains("Wind")) AddElement(ElementKind.Wind,group,Position,Position,side,f.windWarning,f.windDuration,0,0);
            if(recipe.Contains("Fire"))
            {
                if(!AddAreas(ElementKind.Fire,group,fires,R*f.fireRadiusR,f.fireWarning,f.fireDuration,f.fireDamage,forward))return false;
            }
            if(recipe.Contains("Water") && !AddAreas(ElementKind.Water,group,waters,R*f.waterRadiusR,f.waterWarning,f.waterDuration,0,forward))return false;
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
            if(recipe.Contains("Lightning"))
            {
                var rule=Config.stages[Stage];
                // The lightning locks immediately and resolves before the wind starts pushing.
                var paired=new StageRule{warning=1.2f,radius=rule.radius,damage=rule.damage};
                SpawnStrike(areaIndex%2==0?StrikeKind.Normal:StrikeKind.Predictive,paired,false,areaIndex%2!=0);
                if(strikes.Count==lightningCount){elements.RemoveRange(count,elements.Count-count);return false;}
            }
            if(!FinalSafety()) {elements.RemoveRange(count,elements.Count-count);if(strikes.Count>lightningCount)strikes.RemoveRange(lightningCount,strikes.Count-lightningCount);Event?.Invoke("final_safety_deferred",Position);return false;}
            lastRecipe=recipe;FinalCue=recipe;
            foreach(string name in new[]{"Wind","Fire","Water","Earth","Bloom","Lightning"})if(recipe.Contains(name))familyTime[name]=Elapsed;
            Event?.Invoke("final_cue_"+recipe.ToLowerInvariant(),Position);return true;
        }
        bool AddAreas(ElementKind kind,int group,int amount,float radius,float warning,float duration,float damage,Vector3 forward)
        {
            int before=elements.Count;float R=Config.arenaRadius;
            // Small separated clusters leave the rear half-plane open. No ring around the player.
            var side=new Vector3(-forward.z,0,forward.x);
            for(int i=0;i<amount;i++)
            {
                bool placed=false;
                for(int attempt=0;attempt<24;attempt++)
                {
                    var p=i==0 && attempt==0?Vector3.ClampMagnitude(Position+forward*2,R-radius):
                        Vector3.ClampMagnitude(Position+forward*(2+(float)random.NextDouble()*7)+side*((float)random.NextDouble()*2-1)*8,R-radius);
                    bool clear=true;foreach(var h in elements)if((h.kind==ElementKind.Fire||h.kind==ElementKind.Water) && Vector3.Distance(p,h.position)<radius+h.radius+1.1f)clear=false;
                    if(!clear)continue;
                    AddElement(kind,group,p,p,Vector3.zero,warning,duration,radius,damage);placed=true;break;
                }
                if(!placed){elements.RemoveRange(before,elements.Count-before);return false;}
            }
            return true;
        }
        void AddElement(ElementKind kind,int group,Vector3 p,Vector3 end,Vector3 dir,float warning,float duration,float radius,float damage)
        {elements.Add(new ElementHazard{id=++nextId,group=group,kind=kind,position=p,end=end,direction=dir,warning=warning,duration=duration,radius=radius,damage=damage});}
        bool FinalSafety()
        {
            var f=Config.final;var families=new HashSet<string>();foreach(var h in elements)if(h.kind!=ElementKind.Ball)families.Add(Family(h.kind));if(strikes.Count>0)families.Add("Lightning");if(families.Count>f.maxSystems)return false;
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
                float horizon=0;foreach(var h in elements)if(h.kind!=ElementKind.Ball)horizon=Mathf.Max(horizon,h.warning+h.duration-h.age);foreach(var h in strikes)horizon=Mathf.Max(horizon,h.warning+h.lingering-h.age);
                for(float t=f.safetyStep;t<=horizon+f.safetyStep;t+=f.safetyStep)
                {
                    var push=Vector3.zero;foreach(var h in elements)if(h.kind==ElementKind.Wind && h.age+t>=h.warning && h.age+t<h.warning+h.duration)push+=h.direction*R*f.windDistanceR/h.duration;
                    p=Vector3.ClampMagnitude(p+(v*WaterMultiplier(p,t)+push)*f.safetyStep,R);
                    foreach(var h in elements)
                    {
                        if(h.kind==ElementKind.Water)continue;
                        bool inside=h.kind==ElementKind.Ball?Vector3.Distance(p,Vector3.ClampMagnitude(h.position+h.direction*Config.moveSpeed*f.ballSpeed*Mathf.Min(t,2),R-h.radius))<h.radius+margin:Contains(h,p,margin);
                        if(h.age+t>=h.warning-f.safetyStep && h.age+t<=h.warning+h.duration && inside){clear=false;break;}
                    }
                    foreach(var h in strikes)if(h.age+t>=h.warning-f.safetyStep && h.age+t<=h.warning+Mathf.Max(f.instantWindow,h.lingering) && Vector3.Distance(p,h.position)<h.radius+margin)clear=false;
                    if(!clear)break;
                }
                if(clear)return true;
            }
            return false;
        }
    }
}
