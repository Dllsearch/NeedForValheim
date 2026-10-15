using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;

namespace NeedForValheim {
public partial class Plugin {
    // Rim size is per vehicle: the profile decides the nominal size and its slider range.
    readonly Dictionary<string,ConfigEntry<float>> rimSettings=new Dictionary<string,ConfigEntry<float>>();
    ConfigEntry<float> RimSetting=>rimSettings.TryGetValue(current.Profile.ConfigPrefix,out var entry)?entry:rimSettings[string.Empty];
    ConfigEntry<float> baseConfig_lowering;
    ConfigEntry<float> lowering=>VehicleConfig(baseConfig_lowering);

    void InitializeWheelTuning() {
        baseConfig_lowering=WheelSetting("LoweringCm",0,0,12,"Lower the body by raising suspension anchors, in centimetres. Zero restores stock height; less ground clearance at higher values.");
        frontCamber=WheelSetting("FrontCamber",0,-15,10,"Front visual camber in degrees. Negative tilts the top inward.");
        rearCamber=WheelSetting("RearCamber",0,-15,10,"Rear visual camber in degrees. Negative tilts the top inward.");
        frontWheelOffset=WheelSetting("FrontOffsetCm",0,-10,20,"Front visual offset per wheel in centimetres. Positive moves outward.");
        rearWheelOffset=WheelSetting("RearOffsetCm",0,-10,20,"Rear visual offset per wheel in centimetres. Positive moves outward.");
        foreach(var profile in vehicleCatalog.Profiles) {
            var rig=profile.rig;
            rimSettings[profile.ConfigPrefix]=Config.Bind(profile.ConfigPrefix+"Wheels","RimInches",rig.rimNominal,
                new ConfigDescription("Nominal rim diameter relative to the original NFS 20-inch wheel. Tyre outside diameter stays fixed.",
                    new AcceptableValueRange<float>(rig.rimMin,rig.rimMax)));
        }
    }
    ConfigEntry<float> WheelSetting(string key,float value,float min,float max,string description) =>
        BindVehicle("Wheels",key,value,new ConfigDescription(description,new AcceptableValueRange<float>(min,max)));

    void DrawWheelMenu() {
        var rig=current.Profile.rig;var rim=RimSetting;
        GUI.enabled=originalWheels;
        WheelSlider(frontCamber,"Front camber",140,-15,10,.5f,"°");
        WheelSlider(rearCamber,"Rear camber",191,-15,10,.5f,"°");
        WheelSlider(frontWheelOffset,"Front wheel offset",242,-10,20,.5f," cm / wheel");
        WheelSlider(rearWheelOffset,"Rear wheel offset",293,-10,20,.5f," cm / wheel");
        WheelSlider(rim,"Rim size",344,rig.rimMin,rig.rimMax,.5f," inches");
        GUI.enabled=true;
        WheelSlider(lowering,"Lowering",395,0,12,.5f," cm");
        if(MenuButton(new Rect(25,455,510,30),"Reset wheels and lowering")) {
            lowering.Value=0;
            frontCamber.Value=rearCamber.Value=0;
            frontWheelOffset.Value=rearWheelOffset.Value=0;rim.Value=rig.rimNominal;
        }
        GUI.enabled=true;
        UiLabel(new Rect(25,490,510,30),originalWheels?
            "Lowering reduces ground clearance. Camber and offset are visual.":
            "Available for imported original wheels.",mutedStyle);
    }
    void WheelSlider(ConfigEntry<float> setting,string label,float y,float min,float max,float step,string unit) {
        float current=WheelFitMath.Sanitize(setting.Value,min,max,(float)setting.DefaultValue);
        UiLabel(new Rect(25,y,510,24),label+"   "+current.ToString("0.0")+unit,labelStyle);
        float next=UiSlider(new Rect(25,y+31,510,20),current,min,max,step);
        next=Mathf.Round(next/step)*step;
        if(next!=setting.Value)setting.Value=next;
    }

    float BodykitTrack(int index,out float widthScale) {
        var rig=current.Profile.rig;var kit=current.styling?.ActiveOption("Bodykit");
        widthScale=index<2?(kit?.frontTyreScale??1):(kit?.rearTyreScale??1);
        float offset=index<2?(kit?.frontWheelOffset??0):(kit?.rearWheelOffset??0);
        float width=index<2?rig.tyreWidth:rig.rearTyreWidth;
        return WheelFitMath.StylingTrack(rig.TrackWidth(index),width,offset,widthScale);
    }
    void UpdateExtraRearPose(int index) {
        if(index<2||current.extraRearWheels==null)return;
        var shift=car.transform.forward*current.Profile.rig.extraRearAxleOffset;
        var source=visuals[index];var extra=current.extraRearWheels[index-2];extra.SetPositionAndRotation(source.position+shift,source.rotation);extra.localScale=source.localScale;
        var brake=brakeVisuals[index];current.extraRearBrakes[index-2].SetPositionAndRotation(brake.position+shift,brake.rotation);
    }
    void UpdateWheelVisual(int index) {
        var rig=current.Profile.rig;
        float track=BodykitTrack(index,out var widthScale);
        var anchor=wheels[index].transform.localPosition;anchor.x=(index%2==0?-1:1)*track;wheels[index].transform.localPosition=anchor;
        visuals[index].localScale=new Vector3(widthScale,1,1);
        Vector3 pos;Quaternion rot;
        if(Flying)current.flight.WheelPose(index,out pos,out rot);else wheels[index].GetWorldPose(out pos,out rot);
        float fold=Flying?current.flight.State.Blend:0;
        var vectoring=Flying?car.transform.rotation*Quaternion.Euler(current.flight.WheelPitch*fold,0,current.flight.WheelRoll*fold)*Quaternion.Inverse(car.transform.rotation):Quaternion.identity;
        if(!originalWheels) {
            var folding=Quaternion.AngleAxis(FlightState.WheelTilt(index,0,fold),Vector3.forward);
            visuals[index].SetPositionAndRotation(pos,vectoring*car.transform.rotation*folding*Quaternion.Inverse(car.transform.rotation)*rot*Quaternion.Euler(0,0,90));return;
        }
        float side=index%2==0?-1:1;
        float camber=WheelFitMath.Sanitize((index<2?frontCamber:rearCamber).Value,-15,10,0);
        float offset=WheelFitMath.Sanitize((index<2?frontWheelOffset:rearWheelOffset).Value,-10,20,0)*.01f;
        // Translation stays in the car frame; camber precedes the wheel's spin
        // so neither offset nor tilt rotates around the axle during driving.
        pos+=car.transform.up*wheelVisualLift+car.transform.forward*(index<2?frontWheelVisualOffset:rearWheelVisualOffset)
            +car.transform.right*(side*offset);
        pos+=car.transform.right*(side*.22f*fold);
        rot=Quaternion.Slerp(rot,car.transform.rotation,fold);
        var steering=car.transform.rotation*Quaternion.Euler(0,wheels[index].steerAngle*(1-fold),0);
        var tilt=Quaternion.AngleAxis(FlightState.WheelTilt(index,camber,fold),Vector3.forward);
        var mirror=Quaternion.Euler(0,index%2==0?180:0,0);
        visuals[index].SetPositionAndRotation(pos,vectoring*steering*tilt*Quaternion.Inverse(steering)*rot*mirror);
        if(brakeVisuals[index]!=null)brakeVisuals[index].SetPositionAndRotation(pos,vectoring*steering*tilt*mirror);
        UpdateExtraRearPose(index);
        if(current.spinners!=null&&current.spinners[index]!=null) {
            var motion=current.spinnerMotion[index];motion.Step(Time.deltaTime,wheels[index].rpm);
            current.spinners[index].SetPositionAndRotation(pos,vectoring*steering*tilt*Quaternion.AngleAxis(motion.Angle,Vector3.right)*mirror);
            float scale=WheelFitMath.Sanitize(RimSetting.Value,rig.rimMin,rig.rimMax,rig.rimNominal)/rig.rimNominal;
            current.spinners[index].localScale=new Vector3(1,scale,scale);
        }
    }
}
}
