using System;
using System.Collections.Generic;

namespace NeedForValheim {
// Physical controls, deliberately independent of Valheim's remappable combat
// bindings. Sprite names match ZInput's xbox/ps5 sprite map.
public static class VehicleHintContent {
    public static string Glyph(string xbox,string ps,bool playstation)=>"<sprite=\""+(playstation?"ps5":"xbox")+"\" name=\""+(playstation?ps:xbox)+"\">";
    public static string[] Build(bool controller,bool ps,bool flight,bool menu,bool nitro,string menuKey,Func<string,string> tr) {
        var rows=new List<string>();
        Func<string,string,string> icon=(x,p)=>Glyph(x,p,ps);
        Action<string,string> row=(key,label)=>rows.Add(key+"  "+tr(label));
        string close=controller?icon("button_ls","button_l3"):menuKey;
        if(menu) {
            row(close,"Close menu");
            if(controller)row(icon("button_b","button_circle"),"Back");
            row(controller?icon("button_a","button_cross"):"LMB",controller?"Select / edit":"Select");
            row(controller?icon("lstick","lstick"):"↑ / ↓",controller?"Navigation":"Category");
            if(controller)row(icon("button_lb","button_l1")+" / "+icon("button_rb","button_r1"),"Categories / Content");
            row(controller?icon("rstick","rstick"):"RMB","Camera");
            return rows.ToArray();
        }
        row(controller?icon("button_rt","button_r2"):"W",flight?"Forward":"Throttle");
        row(controller?icon("button_lt","button_l2"):"S",flight?"Reverse":"Brake / reverse");
        row(controller?icon("lstick","lstick"):"A / D","Steer");
        row(controller?icon("rstick","rstick"):tr("Mouse"),"Camera");
        if(flight) {
            row(controller?icon("button_rb","button_r1"):"Shift","Ascend");
            row(controller?icon("button_lb","button_l1"):tr("Space"),"Descend");
        }else {
            row(controller?icon("button_a","button_cross"):tr("Space"),"Handbrake");
            row(controller?icon("button_lb","button_l1"):"RMB","Fire turret");
        }
        if(nitro||flight)row(controller?icon("button_x","button_square"):"LMB / "+tr("Ctrl"),"Nitro");
        row(controller?icon("button_b","button_circle"):"E / F7",flight?"Land & exit":"Exit");
        row(controller?icon("dpad_left","dpad_left")+" / "+icon("dpad_right","dpad_right"):"← / →","Songs");
        row(controller?icon("dpad_up","dpad_up")+" / "+icon("dpad_down","dpad_down"):"1–4","Drive mode");
        row(controller?close:"Tab / "+close,"Mod menu");
        row(controller?icon("button_rs","button_r3"):"H","Horn");
        if(flight)row("", "Turret: automatic");
        return rows.ToArray();
    }
}
}
