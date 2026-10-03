using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Ascension.Editor
{
    // Editor-only deterministic regression check. Native player runs remain the playtest evidence.
    public static class V11Checks
    {
        static void Check(bool condition,string message) {if(!condition)throw new Exception("V1.1 check: "+message);}
        static Balance Config() => JsonUtility.FromJson<Balance>(Resources.Load<TextAsset>("Balance").text);
        static ElementHazard Water(int id) => new ElementHazard{id=id,kind=ElementKind.Water,position=Vector3.zero,radius=2,warning=0,duration=5,active=true};
        public static void Validate()
        {
            CheckTelegraphTiming();
            var c=Config();Check(Mathf.Abs(c.TotalDuration-180)<.01f,"Formal must be 180s");
            float fast=0;foreach(float t in c.fastDurations)fast+=t;Check(fast>=70&&fast<=90,"Fast duration");
            var m=new TribulationModel(c);m.Start();var areas=(List<ElementHazard>)m.Elements;
            areas.Add(Water(1000));areas.Add(Water(1001));m.Tick(.1f,Vector3.right);
            Check(Mathf.Abs(m.Position.x-.42f)<.001f,"water slow must not stack");Check(m.HP==100,"water damage");
            m.Shield();Check(m.MovementMultiplier==.7f,"shield must not remove slow");
            m.Dash(Vector3.right);float before=m.Position.x;m.Tick(.1f,Vector3.right);
            Check(Mathf.Abs(m.Position.x-before-2.4f)<.001f,"water must not slow dash");
            Check(m.MovementMultiplier==1,"leaving water restores speed");
            float elapsed=m.Elapsed;m.TogglePause();m.Tick(.1f,Vector3.right);Check(m.Elapsed==elapsed,"pause");
            m.Start();Check(m.Elements.Count==0&&m.Qi==c.initialQi&&m.HP==c.maxHP,"restart reset");
            areas.Add(new ElementHazard{id=1002,kind=ElementKind.Ball,position=Vector3.zero,end=Vector3.forward*10,direction=Vector3.forward,radius=.8f,warning=0,duration=15,damage=13,active=true});
            m.Tick(.05f,Vector3.zero);Check(m.HP==87,"ball contact damage");
            m.Tick(.05f,Vector3.zero);Check(m.HP==87,"ball contact cooldown");
            var seen=new HashSet<ElementKind>();int maxFire=0,maxWater=0,maxBall=0,randomWarnings=0,impactChecks=0;var randomStages=new HashSet<int>();
            for(int seed=0;seed<12;seed++)
            {
                c=Config();c.seed=24130+seed;c.maxHP=100000; // Coverage only: never presented as a survival result.
                if(seed>=6)for(int i=0;i<4;i++)c.stages[i].duration=c.fastDurations[i];
                m=new TribulationModel(c);m.Start();var positions=new Dictionary<int,Vector3>();
                var born=new Dictionary<int,float>();var hit=new HashSet<int>();
                m.Event+=(key,p)=>{
                    if(key=="final_cue_windlightning")Check(m.Strikes.Count>0,"wind missing lightning partner");
                    if(key.StartsWith("warning_"))foreach(var strike in m.Strikes)if(!born.ContainsKey(strike.id))
                    {
                        born[strike.id]=m.Elapsed;
                        Check(strike.warning>=1.19f&&strike.warning<=1.401f,"warning timing range");
                        if(strike.kind==StrikeKind.RandomGround){randomWarnings++;randomStages.Add(m.Stage);Check(strike.position.magnitude+strike.radius<c.arenaRadius,"random circle crosses edge");}
                    }
                    if(key=="lightning")foreach(var strike in m.Strikes)if(strike.impacted && hit.Add(strike.id))
                    {Check(born.ContainsKey(strike.id)&&m.Elapsed-born[strike.id]>=strike.warning-.001f,"impact before telegraph duration");impactChecks++;}
                };
                while(m.Status==RunStatus.Playing)
                {
                    float angle=m.Elapsed*.65f;var move=seed%2==0?new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle)):Vector3.zero;
                    m.Tick(.05f,move);int fire=0,water=0,ball=0;var families=new HashSet<string>();
                    foreach(var h in m.Elements)
                    {
                        seen.Add(h.kind);if(h.kind==ElementKind.Fire)fire++;if(h.kind==ElementKind.Water)water++;
                        if(h.kind==ElementKind.Ball)
                        {
                            ball++;Check(h.position.magnitude<=c.arenaRadius-h.radius+.001f,"ball bounds");
                            if(positions.TryGetValue(h.id,out var p))Check(Vector3.Distance(p,h.position)<=c.moveSpeed*c.final.ballSpeed*.05f+.002f,"ball teleport");
                            positions[h.id]=h.position;
                        }
                        else families.Add(h.kind==ElementKind.BloomRay?"BloomCenter":h.kind.ToString());
                    }
                    if(m.Strikes.Count>0)families.Add("Lightning");
                    int randomCount=0;foreach(var strike in m.Strikes)if(strike.kind==StrikeKind.RandomGround)
                    {
                        randomCount++;foreach(var other in m.Strikes)if(other.id>strike.id&&other.kind==StrikeKind.RandomGround)Check(Vector3.Distance(strike.position,other.position)>=strike.radius+other.radius+.49f,"random overlap");
                    }
                    Check(randomCount<=c.randomLimits[m.Stage],"random cap");
                    Check(families.Count<=2,"more than two main families");
                    Check(fire<=5&&water<=4&&ball<=3,"hazard hard cap");
                    maxFire=Math.Max(maxFire,fire);maxWater=Math.Max(maxWater,water);maxBall=Math.Max(maxBall,ball);
                }
                Check(m.Status==RunStatus.Won&&m.FinalWaves==9,"all nine divine waves before victory, seed "+c.seed+" waves "+m.FinalWaves);
            }
            Check(maxFire==5&&maxWater==3&&maxBall==3,"late-game count coverage");
            Check(seen.Count==7,"all hazard kinds covered");
            Check(randomWarnings>0&&randomStages.Contains(0)&&randomStages.Contains(1)&&randomStages.Contains(2),"early random stage coverage");
            string root=Directory.GetParent(Application.dataPath).Parent.FullName;
            File.WriteAllText(Path.Combine(root,"Docs/Evidence/v11-r1-model-checks.json"),"{\"passed\":true,\"coverageRuns\":12,\"nativePlaytest\":false,\"randomWarnings\":"+randomWarnings+",\"impactTimingChecks\":"+impactChecks+"}");
            File.WriteAllText(Path.Combine(root,"Docs/Evidence/v11-model-checks.json"),"{\"passed\":true,\"coverageRuns\":12,\"nativePlaytest\":false,\"maxFire\":"+maxFire+",\"maxWater\":"+maxWater+",\"maxBall\":"+maxBall+"}");
            Debug.Log("[V1.1] model checks passed (12 coverage runs; not player survival evidence)");
        }
        static void CheckTelegraphTiming()
        {
            var c=Config();c.phased=false;var m=new TribulationModel(c);m.Start();
            var strikes=(List<Strike>)m.Strikes;
            var tracking=new Strike{id=1000,kind=StrikeKind.Tracking,trackTime=c.trackingTime,warning=c.trackingTime+c.lockTime,radius=c.trackingRadius,damage=18};strikes.Add(tracking);
            while(m.Elapsed<.91f)m.Tick(.01f,Vector3.right);
            Check(tracking.locked,"tracking lock at .9s");var locked=tracking.position;
            while(m.Elapsed<1.39f)m.Tick(.01f,Vector3.right);
            Check(!tracking.impacted&&m.HP==100,"no tracking VFX/damage before1.4s");Check(tracking.position==locked,"tracking moved after lock");
            m.Tick(.02f,Vector3.right);Check(tracking.impacted&&m.HP==100,"walk out of tracking without skills");
            m.Start();strikes=(List<Strike>)m.Strikes;var normal=new Strike{id=1000,kind=StrikeKind.Normal,warning=1.3f,radius=2,damage=18,locked=true};strikes.Add(normal);
            while(m.Elapsed<1.28f)m.Tick(.01f,Vector3.zero);Check(m.HP==100&&!normal.impacted,"no early normal damage");
            m.Tick(.03f,Vector3.zero);Check(normal.impacted&&m.HP==82,"normal impact and damage same tick");
        }
    }
}
