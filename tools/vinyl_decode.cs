using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
// Batch equivalent of the repository's U2 indexed-texture/Huffman decoder.
// Avoid thousands of Python per-bit loops; original source files remain private.
class VinylDecode {
    class Bits {
        readonly byte[] data;int bit;
        public Bits(byte[] bytes){data=bytes;}
        public int Read(int n){if(bit+n>data.Length*8)throw new InvalidDataException("Truncated Huffman");int v=0;while(n-->0){v=v*2+(data[bit>>3]>>(7-(bit&7))&1);bit++;}return v;}
        public int Variable(){int w=2;while(Read(1)==0)if(++w>24)throw new InvalidDataException("Huffman variable");return Read(w)+(1<<w)-4;}
    }
    static byte[] Huff(byte[] input,int capacity){
        var b=new Bits(input);int method=b.Read(16);if((method&0xf0ff)!=0x30fb)throw new InvalidDataException("Huffman method");if((method&0x100)!=0)b.Read(24);
        int size=b.Read(24),escape=b.Read(8);if(size<1||size>capacity||size>8*1024*1024)throw new InvalidDataException("Huffman size");
        var counts=new List<int>{0};int code=0;
        while(counts.Count<=16&&(code<<(17-counts.Count))<65536){int n=b.Variable();if(n>256)throw new InvalidDataException("Huffman tree");counts.Add(n);code=code*2+n;}
        int total=counts.Sum();if(total<1||total>256)throw new InvalidDataException("Huffman symbols");
        var symbols=new int[total];var used=new bool[256];int value=255;
        for(int i=0;i<total;i++){int step=b.Variable()+1;if(step>256)throw new InvalidDataException("Huffman symbol");while(step>0){value=(value+1)&255;if(!used[value])step--;}symbols[i]=value;used[value]=true;}
        var first=new int[counts.Count];var index=new int[counts.Count];code=0;int offset=0;
        for(int i=0;i<counts.Count;i++){first[i]=code;index[i]=offset;code=(code+counts[i])*2;offset+=counts[i];}
        var output=new byte[size];int pos=0;
        while(pos<size){code=0;int symbol=-1;for(int w=1;w<counts.Count;w++){code=code*2+b.Read(1);int delta=code-first[w];if(delta>=0&&delta<counts[w]){symbol=symbols[index[w]+delta];break;}}
            if(symbol<0)throw new InvalidDataException("Huffman code");if(symbol!=escape){output[pos++]=(byte)symbol;continue;}
            int n=b.Variable();if(n>0){if(pos+n>size)throw new InvalidDataException("Huffman repeat");byte last=pos>0?output[pos-1]:(byte)0;Array.Fill(output,last,pos,n);pos+=n;}
            else {if(b.Read(1)!=0)break;output[pos++]=(byte)b.Read(8);}}
        if(pos!=size)throw new InvalidDataException("Huffman output");int mode=method&0xfeff;
        if(mode==0x32fb||mode==0x34fb){int x=0,y=0;for(int i=0;i<size;i++){x=(x+output[i])&255;y=(y+x)&255;output[i]=(byte)(mode==0x34fb?y:x);}}
        return output;
    }
    static uint U(byte[] b,int p)=>BitConverter.ToUInt32(b,p);
    static byte[] Jdlz(byte[] input,int offset,int capacity){
        int size=(int)U(input,offset+8);if(size<1||size>capacity)throw new InvalidDataException("JDLZ size");var output=new byte[size];int pos=offset+16,n=0,f1=1,f2=1;
        while(n<size){if(f1==1)f1=input[pos++]|256;if(f2==1)f2=input[pos++]|256;
            if((f1&1)!=0){int a=input[pos++],b=input[pos++],count,distance;if((f2&1)!=0){count=(b|((a&240)<<4))+3;distance=(a&15)+1;}else{count=(a&31)+3;distance=(b|((a&224)<<3))+17;}
                if(distance>n||n+count>size)throw new InvalidDataException("JDLZ reference");while(count-->0){output[n]=output[n-distance];n++;}f2>>=1;
            }else output[n++]=input[pos++];f1>>=1;}
        return output;
    }
    static int Find(byte[] b,uint value){for(int i=0;i<b.Length-4;i++)if(U(b,i)==value)return i;return -1;}
    static string Pack(byte[] bytes){using(var m=new MemoryStream()){using(var z=new DeflateStream(m,CompressionLevel.Fastest,true))z.Write(bytes,0,bytes.Length);return Convert.ToBase64String(m.ToArray());}}
    static void Main(string[] args){try{Run(args);}catch(Exception e){Console.Error.WriteLine("Vinyl decode failed: "+e.Message);Environment.ExitCode=1;}}
    static void Run(string[] args){
        var data=File.ReadAllBytes(args[0]);int start=Find(data,0xb3310000);if(start<0)throw new InvalidDataException("No U2 TPK");
        int end=start+8+(int)U(data,start+4),pos=start+8;var result=new List<object>();
        while(pos+8<=end){uint tag=U(data,pos);int size=(int)U(data,pos+4);pos+=8;if(pos+size>end)throw new InvalidDataException("TPK table");
            if(tag==0x33310003)for(int slot=pos;slot<pos+size;slot+=24){uint key=U(data,slot);int off=(int)U(data,slot+4),encoded=(int)U(data,slot+8),decoded=(int)U(data,slot+12),tail=(int)U(data,slot+16);
                string magic=Encoding.ASCII.GetString(data,off,4);
                var raw=magic=="HUFF"?Huff(data.AsSpan(off+16,encoded-16).ToArray(),decoded):magic=="JDLZ"?Jdlz(data,off,decoded):throw new InvalidDataException("Vinyl compression "+magic);
                int p=decoded-tail+0x88;if(p<24||p+0x28>raw.Length||U(raw,p)!=key)throw new InvalidDataException("Texture key");
                int width=BitConverter.ToUInt16(raw,p+0x20),height=BitConverter.ToUInt16(raw,p+0x22),format=raw[p+0x26];
                string name=Encoding.ASCII.GetString(raw,p-24,24).Split('\0')[0];byte[] rgba;
                if(format==8||format==0x80||format==0x81){int palette=p-24-12-1024;if(palette<width*height)throw new InvalidDataException("Palette");rgba=new byte[width*height*4];for(int i=0;i<width*height;i++){int color=palette+raw[i]*4,pixel=i*4;rgba[pixel]=raw[color+2];rgba[pixel+1]=raw[color+1];rgba[pixel+2]=raw[color];rgba[pixel+3]=raw[color+3];}}
                else if(format==0x20){rgba=raw.Take(width*height*4).ToArray();for(int i=0;i<rgba.Length;i+=4)(rgba[i],rgba[i+2])=(rgba[i+2],rgba[i]);}
                else throw new InvalidDataException("Unsupported vinyl texture: "+name+" format "+format);
                result.Add(new{key,name,width,height,data=Pack(rgba)});
            }pos+=size;}
        File.WriteAllText(args[1],JsonSerializer.Serialize(result));Console.WriteLine("Decoded "+result.Count+" original vinyl textures");
    }
}
