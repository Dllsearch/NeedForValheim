using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace NeedForValheim {
// Uses the live character's chair animation and a small procedural limb/entry layer.
// Does not instantiate a Player clone or change equipment, inventory or network ownership.
internal sealed class VehicleCabinDriver:IDisposable {
    public readonly CabinTransition Motion=new CabinTransition();
    readonly Transform player,car,visual,hips;
    readonly Animator animator;
    readonly VehicleInterior interior;
    readonly Vector3 visualPosition,visualScale;
    readonly Quaternion visualRotation;
    readonly AnimatorCullingMode culling;
    readonly bool rootMotion;
    readonly Dictionary<string,float> floats=new Dictionary<string,float>();
    readonly Dictionary<string,bool> bools=new Dictionary<string,bool>();
    readonly Dictionary<string,Transform> bones;
    readonly Queue<Vector3> approach=new Queue<Vector3>();
    readonly CabinApproach approachWatch=new CabinApproach();
    readonly List<Tuple<Renderer,bool>> equipment=new List<Tuple<Renderer,bool>>();
    readonly List<Tuple<SkinnedMeshRenderer,bool>> skins=new List<Tuple<SkinnedMeshRenderer,bool>>();
    Vector3 outside,exitWorld,standingLocal;
    Quaternion standingRotation;
    bool disposed;
    float windowRest;
    readonly CabinControlPose controls=new CabinControlPose();
    float controlThrottle,controlBrake,controlSteer;
    int controlGear;
    bool controlHandbrake;
    public bool Failed {get;private set;}
    public Vector3 ExitPosition=>exitWorld;
    public void SetExitPosition(Vector3 point){exitWorld=point;}
    public VehicleCabinDriver(MonoBehaviour local,Transform car,VehicleInterior interior,Vector3 outsideWorld) {
        player=local.transform;this.car=car;this.interior=interior;
        visual=player.Find("Visual");
        animator=visual!=null?visual.GetComponentInChildren<Animator>():null;
        if(animator==null)throw new InvalidOperationException("Player animator unavailable for cabin pose");
        bones=visual.GetComponentsInChildren<Transform>(true).GroupBy(t=>t.name).ToDictionary(g=>g.Key,g=>g.First());
        if(!bones.TryGetValue("Hips",out hips))throw new InvalidOperationException("Player hip bone unavailable");
        visualPosition=visual.localPosition;visualScale=visual.localScale;visualRotation=visual.localRotation;
        culling=animator.cullingMode;rootMotion=animator.applyRootMotion;
        foreach(var p in animator.parameters) {
            if(p.type==AnimatorControllerParameterType.Bool && (p.name=="attach_chair"||p.name=="inWater"||p.name=="onGround"))bools[p.name]=animator.GetBool(p.name);
            if(p.type==AnimatorControllerParameterType.Float && (p.name=="forward_speed"||p.name=="sideway_speed"||p.name=="turn_speed"))floats[p.name]=animator.GetFloat(p.name);
        }
        if(!bools.ContainsKey("attach_chair"))throw new InvalidOperationException("Player chair animation parameter unavailable");
        interior.CreateControls(car.gameObject);
        animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.applyRootMotion=false;
        // Held items would pass through the wheel. Preserve their visibility without unequipping.
        foreach(var name in new[]{"LeftHand_Attach","RightHand_Attach"})if(bones.TryGetValue(name,out var attach))
            foreach(var r in attach.GetComponentsInChildren<Renderer>(true)){equipment.Add(Tuple.Create(r,r.enabled));r.enabled=false;}
        foreach(var r in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true)){skins.Add(Tuple.Create(r,r.updateWhenOffscreen));r.updateWhenOffscreen=true;}
        outside=car.InverseTransformPoint(outsideWorld);exitWorld=outsideWorld;
        standingLocal=car.InverseTransformPoint(player.position);standingRotation=Quaternion.Inverse(car.rotation)*player.rotation;
        // Route a character on the passenger side around the nearest end of the car.
        if(standingLocal.x>interior.ExitSide+.3f) {
            float end=standingLocal.z>=interior.SeatPosition.z?interior.FrontEnd:interior.RearEnd;
            approach.Enqueue(new Vector3(standingLocal.x,outside.y,end));
            approach.Enqueue(new Vector3(outside.x,outside.y,end));
        }
        approach.Enqueue(outside);
    }
    void Bool(string name,bool value){if(bools.ContainsKey(name))animator.SetBool(name,value);}
    void Float(string name,float value){if(floats.ContainsKey(name))animator.SetFloat(name,value);}
    public void SetControls(float throttle,float brake,bool handbrake,int gear,float steer=0) {
        controlThrottle=throttle;controlBrake=brake;controlHandbrake=handbrake;controlGear=gear;controlSteer=steer;
    }
    public bool BeginExit(Vector3 destination) {
        if(!Motion.BeginExit())return false;
        exitWorld=destination;outside=car.InverseTransformPoint(destination);return true;
    }
    public void Tick(float dt,bool blocked,ShowroomState doors) {
        if(disposed)return;
        Motion.Step(dt,blocked,doors?.Progress[1]??0,doors!=null);
        Motion.ApplyDoorRequest(doors);
        controls.Step(blocked?0:dt,controlThrottle,controlBrake,controlHandbrake,controlGear,Motion.CanDrive);
        interior.ApplyControls(controls);
        float blend=Motion.SeatBlend;
        Bool("attach_chair",blend>.12f);Bool("inWater",false);Bool("onGround",true);
        Float("sideway_speed",0);Float("turn_speed",0);Float("forward_speed",0);
        visual.localPosition=visualPosition;visual.localRotation=visualRotation;
        visual.localScale=visualScale*Mathf.Lerp(1,interior.DriverScale,blend);
        if(Motion.Phase==CabinPhase.Approach) {
            if(!blocked&&approach.Count>0) {
                var target=approach.Peek();var next=Vector3.MoveTowards(standingLocal,target,Mathf.Min(dt,.1f)*1.65f);
                var distance=target-standingLocal;distance.y=0;
                if(approachWatch.Step(dt,distance.magnitude,false)){ArriveAtDoor();return;}
                var world=car.TransformPoint(next);
                var ground=Physics.RaycastAll(world+Vector3.up*1.5f,Vector3.down,3,~0,QueryTriggerInteraction.Ignore)
                    .Where(h=>!h.collider.transform.IsChildOf(player)&&!h.collider.transform.IsChildOf(car)).OrderBy(h=>h.distance).ToArray();
                if(ground.Length==0||ground[0].normal.y<.65f){ArriveAtDoor();return;}
                world.y=ground[0].point.y+.06f;next=car.InverseTransformPoint(world);
                if(!ClearCapsule(world)){ArriveAtDoor();return;}
                var direction=target-standingLocal;direction.y=0;
                if(direction.sqrMagnitude>.0001f)standingRotation=Quaternion.RotateTowards(standingRotation,Quaternion.LookRotation(direction),dt*360);
                standingLocal=next;Float("forward_speed",1.65f);
                var remaining=next-target;remaining.y=0;if(remaining.magnitude<.04f){approach.Dequeue();approachWatch.NextWaypoint();}
            }
            player.SetPositionAndRotation(car.TransformPoint(standingLocal),car.rotation*standingRotation);
            if(approach.Count==0)Motion.Arrive();return;
        }
        // Turn into the doorway and lower into the seat while the native chair pose blends.
        var seat=interior.SeatPosition;
        var seatRoot=seat-new Vector3(0,.7f,0);
        player.SetPositionAndRotation(car.TransformPoint(Vector3.Lerp(outside,seatRoot,blend)),
            car.rotation*Quaternion.Slerp(Quaternion.Euler(0,Motion.Phase==CabinPhase.Exit||Motion.Phase==CabinPhase.Complete?-90:90,0),Quaternion.identity,blend));
        PoseSeat(blend,seat,dt,blocked,doors,Motion.CanDrive);
    }
    public void TickObserver(float dt,float blend,ShowroomState doors) {
        if(disposed||visual==null||animator==null)return;
        blend=float.IsNaN(blend)||float.IsInfinity(blend)?0:Mathf.Clamp01(blend);
        controls.Step(dt,controlThrottle,controlBrake,controlHandbrake,controlGear,blend>.95f);interior.ApplyControls(controls);
        Bool("attach_chair",blend>.12f);Bool("inWater",false);Bool("onGround",true);
        Float("sideway_speed",0);Float("turn_speed",0);Float("forward_speed",0);
        visual.localPosition=visualPosition;visual.localRotation=visualRotation;
        visual.rotation=Quaternion.Slerp(visual.rotation,car.rotation*visualRotation,blend);
        visual.localScale=visualScale*Mathf.Lerp(1,interior.DriverScale,blend);
        PoseSeat(blend,interior.SeatPosition,dt,false,doors,blend>.95f);
    }
    void PoseSeat(float blend,Vector3 seat,float dt,bool blocked,ShowroomState doors,bool canDrive) {
        if(blend<=0)return;

            // Align the actual pelvis, allowing for gender, equipment and the native chair pose.
            visual.position+= (car.TransformPoint(seat)-hips.position)*blend;
            if(bones.TryGetValue("Head",out var head)) {
                float headHeight=car.InverseTransformPoint(head.position).y;
                float scale=visual.lossyScale.y/car.lossyScale.y;
                visual.position-=car.up*CabinAppearance.RoofLowering(headHeight,scale,interior.HeadCeiling)*blend;
            }
            // Withdraw the resting arm before opening the door; never follow a scissor door upwards.
            float restTarget=interior.SupportsWindowRest&&canDrive&&Mathf.Abs(controlThrottle)<.05f&&Mathf.Abs(controlSteer)<.05f&&
                doors!=null&&!doors.Open[1]&&doors.Progress[1]<.03f?1:0;
            float poseDt=blocked&&!canDrive?0:Mathf.Min(dt,.1f);
            windowRest=Mathf.MoveTowards(windowRest,restTarget,poseDt*5);
            var sill=seat+new Vector3(-.426f,.47f,.33f);
            var leftPole=seat+Vector3.Lerp(new Vector3(-.25f,.2f,0),new Vector3(-.436f,.48f,-.03f),windowRest);
            Limb("LeftArm","LeftForeArm","LeftHand",Vector3.Lerp(interior.LeftGrip,car.TransformPoint(sill),windowRest),leftPole,blend);
            var rightTarget=Vector3.Lerp(interior.RightGrip,interior.ShiftGrip,controls.Shift);
            rightTarget=Vector3.Lerp(rightTarget,interior.HandbrakeGrip,controls.Handbrake);
            Limb("RightArm","RightForeArm","RightHand",rightTarget,seat+new Vector3(.30f,.30f,.20f),blend);
            AimHand("RightHand","RightHandMiddle1",Vector3.Lerp(interior.RightHandForward,car.up,Mathf.Max(controls.Shift,controls.Handbrake)),blend);
            AimHand("LeftHand","LeftHandMiddle1",Vector3.Lerp(interior.RightHandForward,car.forward,windowRest),blend);
            Limb("LeftUpLeg","LeftLeg","LeftFoot",interior.LeftFoot,seat+new Vector3(-.12f,.11f,.35f),blend);
            Limb("RightUpLeg","RightLeg","RightFoot",Vector3.Lerp(interior.RightFoot,interior.BrakeFoot,controls.Brake),seat+new Vector3(.085f,.11f,.35f),blend);

    }
    void ArriveAtDoor() {
        // The path may cross rocks or a wall. Recheck the destination itself;
        // never complete entry by placing the avatar in an occupied capsule.
        var origin=car.TransformPoint(outside);
        var hits=Physics.RaycastAll(origin+Vector3.up*1.5f,Vector3.down,3,~0,QueryTriggerInteraction.Ignore)
            .Where(h=>!h.collider.transform.IsChildOf(player)&&!h.collider.transform.IsChildOf(car)).OrderBy(h=>h.distance).ToArray();
        if(hits.Length==0||hits[0].normal.y<.65f){Failed=true;return;}
        var foot=hits[0].point+Vector3.up*.06f;
        if(!ClearCapsule(foot)){Failed=true;return;}
        outside=standingLocal=car.InverseTransformPoint(foot);exitWorld=foot;approach.Clear();
        player.SetPositionAndRotation(foot,car.rotation*Quaternion.Euler(0,90,0));
        Float("forward_speed",0);Motion.Arrive();
    }
    void AimHand(string handName,string fingerName,Vector3 direction,float blend) {
        if(!bones.TryGetValue(handName,out var hand)||!bones.TryGetValue(fingerName,out var finger))return;
        var current=finger.position-hand.position;
        if(current.sqrMagnitude<.00001f)return;
        hand.rotation=Quaternion.Slerp(hand.rotation,Quaternion.FromToRotation(current,direction)*hand.rotation,blend);
    }
    // Two-bone IK keeps limb lengths unchanged; pole positions control knee/elbow bend.
    void Limb(string upperName,string lowerName,string endName,Vector3 target,Vector3 pole,float blend) {
        if(!bones.TryGetValue(upperName,out var a)||!bones.TryGetValue(lowerName,out var b)||!bones.TryGetValue(endName,out var end))return;
        var startA=a.rotation;var startB=b.rotation;var endRotation=end.rotation;
        float first=Vector3.Distance(a.position,b.position),second=Vector3.Distance(b.position,end.position);
        Vector3 direction=target-a.position;float distance=Mathf.Clamp(direction.magnitude,Mathf.Abs(first-second)+.001f,first+second-.001f);
        if(first<.001f||second<.001f||direction.sqrMagnitude<.00001f)return;
        direction.Normalize();var bend=Vector3.ProjectOnPlane(car.TransformPoint(pole)-a.position,direction).normalized;
        float along=(first*first-second*second+distance*distance)/(2*distance);
        Vector3 knee=a.position+direction*along+bend*Mathf.Sqrt(Mathf.Max(0,first*first-along*along));
        a.rotation=Quaternion.FromToRotation(b.position-a.position,knee-a.position)*a.rotation;
        b.rotation=Quaternion.FromToRotation(end.position-b.position,target-b.position)*b.rotation;
        var finalA=a.rotation;var finalB=b.rotation;
        a.rotation=Quaternion.Slerp(startA,finalA,blend);b.rotation=Quaternion.Slerp(startB,finalB,blend);end.rotation=endRotation;
    }
    bool ClearCapsule(Vector3 foot) {
        return !Physics.OverlapCapsule(foot+Vector3.up*.32f,foot+Vector3.up*1.5f,.27f,~0,QueryTriggerInteraction.Ignore)
            .Any(c=>!c.transform.IsChildOf(player)&&!c.transform.IsChildOf(car));
    }
    public void Dispose() {
        if(disposed)return;disposed=true;
        interior.ApplyControls(new CabinControlPose());
        if(animator!=null) {
            foreach(var p in bools)animator.SetBool(p.Key,p.Value);
            foreach(var p in floats)animator.SetFloat(p.Key,p.Value);
            animator.cullingMode=culling;animator.applyRootMotion=rootMotion;
        }
        if(visual!=null){visual.localPosition=visualPosition;visual.localRotation=visualRotation;visual.localScale=visualScale;}
        foreach(var item in equipment)if(item.Item1!=null)item.Item1.enabled=item.Item2;
        foreach(var item in skins)if(item.Item1!=null)item.Item1.updateWhenOffscreen=item.Item2;
    }
}
}
