using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ascension
{
    [Serializable]
    public sealed class Balance
    {
        public float maxHP = 100, moveSpeed = 6, arenaRadius = 16, hitRadius = .45f;
        public float duration = 45, warningTime = 1.5f, strikeInterval = 3.2f, damage = 18, strikeRadius = 2.3f;
        public float hitGrace = .65f;
        public bool phased;
        public float maxQi = 100, initialQi = 60, dashCost = 18, dashCooldown = 2.5f, dashDuration = .22f, dashSpeed = 24;
        public float shieldCost = 30, shieldCooldown = 6, shieldDuration = 2, shieldCapacity = 45;
        public float qiValue = 20, qiLifetime = 18, pickupRadius = 1.2f, trackingTime = .8f, lockTime = .85f, chainInterval = .5f;
        public int qiLimit = 6, strikeLimit = 6, chainCount = 3, finalWaves = 9, seed = 24130;
        public float riskDropChance = .25f;
        public float predictionLead = 1.2f, blockingDuration = 1.8f;
        public FinalBalance final = new FinalBalance();
        public Technique[] techniques = {new Technique()};
        public float[] fastDurations = {18,24,30,30};
        public StageRule[] stages = {
            new StageRule { duration=90, interval=3.2f, warning=1.5f, damage=18, radius=2.3f, qiInterval=8 },
            new StageRule { duration=120, interval=2.8f, warning=1.3f, damage=22, radius=2.5f, qiInterval=6, chainChance=.5f },
            new StageRule { duration=120, interval=2.4f, warning=1.1f, damage=26, radius=2.7f, qiInterval=9, chainChance=.35f, trackingChance=.3f },
            new StageRule { duration=30, interval=3.2f, warning=1.1f, damage=32, radius=3, qiInterval=10 }
        };
        public float TotalDuration { get { if(!phased) return duration; float t=0; foreach(var s in stages) t+=s.duration; return t; } }
    }
    [Serializable] public sealed class StageRule
    {
        public float duration, interval, warning, damage, radius, qiInterval, chainChance, trackingChance, predictiveChance, blockingChance;
    }
    public enum RunStatus { Menu, Playing, Paused, Lost, Won }
    public enum StrikeKind { Normal, Chain, Tracking, Divine, Predictive, Blocking }
    [Serializable]
    public sealed class Strike
    {
        public int id;
        public Vector3 position;
        public float age, warning, radius, damage;
        public float trackTime;
        public StrikeKind kind;
        public bool locked, impacted;
        public float lingering;
    }
    [Serializable] public sealed class QiOrb { public int id; public Vector3 position; public float age; public bool risky; }
    public sealed partial class TribulationModel
    {
        public Balance Config { get; }
        public RunStatus Status { get; private set; } = RunStatus.Menu;
        public float HP { get; private set; }
        public float Elapsed { get; private set; }
        public Vector3 Position { get; private set; }
        public float Qi { get; private set; }
        public float DashCooldown { get; private set; }
        public float DashRemaining { get; private set; }
        public float ShieldCooldown { get; private set; }
        public float ShieldRemaining { get; private set; }
        public float ShieldAmount { get; private set; }
        public int Stage { get; private set; }
        public int FinalWaves { get; private set; }
        public bool CanDash => Status == RunStatus.Playing && DashCooldown <= 0 && Qi >= Config.dashCost;
        public bool CanShield => Status == RunStatus.Playing && ShieldCooldown <= 0 && Qi >= Config.shieldCost;
        public float Remaining => Mathf.Max(0, Config.TotalDuration-Elapsed);
        public IReadOnlyList<Strike> Strikes => strikes;
        public IReadOnlyList<QiOrb> Orbs => orbs;
        public event Action<string, Vector3> Event;
        readonly List<Strike> strikes = new List<Strike>();
        readonly List<QiOrb> orbs = new List<QiOrb>();
        float nextStrike, grace, nextQi, chainTimer;
        int chainLeft;
        StageRule chainRule;
        Vector3 dashDirection, velocity;
        System.Random random;
        int nextId;

        public TribulationModel(Balance config) { Config = config; HP = config.maxHP; Qi=config.initialQi; random=new System.Random(config.seed); }
        public void Start()
        {
            ResetFinal(); HP = Config.maxHP; Elapsed = 0; Position = Vector3.zero;
            velocity=Vector3.zero; grace = 0; nextStrike = 2; nextId = 0; strikes.Clear();
            Qi=Config.initialQi; DashCooldown=0; DashRemaining=0; ShieldCooldown=0; ShieldRemaining=0; ShieldAmount=0;
            Stage=0; FinalWaves=0; nextQi=Config.stages[0].qiInterval; chainLeft=0; chainTimer=0; orbs.Clear(); random=new System.Random(Config.seed);
            if(Config.phased) { AddOrb(new Vector3(0,0,3),false); AddOrb(new Vector3(4,0,0),false); AddOrb(new Vector3(-4,0,0),false); }
            Status = RunStatus.Playing; Event?.Invoke("started", Position);
        }
        public void TogglePause()
        {
            if (Status == RunStatus.Playing) { Status = RunStatus.Paused; Event?.Invoke("paused",Position); }
            else if (Status == RunStatus.Paused) { Status = RunStatus.Playing; Event?.Invoke("resumed",Position); }
        }
        public bool Dash(Vector3 direction)
        {
            if(!CanDash) { Event?.Invoke("dash_unavailable",Position); return false; }
            direction.y=0; dashDirection=direction.sqrMagnitude>.001f?direction.normalized:Vector3.forward;
            Qi-=Config.dashCost; DashCooldown=Config.dashCooldown; DashRemaining=Config.dashDuration;
            Event?.Invoke("dash",Position); return true;
        }
        public bool Shield()
        {
            if(!CanShield) { Event?.Invoke("shield_unavailable",Position); return false; }
            Qi-=Config.shieldCost; ShieldCooldown=Config.shieldCooldown; ShieldRemaining=Config.shieldDuration; ShieldAmount=Config.shieldCapacity;
            Event?.Invoke("shield",Position); return true;
        }
        public void Tick(float dt, Vector3 movement)
        {
            if (Status != RunStatus.Playing || dt <= 0) return;
            dt = Mathf.Min(dt, .1f);
            movement.y=0;
            velocity=Vector3.ClampMagnitude(movement,1)*Config.moveSpeed;
            float dashStep=Mathf.Min(dt,DashRemaining);
            Position = Vector3.ClampMagnitude(Position + dashDirection*Config.dashSpeed*dashStep + Vector3.ClampMagnitude(movement, 1) * Config.moveSpeed * (dt-dashStep), Config.arenaRadius);
            ApplyWind(dt,dashStep);
            Elapsed += dt; grace = Mathf.Max(0, grace - dt);
            if(Config.phased)
            {
                float boundary=Config.stages[0].duration; int phase=0;
                while(phase<Config.stages.Length-1 && Elapsed>=boundary) { phase++; boundary+=Config.stages[phase].duration; }
                if(phase!=Stage) { Stage=phase; if(Stage==3) { strikes.Clear(); chainLeft=0; finalStarted=Elapsed; } nextStrike=1.5f; nextQi=Mathf.Min(nextQi,Config.stages[Stage].qiInterval); Event?.Invoke("stage_changed",Position); }
            }
            for (int i = strikes.Count - 1; i >= 0; i--)
            {
                var strike = strikes[i];
                if(strike.trackTime>0 && !strike.locked)
                {
                    strike.position=Position;
                    if(strike.age+dt>=strike.trackTime)
                    {
                        strike.locked=true;
                        // Include this newly locked strike even when the frame crosses
                        // trackTime by a fraction; its age advances below, after admission.
                        if(!HasWalkingExit(strike.warning-strike.age)) { strikes.RemoveAt(i); Event?.Invoke("pattern_suppressed",strike.position); continue; }
                        Event?.Invoke("tracking_locked",strike.position);
                    }
                }
                strike.age += dt;
                if (strike.age < strike.warning) continue;
                if(!strike.impacted) { strike.impacted=true; Event?.Invoke("lightning", strike.position); }
                if (Vector3.Distance(Position, strike.position) <= strike.radius + Config.hitRadius && grace <= 0 && DashRemaining <= 0)
                {
                    TakeDamage(strike.damage,"雷劫 / "+strike.kind);
                    grace = Config.hitGrace;
                }
                else if(Vector3.Distance(Position,strike.position)<=strike.radius+Config.hitRadius && DashRemaining>0) Event?.Invoke("dash_evaded",Position);
                if(strike.age>=strike.warning+strike.lingering)
                {
                    if(Config.phased && Stage>=1 && random.NextDouble()<Config.riskDropChance) AddOrb(strike.position,true);
                    strikes.RemoveAt(i);
                }
            }
            TickElements(dt);
            if(Config.phased && Stage==3) TickFinalDirector();
            DashRemaining=Mathf.Max(0,DashRemaining-dt); DashCooldown=Mathf.Max(0,DashCooldown-dt);
            ShieldCooldown=Mathf.Max(0,ShieldCooldown-dt);
            if(ShieldRemaining>0) { ShieldRemaining=Mathf.Max(0,ShieldRemaining-dt); if(ShieldRemaining<=0) { ShieldAmount=0; Event?.Invoke("shield_ended",Position); } }
            if (HP <= 0) { Finish(RunStatus.Lost); return; }
            if (Elapsed >= Config.TotalDuration) { Finish(RunStatus.Won); return; }
            if(Config.phased)
            {
                for(int i=orbs.Count-1;i>=0;i--)
                {
                    var orb=orbs[i]; orb.age+=dt;
                    if(Qi<Config.maxQi && Vector3.Distance(Position,orb.position)<=Config.pickupRadius)
                    { Qi=Mathf.Min(Config.maxQi,Qi+Config.qiValue); orbs.RemoveAt(i); Event?.Invoke("qi_collected",orb.position); }
                    else if(orb.age>=Config.qiLifetime) orbs.RemoveAt(i);
                }
                nextQi-=dt;
                if(nextQi<=0) { nextQi+=Config.stages[Stage].qiInterval; SpawnSafeOrb(); }
                chainTimer-=dt;
                if(chainLeft>0 && chainTimer<=0) { SpawnStrike(StrikeKind.Chain,chainRule,false); chainLeft--; chainTimer=Config.chainInterval; }
            }
            nextStrike -= dt;
            if (nextStrike <= 0)
            {
                if(!Config.phased)
                {
                    nextStrike+=Config.strikeInterval;
                    SpawnStrike(StrikeKind.Normal,new StageRule{warning=Config.warningTime,radius=Config.strikeRadius,damage=Config.damage},false);
                }
                else
                {
                    var rule=Config.stages[Stage]; nextStrike+=rule.interval;
                    if(Stage==3) { /* The configured Final Director owns this stage. */ }
                    else
                    {
                        double roll=random.NextDouble();
                        if(roll<rule.blockingChance) SpawnBlocking(rule);
                        else if(roll<rule.blockingChance+rule.predictiveChance) SpawnStrike(StrikeKind.Predictive,rule,false,true);
                        else if(roll<rule.blockingChance+rule.predictiveChance+rule.trackingChance) SpawnStrike(StrikeKind.Tracking,rule,true);
                        else if(roll<rule.blockingChance+rule.predictiveChance+rule.trackingChance+rule.chainChance)
                        { SpawnStrike(StrikeKind.Chain,rule,false); chainLeft=Config.chainCount-1; chainTimer=Config.chainInterval; chainRule=rule; }
                        else SpawnStrike(StrikeKind.Normal,rule,false);
                    }
                }
            }
        }
        void SpawnStrike(StrikeKind kind, StageRule rule, bool tracking, bool predictive=false)
        {
            Vector3 target=predictive?Vector3.ClampMagnitude(Position+velocity*Config.predictionLead,Config.arenaRadius-1):Position;
            AddStrike(kind,rule,tracking,target,0);
        }
        void SpawnBlocking(StageRule rule)
        {
            // A short wall ahead; the rear half-plane remains available. Never surround the player.
            Vector3 forward=velocity.sqrMagnitude>.1f?velocity.normalized:Vector3.forward;
            Vector3 side=new Vector3(-forward.z,0,forward.x);
            Vector3 center=Position+forward*5.5f;
            for(int i=-1;i<=1;i++)
            {
                Vector3 p=center+side*i*(rule.radius*1.65f);
                if(p.magnitude>Config.arenaRadius-1) continue;
                AddStrike(StrikeKind.Blocking,rule,false,p,Config.blockingDuration);
            }
        }
        void AddStrike(StrikeKind kind,StageRule rule,bool tracking,Vector3 target,float lingering)
        {
            if(strikes.Count>=Config.strikeLimit) return;
            var strike=new Strike { id=++nextId, position=target, kind=kind,
                warning=tracking?Config.trackingTime+Config.lockTime:rule.warning,
                radius=rule.radius, damage=rule.damage, trackTime=tracking?Config.trackingTime:0,
                locked=!tracking, lingering=lingering };
            strikes.Add(strike);
            // Conservative admission test: at least one ordinary walking route avoids all
            // currently locked hazards at its expected impact times, including lingering zones.
            if(!HasWalkingExit(strike.warning)) { strikes.Remove(strike); Event?.Invoke("pattern_suppressed",target); return; }
            Event?.Invoke("warning_"+kind.ToString().ToLowerInvariant(),target);
        }
        bool HasWalkingExit(float horizon)
        {
            for(int direction=0;direction<32;direction++)
            {
                float a=direction*Mathf.PI*2/32;
                Vector3 v=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*Config.moveSpeed;
                bool clear=true;
                foreach(var h in strikes)
                {
                    if(!h.locked) continue;
                    float start=Mathf.Max(0,h.warning-h.age), end=Mathf.Min(horizon,h.warning+h.lingering-h.age);
                    if(start>horizon || end<0) continue;
                    for(float t=start;t<=end+.025f;t+=.05f)
                        if(Vector3.Distance(Vector3.ClampMagnitude(Position+v*t,Config.arenaRadius),h.position)<h.radius+Config.hitRadius+.3f) { clear=false; break; }
                    if(!clear) break;
                }
                if(clear) return true;
            }
            return false;
        }
        void AddOrb(Vector3 position,bool risky)
        {
            if(orbs.Count>=Config.qiLimit) return;
            position.y=0; orbs.Add(new QiOrb{id=++nextId,position=Vector3.ClampMagnitude(position,Config.arenaRadius-1),risky=risky});
        }
        void SpawnSafeOrb()
        {
            for(int attempt=0;attempt<12;attempt++)
            {
                float a=(float)random.NextDouble()*Mathf.PI*2;
                // Central or off-route caches: no automatic refill on a fixed outer orbit.
                float radius=attempt%2==0?2+(float)random.NextDouble()*3:6+(float)random.NextDouble()*5;
                Vector3 p=new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius);
                if(Vector3.Distance(Position,p)<3) continue;
                bool safe=true; foreach(var h in strikes) if(Vector3.Distance(h.position,p)<h.radius+1.5f) safe=false;
                foreach(var q in orbs) if(Vector3.Distance(q.position,p)<2) safe=false;
                if(safe) { AddOrb(p,false); return; }
            }
        }
        void Finish(RunStatus status)
        {
            Status = status; elements.Clear(); strikes.Clear(); orbs.Clear(); chainLeft=0; DashRemaining=0; ShieldRemaining=0; ShieldAmount=0;
            Event?.Invoke(status == RunStatus.Won ? "won" : "lost", Position);
        }
    }
}
