using System;
using System.IO;
using System.IO.Compression;
using Newtonsoft.Json;

namespace NeedForValheim {
public sealed class ModelData {
    public int schemaVersion; public string preset;
    public float wheelVisualLift, frontWheelVisualOffset, rearWheelVisualOffset;
    public float previewHubHeight;
    public bool suspensionHubAtRest;
    public ModelPart[] parts; public ModelMaterial[] materials; public ModelTexture[] textures;
    public ModelLight[] lights; public int neonTexture = -1;
    public ModelEffectEmitter[] effectEmitters;
    public ModelExhaustVariant[] exhaustVariants;
    public int nitroFlameTexture = -1, nitroPurgeTexture = -1;
    public int nitroAtlasColumns=2,nitroAtlasRows=2;
    public ModelAnimation[] animations;
    public ModelTrunkLayout[] trunkLayouts;
    public ModelAudioComponent[] audioComponents;
    public TurretRig turretRig;
    public ModelStyleOption[] styleOptions;
    public string[] styleReplacements;
    public ModelDecalSlot[] decalSlots;
}
public sealed class ModelStyleOption {public string category,title;public int id;public int rimInches,sourceKit=-1;public float frontWheelOffset,rearWheelOffset,frontTyreScale=1,rearTyreScale=1;}
public sealed class ModelDecalSlot {public string key,title,kind;}
public sealed class ModelAnimation { public string name; public ModelVertex pivot; public ModelAnimationFrame[] frames; }
public sealed class ModelAnimationFrame { public ModelRotation rotation; public ModelVertex offset; }
public struct ModelRotation { public float x,y,z,w; }
public sealed class ModelTrunkLayout { public string title; public ModelAudioSocket[] sockets; }
public sealed class ModelAudioSocket { public string size; public ModelVertex position,right,up,forward; }
public sealed class ModelAudioComponent { public string id,title,kind,size; }
public sealed class ModelEffectEmitter { public string role; public ModelVertex position,direction; }
public sealed class ModelExhaustVariant { public int style; public ModelEffectEmitter[] emitters; }
public sealed class ModelNeonSegment { public ModelVertex start,end; }
public sealed class ModelPart { public ModelNeonSegment[] neonSegments; public string name, group, axle, lightRole, movingPart, optionGroup, option; public bool rotatesWithWheel; public int material,vertexCount,indexCount; public string packedGeometry; public int sourceKit=-1;public int rimInches;public float wheelBead,wheelOutside; public ModelVertex[] vertices, normals; public ModelUV[] uv; public int[] indices; }
public sealed class ModelLight { public string role; public ModelVertex position, direction; }
public struct ModelVertex { public float x, y, z; }
public struct ModelUV { public float u, v; }
public sealed class ModelMaterial { public string name,alphaMode; public float[] color; public float metallic, smoothness; public int texture; public bool bodyPaint,rimPaint,wheelRubber; }
public sealed class ModelTexture { public string name; public int width, height; public byte[] rgba; public string rgbaDeflate; }

// Use the managed JSON reader shipped with Valheim. Unity's native serializer
// did not populate the external plugin's model fields in the installed game.
public static class ModelFile {
    public static bool ForAxle(ModelPart part,int index) => string.IsNullOrEmpty(part.axle)||(index<2?part.axle=="front":part.axle=="rear");
    public static ModelData Load(string path) {
        var file = new FileInfo(path);
        if (!file.Exists) throw new FileNotFoundException("Vehicle model file missing", path);
        if (file.Length == 0 || file.Length > 32 * 1024 * 1024)
            throw new InvalidDataException("Vehicle model file has an invalid size: " + file.Length);
        ModelData model;
        using(var stream=File.OpenText(path))using(var reader=new JsonTextReader(stream))
            model=JsonSerializer.Create(new JsonSerializerSettings{TypeNameHandling=TypeNameHandling.None,MaxDepth=32,CheckAdditionalContent=true}).Deserialize<ModelData>(reader);
        if (model?.parts == null || model.parts.Length == 0 || model.parts.Length > (model.styleOptions==null?320:2048))
            throw new InvalidDataException("Vehicle model must contain between 1 and 320 parts: " + path);
        if (model.schemaVersion != 0 && model.schemaVersion != 2 && model.schemaVersion != 3)
            throw new InvalidDataException("Unsupported visual model schema");
        // Schema 3 keeps every geometry rule of schema 2 and makes the showroom and
        // exhaust catalogs optional, so a car ships with only the catalogs its models have.
        bool rich = model.schemaVersion >= 2;
        if (rich) {
            if(!ValidCoordinate(model.previewHubHeight)||model.previewHubHeight<0||model.previewHubHeight>2)throw new InvalidDataException("Invalid preview hub height");
            foreach (var offset in new[] { model.wheelVisualLift, model.frontWheelVisualOffset, model.rearWheelVisualOffset })
                if (!ValidCoordinate(offset) || Math.Abs(offset) > .2f) throw new InvalidDataException("Invalid visual wheel offset");
            if (model.materials == null || model.materials.Length == 0 || model.materials.Length > 512 || model.textures == null || model.textures.Length > 128)
                throw new InvalidDataException("Invalid visual material table");
            long expandedBytes=0;
            foreach (var texture in model.textures) {
                if(texture!=null&&texture.rgbaDeflate!=null) {
                    if(texture.rgba!=null||texture.width<1||texture.height<1||texture.width>2048||texture.height>2048)throw new InvalidDataException("Invalid packed texture dimensions");
                    texture.rgba=Inflate(texture.rgbaDeflate,checked(texture.width*texture.height*4),ref expandedBytes);texture.rgbaDeflate=null;
                }
                if (texture == null || texture.width < 1 || texture.height < 1 || texture.width > 2048 || texture.height > 2048 ||
                    texture.rgba == null || texture.rgba.Length != (long)texture.width * texture.height * 4)
                    throw new InvalidDataException("Invalid RGBA texture");
            }
            foreach(var part in model.parts)if(part!=null&&part.packedGeometry!=null) {
                if(part.vertices!=null||part.indices!=null||part.normals!=null||part.uv!=null||part.vertexCount<3||part.vertexCount>65535||part.indexCount<3||part.indexCount>393210||part.indexCount%3!=0)throw new InvalidDataException("Invalid packed mesh dimensions");
                var bytes=Inflate(part.packedGeometry,checked(part.vertexCount*32+part.indexCount*4),ref expandedBytes);
                part.vertices=new ModelVertex[part.vertexCount];part.normals=new ModelVertex[part.vertexCount];part.uv=new ModelUV[part.vertexCount];part.indices=new int[part.indexCount];
                using(var input=new BinaryReader(new MemoryStream(bytes))) {
                    for(int i=0;i<part.vertexCount;i++){part.vertices[i]=new ModelVertex{x=input.ReadSingle(),y=input.ReadSingle(),z=input.ReadSingle()};part.normals[i]=new ModelVertex{x=input.ReadSingle(),y=input.ReadSingle(),z=input.ReadSingle()};part.uv[i]=new ModelUV{u=input.ReadSingle(),v=input.ReadSingle()};}
                    for(int i=0;i<part.indexCount;i++)part.indices[i]=input.ReadInt32();
                }part.packedGeometry=null;
            }
            foreach (var material in model.materials) {
                if (material == null || (material.alphaMode!=null&&material.alphaMode!="opaque"&&material.alphaMode!="blend"&&material.alphaMode!="mask") || material.color == null || material.color.Length != 4 || material.texture < -1 || material.texture >= model.textures.Length ||
                    !Unit(material.metallic) || !Unit(material.smoothness)) throw new InvalidDataException("Invalid material");
                foreach (var c in material.color) if (!Unit(c)) throw new InvalidDataException("Invalid material color");
                // Older MW exports classified BODY_PLAINNOTHING as paint,
                // allowing vinyl replacement to turn black cavities into skin.
                // Correct descriptors before styling, wetness and mesh setup.
                if(material.name!=null&&material.name.StartsWith("mw_",StringComparison.Ordinal)&&material.name.IndexOf("plainnothing",StringComparison.OrdinalIgnoreCase)>=0) {
                    material.bodyPaint=false;material.rimPaint=false;
                    material.color=new[]{.025f,.025f,.028f,1f};
                    material.metallic=0;material.smoothness=.12f;material.texture=-1;
                }
            }
            if (model.neonTexture < -1 || model.neonTexture >= model.textures.Length) throw new InvalidDataException("Invalid neon texture");
            if(model.nitroAtlasColumns<1||model.nitroAtlasColumns>8||model.nitroAtlasRows<1||model.nitroAtlasRows>8)throw new InvalidDataException("Invalid nitro atlas grid");
            if (model.nitroFlameTexture < -1 || model.nitroFlameTexture >= model.textures.Length ||
                model.nitroPurgeTexture < -1 || model.nitroPurgeTexture >= model.textures.Length)
                throw new InvalidDataException("Invalid nitro texture");
            if(model.effectEmitters!=null) {
                if(model.effectEmitters.Length>16)throw new InvalidDataException("Too many effect emitters");
                foreach(var emitter in model.effectEmitters) {
                    if(emitter==null || (emitter.role!="exhaust"&&emitter.role!="purge") ||
                        !ValidVector(emitter.position)||!ValidVector(emitter.direction))
                        throw new InvalidDataException("Invalid effect emitter");
                    var d=emitter.direction;
                    if(d.x*d.x+d.y*d.y+d.z*d.z<.001f)throw new InvalidDataException("Zero emitter direction");
                }
            }
            if (model.lights != null) {
                if (model.lights.Length > 16) throw new InvalidDataException("Too many vehicle lights");
                foreach (var light in model.lights) {
                    if (light == null || (light.role != "headlight" && light.role != "taillight" && light.role != "plate" && light.role != "reverse") ||
                        !ValidVector(light.position) || !ValidVector(light.direction)) throw new InvalidDataException("Invalid vehicle light");
                    var d = light.direction;
                    if (d.x*d.x+d.y*d.y+d.z*d.z < .001f) throw new InvalidDataException("Zero light direction");
                }
            }
        }
        foreach (var part in model.parts) {
            if(part?.neonSegments!=null) {
                if(part.group!="body"||part.neonSegments.Length>7)throw new InvalidDataException("Invalid source neon segment count");
                foreach(var segment in part.neonSegments) {
                    if(segment==null)throw new InvalidDataException("Missing source neon segment");
                    foreach(var v in new[]{segment.start,segment.end})if(!ValidCoordinate(v.x)||!ValidCoordinate(v.y)||!ValidCoordinate(v.z))throw new InvalidDataException("Invalid source neon endpoint");
                }
            }

            if (part == null || string.IsNullOrWhiteSpace(part.name) || part.vertices == null ||
                part.vertices.Length < 3 || part.vertices.Length > 65535 ||
                part.indices == null || part.indices.Length < 3 || part.indices.Length % 3 != 0)
                throw new InvalidDataException("Vehicle model contains an incomplete mesh");
            foreach (var vertex in part.vertices)
                if (!ValidCoordinate(vertex.x) || !ValidCoordinate(vertex.y) || !ValidCoordinate(vertex.z))
                    throw new InvalidDataException("Vehicle model has an invalid vertex: " + part.name);
            foreach (var index in part.indices)
                if (index < 0 || index >= part.vertices.Length)
                    throw new InvalidDataException("Vehicle model has an out-of-range triangle index: " + part.name);
            if (rich) {
                if(part.rimInches!=0&&(part.group!="wheel"||part.rimInches<17||part.rimInches>20||!ValidCoordinate(part.wheelBead)||!ValidCoordinate(part.wheelOutside)||part.wheelBead<=0||part.wheelOutside<=part.wheelBead))throw new InvalidDataException("Invalid native rim metadata");
                if (!string.IsNullOrEmpty(part.axle) && ((part.axle!="front"&&part.axle!="rear") ||
                    (part.group!="wheel"&&part.group!="brake"&&part.group!="spinner"))) throw new InvalidDataException("Invalid wheel axle role");
                if (part.rotatesWithWheel && part.group != "brake") throw new InvalidDataException("Only brake rotors may opt into wheel rotation");
                if (!string.IsNullOrEmpty(part.lightRole) && part.lightRole != "headlight" && part.lightRole != "taillight" && part.lightRole != "brakelight" && part.lightRole != "reverse")
                    throw new InvalidDataException("Invalid light surface role");
                if (part.material < 0 || part.material >= model.materials.Length ||
                    (part.group != "body" && part.group != "wheel" && part.group != "brake" && part.group != "audio" && part.group != "spinner") ||
                    part.uv == null || part.uv.Length != part.vertices.Length || part.normals == null || part.normals.Length != part.vertices.Length)
                    throw new InvalidDataException("Invalid visual mesh attributes");
                foreach (var uv in part.uv) if (!ValidCoordinate(uv.u) || !ValidCoordinate(uv.v)) throw new InvalidDataException("Invalid UV");
                foreach (var n in part.normals) if (!ValidCoordinate(n.x) || !ValidCoordinate(n.y) || !ValidCoordinate(n.z)) throw new InvalidDataException("Invalid normal");
            }
        }
        ValidateDecals(model);
        ValidateStyling(model);
        ValidateShowroom(model);
        ValidateExhaust(model);
        return model;
    }
    static byte[] Inflate(string encoded,int length,ref long total) {
        total+=length;if(total>256L*1024*1024)throw new InvalidDataException("Expanded model exceeds 256 MiB");
        var packed=Convert.FromBase64String(encoded);var bytes=new byte[length];
        using(var input=new DeflateStream(new MemoryStream(packed),CompressionMode.Decompress)) {
            int offset=0,read;while(offset<length&&(read=input.Read(bytes,offset,length-offset))>0)offset+=read;
            if(offset!=length||input.ReadByte()!=-1)throw new InvalidDataException("Packed asset length mismatch");
        }return bytes;
    }
    static void ValidateDecals(ModelData model) {
        if(model.decalSlots==null){if(Array.Exists(model.parts,p=>p.optionGroup=="decal"))throw new InvalidDataException("Decal parts have no slots");return;}
        if(model.schemaVersion!=3||model.decalSlots.Length==0||model.decalSlots.Length>64)throw new InvalidDataException("Invalid decal slot catalog");
        var keys=new System.Collections.Generic.HashSet<string>();
        foreach(var slot in model.decalSlots)if(slot==null||string.IsNullOrEmpty(slot.key)||slot.key.Length>100||string.IsNullOrEmpty(slot.title)||slot.title.Length>64||(slot.kind!="RECT"&&slot.kind!="WIDE")||!keys.Add(slot.key))throw new InvalidDataException("Invalid decal slot");
        foreach(var part in model.parts)if(part.optionGroup!="decal"||part.group!="body"||part.sourceKit< -1||part.sourceKit>31||!keys.Contains(part.option))throw new InvalidDataException("Invalid decal projection part");
        foreach(var key in keys)if(!Array.Exists(model.parts,p=>p.option==key))throw new InvalidDataException("Empty decal slot");
    }
    static void ValidateStyling(ModelData model) {
        if(model.styleOptions==null)return;
        if(model.schemaVersion!=3||model.styleOptions.Length==0||model.styleOptions.Length>512||model.styleReplacements==null||model.styleReplacements.Length>320)throw new InvalidDataException("Invalid styling catalog");
        var keys=new System.Collections.Generic.HashSet<string>();
        foreach(var option in model.styleOptions) {
            if(option==null||(option.rimInches!=0&&(option.category!="Rims"||option.rimInches<17||option.rimInches>20))||!ValidCoordinate(option.frontWheelOffset)||!ValidCoordinate(option.rearWheelOffset)||Math.Abs(option.frontWheelOffset)>.3f||Math.Abs(option.rearWheelOffset)>.3f||!ValidCoordinate(option.frontTyreScale)||!ValidCoordinate(option.rearTyreScale)||option.frontTyreScale<.5f||option.frontTyreScale>2||option.rearTyreScale<.5f||option.rearTyreScale>2||Array.IndexOf(new[]{"Bodykit","Front","Rear","Skirts","Hood","Spoiler","Roof","Brakes","Rims"},option.category)<0||option.id<1||option.id>256||string.IsNullOrWhiteSpace(option.title)||option.title.Length>64||!keys.Add(option.category+":"+option.id))throw new InvalidDataException("Invalid styling option");
        }
        foreach(var part in model.parts)if(part.optionGroup!="styling"||!keys.Contains(part.option))throw new InvalidDataException("Styling mesh has no catalog option");
        foreach(var option in keys)if(!Array.Exists(model.parts,p=>p.option==option))throw new InvalidDataException("Styling option has no mesh");
        foreach(var name in model.styleReplacements)if(string.IsNullOrWhiteSpace(name)||name.Length>160)throw new InvalidDataException("Invalid styling replacement");
    }
    static void ValidateExhaust(ModelData model) {
        if(model.exhaustVariants==null) {
            foreach(var part in model.parts)if(part.optionGroup=="exhaust")throw new InvalidDataException("Exhaust meshes have no catalog");
            return;
        }
        // Schema 2 kept the ten stock NFS exhaust styles; a schema 3 car ships a subset.
        if(model.exhaustVariants.Length<(model.schemaVersion==2?10:1)||model.exhaustVariants.Length>10)
            throw new InvalidDataException("Incomplete exhaust catalog");
        int flameCount=0;
        if(model.effectEmitters!=null)foreach(var emitter in model.effectEmitters)if(emitter.role=="exhaust")flameCount++;
        var styles=new System.Collections.Generic.HashSet<int>();
        foreach(var variant in model.exhaustVariants) {
            if(variant==null||variant.style<1||variant.style>10||!styles.Add(variant.style)||variant.emitters==null||
                (model.schemaVersion==2?(variant.emitters.Length!=2&&variant.emitters.Length!=4):(variant.emitters.Length<1||variant.emitters.Length>4)))throw new InvalidDataException("Invalid exhaust style");
            if(variant.emitters.Length>flameCount)throw new InvalidDataException("Exhaust catalog exceeds the available flame emitters");
            bool mesh=false;
            foreach(var part in model.parts)if(part.optionGroup=="exhaust"&&part.option==variant.style.ToString())mesh=true;
            if(!mesh)throw new InvalidDataException("Exhaust style has no mesh");
            foreach(var emitter in variant.emitters) {
                if(emitter==null||emitter.role!="exhaust"||!ValidVector(emitter.position)||!ValidVector(emitter.direction)||
                    Dot(emitter.direction,emitter.direction)<.001f)throw new InvalidDataException("Invalid exhaust emitter");
            }
        }
    }
    static void ValidateShowroom(ModelData model) {
        if(model.animations==null&&model.trunkLayouts==null&&model.audioComponents==null)return;
        if(model.schemaVersion<2)throw new InvalidDataException("Incomplete showroom catalog");
        // Schema 2 is the finished 350Z showroom: eight motion tracks, four trunk
        // layouts and thirteen choices per socket. Schema 3 validates what is present.
        bool strict=model.schemaVersion==2;
        var names=new System.Collections.Generic.HashSet<string>();
        if(model.animations!=null) {
            if(strict&&model.animations.Length!=8)throw new InvalidDataException("Incomplete showroom catalog");
            foreach(var animation in model.animations) {
                if(animation==null||!names.Add(animation.name)||!ValidVector(animation.pivot)||animation.frames==null||animation.frames.Length<2||animation.frames.Length>120)
                    throw new InvalidDataException("Invalid showroom animation");
                foreach(var frame in animation.frames) {
                    if(frame==null||!ValidVector(frame.offset))throw new InvalidDataException("Invalid motion frame");
                    var q=frame.rotation;float norm=q.x*q.x+q.y*q.y+q.z*q.z+q.w*q.w;
                    if(!ValidCoordinate(norm)||Math.Abs(norm-1)>.01f)throw new InvalidDataException("Invalid motion quaternion");
                }
            }
            if(strict)foreach(var name in new[]{"HOOD_REG","TRUNK_REG","LEFT_DOOR_REG","RIGHT_DOOR_REG","LEFT_DOOR_SUICIDE","RIGHT_DOOR_SUICIDE","LEFT_DOOR_SCISSOR","RIGHT_DOOR_SCISSOR"})
                if(!names.Contains(name))throw new InvalidDataException("Missing motion track");
        }
        if(model.trunkLayouts!=null) {
            if(strict&&model.trunkLayouts.Length!=4)throw new InvalidDataException("Incomplete showroom catalog");
            foreach(var layout in model.trunkLayouts) {
                if(layout==null||layout.sockets==null||layout.sockets.Length>6)throw new InvalidDataException("Invalid trunk layout");
                foreach(var socket in layout.sockets) {
                    if(socket==null||!AudioSize(socket.size)||!ValidVector(socket.position)||!Basis(socket.right,socket.up,socket.forward))
                        throw new InvalidDataException("Invalid audio socket");
                }
            }
        }
        names.Clear();
        if(model.audioComponents!=null) {
            if(strict&&(model.audioComponents.Length>64||model.animations==null||model.trunkLayouts==null))throw new InvalidDataException("Incomplete showroom catalog");
            foreach(var component in model.audioComponents) {
                if(component==null||string.IsNullOrEmpty(component.id)||!names.Add(component.id)||string.IsNullOrEmpty(component.title)||!AudioSize(component.size)||
                    (component.kind!="speaker"&&component.kind!="amp"&&component.kind!="lcd"&&component.kind!="empty"))throw new InvalidDataException("Invalid audio component");
            }
        }
        if(strict&&(model.animations==null||model.trunkLayouts==null||model.audioComponents==null))
            throw new InvalidDataException("Incomplete showroom catalog");
        if(model.trunkLayouts!=null&&model.audioComponents!=null)foreach(var layout in model.trunkLayouts)foreach(var socket in layout.sockets) {
            int count=0;foreach(var component in model.audioComponents)if(component.size==socket.size)count++;
            if(count<(strict?13:1))throw new InvalidDataException("Incomplete component choices for socket size");
        }
        var available=new System.Collections.Generic.HashSet<string>();
        foreach(var part in model.parts) {
            if(!string.IsNullOrEmpty(part.movingPart)&&part.movingPart!="leftDoor"&&part.movingPart!="rightDoor"&&part.movingPart!="hood"&&part.movingPart!="trunk")throw new InvalidDataException("Invalid moving part");
            if(part.group=="audio") {
                if(part.optionGroup!="audio"||!names.Contains(part.option)||!string.IsNullOrEmpty(part.movingPart))throw new InvalidDataException("Unknown audio mesh");
                available.Add(part.option);
            } else if(!string.IsNullOrEmpty(part.optionGroup)) {
                int max=part.optionGroup=="trunk"?3:part.optionGroup=="engine"?2:part.optionGroup=="exhaust"?10:-1;
                if(part.group!="body"||!int.TryParse(part.option,out int index)||index<(part.optionGroup=="exhaust"?1:0)||index>max)throw new InvalidDataException("Invalid showroom option");
            }
        }
        if(!available.SetEquals(names))throw new InvalidDataException("Audio component has no mesh");
    }
    static bool AudioSize(string size)=>size=="SMALL"||size=="MEDIUM"||size=="LARGE";
    static float Dot(ModelVertex a,ModelVertex b)=>a.x*b.x+a.y*b.y+a.z*b.z;
    static bool Basis(ModelVertex a,ModelVertex b,ModelVertex c) {
        if(!ValidVector(a)||!ValidVector(b)||!ValidVector(c))return false;
        float determinant=a.x*(b.y*c.z-b.z*c.y)-b.x*(a.y*c.z-a.z*c.y)+c.x*(a.y*b.z-a.z*b.y);
        return Math.Abs(Dot(a,a)-1)<.01&&Math.Abs(Dot(b,b)-1)<.01&&Math.Abs(Dot(c,c)-1)<.01&&Math.Abs(Dot(a,b))<.01&&Math.Abs(Dot(a,c))<.01&&Math.Abs(Dot(b,c))<.01&&Math.Abs(determinant-1)<.01;
    }
    static bool ValidVector(ModelVertex v) => ValidCoordinate(v.x) && ValidCoordinate(v.y) && ValidCoordinate(v.z);
    static bool ValidCoordinate(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && Math.Abs(value) <= 100f;
    static bool Unit(float value) => ValidCoordinate(value) && value >= 0 && value <= 1;
}
}
