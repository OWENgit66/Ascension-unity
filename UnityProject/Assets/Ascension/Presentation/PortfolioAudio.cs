using System;
using System.IO;
using UnityEngine;
namespace Ascension.Presentation
{
    // Captures only this game's own AudioSource samples. Never records microphone/system audio.
    public sealed class PortfolioAudio : MonoBehaviour
    {
        readonly object gate=new object();
        float[] ring;int at,count,channels=2,rate;double energy;long samples;float peak;
        string directory,pending;float delay,nextReport;
        void Awake()
        {
            rate=AudioSettings.outputSampleRate;ring=new float[rate*2*16];
            var args=Environment.GetCommandLineArgs();for(int i=0;i+1<args.Length;i++)if(args[i]=="--ascension-evidence")directory=Path.GetDirectoryName(args[i+1]);
        }
        void OnAudioFilterRead(float[] data,int channelCount)
        {
            if(ring==null)return;
            lock(gate)
            {
                channels=channelCount;
                for(int i=0;i<data.Length;i++)
                {
                    // Soft limiting protects the mix when several thunder events coincide.
                    float v=(float)(.8*Math.Tanh(data[i]/.8));data[i]=v;
                    ring[at]=v;at=(at+1)%ring.Length;count=Math.Min(count+1,ring.Length);
                    peak=Math.Max(peak,Math.Abs(v));energy+=v*v;samples++;
                }
            }
        }
        public void Capture(string label){pending=label;delay=2.8f;}
        void Update()
        {
            if(Time.unscaledTime>=nextReport)
            {
                nextReport=Time.unscaledTime+10;lock(gate){if(samples>0)Debug.Log("[AscensionAudio] samples="+samples+" peak="+peak.ToString("F4")+" rms="+Math.Sqrt(energy/samples).ToString("F5"));}
            }
            if(pending==null)return;delay-=Time.unscaledDeltaTime;if(delay>0)return;
            var label=pending;pending=null;if(string.IsNullOrEmpty(directory))return;
            float[] copy;int ch;
            lock(gate){copy=new float[count];ch=channels;for(int i=0;i<count;i++)copy[i]=ring[(at-count+i+ring.Length)%ring.Length];}
            string path=Path.Combine(directory,"t07-mix-"+label+"-"+DateTime.UtcNow.ToString("yyyyMMddTHHmmss")+".wav");
            using(var w=new BinaryWriter(File.Create(path)))
            {
                int bytes=copy.Length*2;w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));w.Write(36+bytes);w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));w.Write(16);w.Write((short)1);w.Write((short)ch);w.Write(rate);w.Write(rate*ch*2);w.Write((short)(ch*2));w.Write((short)16);w.Write(System.Text.Encoding.ASCII.GetBytes("data"));w.Write(bytes);foreach(float v in copy)w.Write((short)(v*32767));
            }
            Debug.Log("[AscensionAudio] captured "+path);
        }
    }
}
