using System.Collections.Generic;
using UnityEngine;
namespace Ascension.Presentation
{
    public sealed partial class AscensionView
    {
        readonly Dictionary<int,LineRenderer> elementLines=new Dictionary<int,LineRenderer>();
        string ElementName(ElementKind kind)=>kind==ElementKind.Wind?"风劫 → 逆风 / Dash":kind==ElementKind.Fire?"火劫 · 持续灼烧":kind==ElementKind.Earth?"地劫 · 裂缝":kind==ElementKind.BloomCenter?"散花雷 · 找安全夹角":"散花雷带";
        void DrawElements()
        {
            var alive=new HashSet<int>();
            foreach(var h in runtime.State.Elements)
            {
                alive.Add(h.id);
                Material mat=h.kind==ElementKind.Wind?jade:h.kind==ElementKind.Fire?orange:h.kind==ElementKind.Earth?gold:violet;
                if(!elementLines.TryGetValue(h.id,out var line)){line=Ring("Element "+h.kind,h.position,h.radius,mat,.1f);elementLines.Add(h.id,line);}
                line.sharedMaterial=mat;
                if(h.kind==ElementKind.Wind)
                {
                    line.loop=false;line.positionCount=5;var p=runtime.State.Position+Vector3.up*.08f;var d=h.direction;var side=new Vector3(-d.z,0,d.x);float length=runtime.balance.arenaRadius*.2f;
                    line.SetPositions(new[]{p-d*length,p+d*length,p+d*(length*.5f)+side*length*.35f,p+d*length,p+d*(length*.5f)-side*length*.35f});line.widthMultiplier=h.active?.26f:.14f;
                }
                else if(h.kind==ElementKind.Earth||h.kind==ElementKind.BloomRay)
                {
                    line.loop=false;line.positionCount=2;line.SetPositions(new[]{h.position+Vector3.up*.06f,h.end+Vector3.up*.06f});line.widthMultiplier=h.radius*2;
                }
                else
                {
                    line.loop=true;line.positionCount=64;
                    for(int i=0;i<64;i++){float a=i*Mathf.PI*2/64;line.SetPosition(i,h.position+new Vector3(Mathf.Cos(a)*h.radius,.06f,Mathf.Sin(a)*h.radius));}
                    line.widthMultiplier=h.active?.12f:.24f;
                }
            }
            var stale=new List<int>();foreach(var p in elementLines)if(!alive.Contains(p.Key))stale.Add(p.Key);foreach(int id in stale){Destroy(elementLines[id].gameObject);elementLines.Remove(id);}
        }
        void DrawElementHUD()
        {
            var s=runtime.State;
            if(s.Status!=RunStatus.Playing && s.Status!=RunStatus.Paused)return;
            if(s.Stage==3)GUI.Label(new Rect(340,102,920,40),"终劫 "+(s.FinalSection==1?"A · 识劫":s.FinalSection==2?"B · 合劫":"C · 九天神雷")+"   雷 / 风 / 火 / 地",small);
            foreach(var h in s.Elements)
            {
                if(h.kind==ElementKind.BloomRay)continue;
                var anchor=(h.kind==ElementKind.Earth?(h.position+h.end)/2:h.kind==ElementKind.Wind?s.Position+h.direction*3:h.position)+Vector3.up*.3f;
                WorldWarning(anchor,ElementName(h.kind)+"\n"+(h.active?"发生中 ":"预警 ")+Mathf.Max(0,h.warning+(h.active?h.duration:0)-h.age).ToString("F1"),2);
            }
            if(!string.IsNullOrEmpty(s.LastDamageSource))GUI.Label(new Rect(52,685,690,38),"最近承伤："+DamageSourceLabel(s.LastDamageSource),small);
        }
    }
}
