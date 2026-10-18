using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace NeedForValheim {
public partial class Plugin {
    delegate void GroundCameraPosition(float dt,out Vector3 position,out Quaternion rotation);
    GroundCameraPosition menuGroundCamera;
    bool menuMouseLookHeld;
    Vector3 showroomCameraPosition;
    Quaternion showroomCameraRotation;
    float showroomYaw=145,showroomPitch=18,showroomDistance=6;

    void BeginMenuCamera() {
        menuMouseLookHeld=false;menuGroundCamera=null;
        if(menuCamera==null)return;
        showroomCameraPosition=menuCamera.transform.position;showroomCameraRotation=menuCamera.transform.rotation;
        showroomYaw=145;showroomPitch=18;showroomDistance=current.Profile.capabilities.suvTuning?8:6;
        // Keep the native camera disabled (its LateUpdate captures the cursor).
        // Reuse only its collision-aware positioning, without native zoom/input.
        var method=menuCamera.GetType().GetMethod("GetCameraPosition",BindingFlags.Instance|BindingFlags.NonPublic);
        if(method!=null)menuGroundCamera=(GroundCameraPosition)Delegate.CreateDelegate(typeof(GroundCameraPosition),menuCamera,method);
    }
    void EndMenuCamera(){menuMouseLookHeld=false;menuGroundCamera=null;if(menuCamera!=null)menuCamera.transform.SetPositionAndRotation(showroomCameraPosition,showroomCameraRotation);}
    void TickMenuCamera() {
        Vector2 degrees=Vector2.zero;
        float scale=Mathf.Min(Screen.width/1600f,Screen.height/900f);
        float pointerX=(Input.mousePosition.x-(Screen.width-1600*scale)*.5f)/scale;
        float pointerY=(Screen.height-Input.mousePosition.y-(Screen.height-900*scale)*.5f)/scale;
        bool inPreview=GaragePreviewRect.Contains(new Vector2(pointerX,pointerY));
        bool mouseLook=Input.GetMouseButton(1)&&!Input.GetMouseButton(0)&&!menuPlacement.Dragging&&(menuMouseLookHeld||inPreview);
        float scroll=inPreview?Input.mouseScrollDelta.y:0;
        if(mouseLook) {
            // Ignore cursor recentering on the first captured frame.
            if(menuMouseLookHeld) {
                degrees=orbitCamera.ReadMouseDelta()*menuMouseSensitivity.Value;
                if(menuMouseInvert.Value)degrees.y=-degrees.y;
            }
        }else if(pad!=null) {
            var look=pad.rightStick.ReadUnprocessedValue();
            degrees=new Vector2(ControlRules.Axis(look.x,stickDeadzone.Value),ControlRules.Axis(look.y,stickDeadzone.Value))*menuControllerSensitivity.Value*Mathf.Min(Time.unscaledDeltaTime,.1f);
            if(menuControllerInvert.Value)degrees.y=-degrees.y;
        }
        menuMouseLookHeld=mouseLook;
        Cursor.lockState=mouseLook?CursorLockMode.Locked:CursorLockMode.None;Cursor.visible=!mouseLook;
        GarageCameraInput(degrees,scroll);
    }
    void LateMenuCamera() {
        if(!menuOpen||driving||audioBlocked||!Application.isFocused||menuCamera==null)return;
        if(CanAccessCurrent(LocalPlayer())) {
            var pivot=car.transform.position+Vector3.up*(current.Profile.capabilities.suvTuning?1.2f:.8f);
            var orbit=Quaternion.Euler(showroomPitch,car.transform.eulerAngles.y+showroomYaw,0);
            var delta=orbit*Vector3.back*showroomDistance;float distance=showroomDistance;
            foreach(var hit in Physics.SphereCastAll(pivot,.18f,delta.normalized,distance,~0,QueryTriggerInteraction.Ignore))
                if(!hit.transform.IsChildOf(car.transform)&&!hit.transform.IsChildOf(LocalPlayer().transform))distance=Mathf.Min(distance,Mathf.Max(.8f,hit.distance-.1f));
            var point=pivot+delta.normalized*distance;
            // The menu occupies the left side; keep the preview to its right.
            var facing=Quaternion.LookRotation(pivot-point);
            float offset=Screen.width>1000?-12:0;
            menuCamera.transform.SetPositionAndRotation(point,facing*Quaternion.Euler(0,offset,0));return;
        }
        if(menuGroundCamera==null)return;
        menuGroundCamera(Time.unscaledDeltaTime,out var position,out var rotation);
        menuCamera.transform.SetPositionAndRotation(position,rotation);
    }
    internal bool CaptureMenuLook(PlayerController controller)=>ready&&menuOpen&&controller.GetComponent<Player>()==LocalPlayer()&&!Multiplayer();
}

// PlayerController's LateUpdate independently reads look input. Suppress it
// while our menu handles look, avoiding double rotation and mouse hover drift.
[HarmonyPatch(typeof(PlayerController),"LateUpdate")]
static class NativeMenuLookCapture {
    static bool Prefix(PlayerController __instance)=>Plugin.ControlsOwner==null||!Plugin.ControlsOwner.CaptureMenuLook(__instance);
}
[HarmonyPatch(typeof(Menu),"Show")]
static class NativePauseDuringGarage {
    static bool Prefix()=>Plugin.ControlsOwner==null||!Plugin.ControlsOwner.FrontendOpen;
}
}
