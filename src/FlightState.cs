using System;
namespace NeedForValheim {
public enum FlightPhase { Ground, Folding, Flying, Landing, Unfolding }
// Lifecycle independent of Unity; a ground-mode request keeps lift until touchdown.
public sealed class FlightState {
    public FlightPhase Phase {get;private set;}
    public float Fold {get;private set;}
    public bool Active=>Phase!=FlightPhase.Ground;
    public bool Landing=>Phase==FlightPhase.Landing||Phase==FlightPhase.Unfolding;
    public bool CanPilot=>Phase==FlightPhase.Flying||Phase==FlightPhase.Landing;
    public float Blend=>Fold*Fold*(3-2*Fold);
    public void Resume(bool landing,float blend) {
        blend=float.IsNaN(blend)||float.IsInfinity(blend)?1:Math.Max(0,Math.Min(1,blend));
        // Telemetry contains smoothed visual blend; recover its monotonic input.
        float low=0,high=1;
        for(int i=0;i<16;i++){float middle=(low+high)*.5f;if(middle*middle*(3-2*middle)<blend)low=middle;else high=middle;}
        Fold=(low+high)*.5f;
        Phase=landing?FlightPhase.Landing:blend>=.999f?FlightPhase.Flying:FlightPhase.Folding;
    }
    public void Fly(){if(Phase!=FlightPhase.Flying)Phase=FlightPhase.Folding;}
    public void Land(){if(Active&&!Landing)Phase=FlightPhase.Landing;}
    public void Step(float dt,bool nearGround) {
        if(float.IsNaN(dt)||float.IsInfinity(dt)||dt<=0||!Active)return;
        dt=Math.Min(dt,.1f);
        if(Phase==FlightPhase.Unfolding&&!nearGround)Phase=FlightPhase.Landing;
        if(Phase==FlightPhase.Unfolding) {
            Fold=Math.Max(0,Fold-dt/1.2f);
            if(Fold<=0)Phase=FlightPhase.Ground;
        } else {
            Fold=Math.Min(1,Fold+dt/1.2f);
            if(Phase==FlightPhase.Folding&&Fold>=1)Phase=FlightPhase.Flying;
            if(Phase==FlightPhase.Landing&&nearGround)Phase=FlightPhase.Unfolding;
        }
    }
    public static float Axis(bool positive,bool negative,bool allowed)=>allowed?(positive?1:0)-(negative?1:0):0;
    public static float WheelTilt(int index,float camber,float blend)=>-(index%2==0?-1:1)*(camber+(-90-camber)*blend);
    public static float Acceleration(float desired,float actual,float response,float limit) {
        if(float.IsNaN(desired)||float.IsInfinity(desired)||float.IsNaN(actual)||float.IsInfinity(actual))return 0;
        return Math.Max(-limit,Math.Min(limit,(desired-actual)*response));
    }
}
}
