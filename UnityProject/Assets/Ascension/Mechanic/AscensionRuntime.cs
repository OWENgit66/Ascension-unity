using System;
using System.IO;
using A3GameRuntime;
using UnityEngine;

namespace Ascension
{
    public sealed class AscensionRuntime : MonoBehaviour, IA3GameEntityFactory
    {
        public Balance balance = new Balance();
        public TribulationModel State { get; private set; }
        public float Yaw { get; private set; }
        public float Pitch { get; private set; } = 38;
        public Vector3 Facing { get; private set; } = Vector3.forward;
        public event Action<string, Vector3> GameplayEvent;
        Vector3 remoteMove;
        float remoteUntil;
        A3GameRuntimeEntityComponent remoteEntity;
        bool previousRun, previousJump;
        string evidencePath;
        public bool FastPlaytest { get; private set; }
        public int TechniqueIndex { get; private set; }
        public Technique SelectedTechnique => balance.techniques[TechniqueIndex];
        float[] normalDurations;
        float nextSnapshot;
        void Awake()
        {
            var config=Resources.Load<TextAsset>("Balance");
            if(config!=null) JsonUtility.FromJsonOverwrite(config.text,balance);
            normalDurations=new float[balance.stages.Length];
            for(int i=0;i<normalDurations.Length;i++) normalDurations[i]=balance.stages[i].duration;
            var launchArgs=Environment.GetCommandLineArgs();
            for(int i=0;i<launchArgs.Length;i++) { if(launchArgs[i]=="--ascension-fast") SetFastPlaytest(true); if(launchArgs[i]=="--ascension-seed" && i+1<launchArgs.Length && int.TryParse(launchArgs[i+1],out int seed)) balance.seed=seed; }
            State = new TribulationModel(balance);
            Application.SetStackTraceLogType(LogType.Log,StackTraceLogType.None);
            var args=Environment.GetCommandLineArgs();
            for(int i=0;i<args.Length-1;i++) if(args[i]=="--ascension-evidence") evidencePath=args[i+1];
            State.Event += (key, pos) => {
                GameplayEvent?.Invoke(key, pos);
                Debug.Log("[Ascension] " + key + " time=" + State.Elapsed.ToString("F2") + " hp=" + State.HP + " qi="+State.Qi+" stage="+(State.Stage+1)+" shield="+State.ShieldAmount);
                WriteEvidence(key);
            };
        }
        void Start()
        {
            var framework = A3GameRuntimeSubsystem.Instance;
            if (framework == null) framework = new GameObject("Runtime").AddComponent<A3GameRuntimeSubsystem>();
            framework.Initialize("ascension_whitebox"); framework.RegisterFactory(this);
            Application.runInBackground = true;
        }
        public GameObject CreateEntity(A3GameEntitySpawnRequest request)
        {
            if (remoteEntity != null)
            {
                if(remoteEntity.entityId==request.entity_id) return remoteEntity.gameObject;
                remoteEntity.RuntimeInput-=ReceiveInput; Destroy(remoteEntity.gameObject);
            }
            var go = new GameObject("CultivatorInput");
            remoteEntity = go.AddComponent<A3GameRuntimeEntityComponent>();
            remoteEntity.Initialize(request.entity_id,"ascension_whitebox");
            remoteEntity.RuntimeInput += ReceiveInput;
            return go;
        }
        public bool DestroyEntity(string id)
        {
            if(remoteEntity==null || remoteEntity.entityId!=id) return false;
            remoteMove=Vector3.zero; remoteUntil=0; Destroy(remoteEntity.gameObject); remoteEntity=null; return true;
        }
        void ReceiveInput(A3GameRuntimeInputState input)
        {
            remoteMove = new Vector3(input.move_x,0,input.move_y); remoteUntil=Time.unscaledTime+.4f;
            Yaw=input.yaw; Pitch=Mathf.Clamp(input.pitch,22,65);
            if(input.run && !previousRun) Dash();
            if(input.jump && !previousJump) Shield();
            previousRun=input.run; previousJump=input.jump;
        }
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P)) { State.TogglePause(); remoteUntil=0; }
            bool playing = State.Status == RunStatus.Playing;
            Cursor.lockState = playing ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !playing;
            if (!playing) return;
            Yaw += Input.GetAxisRaw("Mouse X") * 2.5f;
            Pitch = Mathf.Clamp(Pitch - Input.GetAxisRaw("Mouse Y") * 2, 22, 65);
            Vector3 local = new Vector3((Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0), 0,
                (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0));
            Vector3 move = Quaternion.Euler(0, Yaw, 0) * local;
            if(Time.unscaledTime < remoteUntil) move = Quaternion.Euler(0,Yaw,0)*remoteMove;
            if (move.sqrMagnitude > .01f) Facing = move.normalized;
            if(Input.GetKeyDown(KeyCode.LeftShift)||Input.GetKeyDown(KeyCode.RightShift)) Dash();
            if(Input.GetKeyDown(KeyCode.Q)) Shield();
            State.Tick(Time.deltaTime, move);
            if(remoteEntity) remoteEntity.transform.position=State.Position;
            if(Time.unscaledTime>=nextSnapshot) { nextSnapshot=Time.unscaledTime+.25f; WriteEvidence("snapshot"); }
        }
        public void SetFastPlaytest(bool fast)
        {
            if(State!=null && (State.Status==RunStatus.Playing || State.Status==RunStatus.Paused)) return;
            FastPlaytest=fast; for(int i=0;i<balance.stages.Length;i++) balance.stages[i].duration=fast?balance.fastDurations[i]:normalDurations[i];
        }
        public void SelectTechnique(int index) { if(State.Status==RunStatus.Playing || State.Status==RunStatus.Paused)return; TechniqueIndex=Mathf.Clamp(index,0,balance.techniques.Length-1); SelectedTechnique.Apply(balance); }
        public void StartRun() { SelectedTechnique.Apply(balance); Yaw = 0; Pitch = 38; Facing = Vector3.forward; remoteUntil=0; previousRun=false; previousJump=false; State.Start(); }
        public bool Dash() => State.Dash(Facing);
        public bool Shield() => State.Shield();
        public void Pause() => State.TogglePause();
        public string GetStateSnapshot() => SnapshotJson("query");
        string SnapshotJson(string eventKey)
        {
            var hazards=new Strike[State.Strikes.Count]; for(int i=0;i<hazards.Length;i++) hazards[i]=State.Strikes[i];
            var elements=new ElementHazard[State.Elements.Count];for(int i=0;i<elements.Length;i++)elements[i]=State.Elements[i];
            var qi=new QiOrb[State.Orbs.Count]; for(int i=0;i<qi.Length;i++) qi[i]=State.Orbs[i];
            return JsonUtility.ToJson(new Snapshot {technique=SelectedTechnique.id,dashCost=balance.dashCost,shieldCost=balance.shieldCost,qiValue=balance.qiValue,elements=elements,finalSection=State.FinalSection,finalCue=State.FinalCue,lastDamageSource=State.LastDamageSource,windDisplacement=State.WindDisplacement,profile=FastPlaytest?"fast":"normal",seed=balance.seed,totalDuration=balance.TotalDuration,eventKey=eventKey,status=State.Status.ToString(),hp=State.HP,qi=State.Qi,elapsed=State.Elapsed,
                position=State.Position,yaw=Yaw,pitch=Pitch,stage=State.Stage+1,finalWaves=State.FinalWaves,dashCooldown=State.DashCooldown,
                shieldCooldown=State.ShieldCooldown,shieldAmount=State.ShieldAmount,shieldRemaining=State.ShieldRemaining,strikes=hazards,orbs=qi });
        }
        void WriteEvidence(string eventKey)
        {
            if(string.IsNullOrEmpty(evidencePath)) return;
            try { File.AppendAllText(evidencePath,SnapshotJson(eventKey)+Environment.NewLine); }
            catch(Exception e) { Debug.LogWarning("Evidence capture disabled: "+e.Message); evidencePath=null; }
        }
        [Serializable] struct Snapshot
        {
            public string technique; public float dashCost,shieldCost,qiValue;
            public ElementHazard[] elements; public int finalSection; public string finalCue,lastDamageSource; public Vector3 windDisplacement;
            public string eventKey,status,profile; public int seed; public float totalDuration; public float hp,qi,elapsed,yaw,pitch,dashCooldown,shieldCooldown,shieldAmount,shieldRemaining;
            public int stage,finalWaves; public Vector3 position; public Strike[] strikes; public QiOrb[] orbs;
        }
        void OnDisable() { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
    }
}
