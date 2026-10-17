using System;
using NeedForValheim;
static class Program {
    static int checks;
    static void Check(bool value,string label){if(!value)throw new Exception(label);checks++;Console.WriteLine("PASS "+label);}
    static void Main() {
        Func<string,string> de=s=>InterfaceText.Translate(s,true),en=s=>InterfaceText.Translate(s,false);
        var keyboardHelp=string.Join("\n",VehicleHintContent.Build(false,false,false,false,true,"Tab",en));
        Check(keyboardHelp.Contains("E  Exit")&&keyboardHelp.Contains("Tab  Mod menu")&&!keyboardHelp.Contains("F7")&&!keyboardHelp.Contains("F9"),"keyboard help uses native interaction and seated Tab without legacy function keys");
        var xbox=string.Join("\n",VehicleHintContent.Build(true,false,false,false,nitro:true,"F9",de));
        Check(xbox.Contains("name=\"button_lb\">  Geschütz feuern")&&xbox.Contains("name=\"button_x\">  Nitro")&&xbox.Contains("name=\"button_a\">  Handbremse"),"Xbox ground hints match physical turret, nitro and handbrake bindings");
        var ps=string.Join("\n",VehicleHintContent.Build(true,true,false,false,nitro:true,"F9",en));
        Check(ps.Contains("<sprite=\"ps5\" name=\"button_l1\">  Fire turret")&&ps.Contains("name=\"button_square\">  Nitro")&&ps.Contains("name=\"button_cross\">  Handbrake")&&!ps.Contains("xbox"),"PS5 uses native L1, square and cross glyph names");
        var flying=string.Join("\n",VehicleHintContent.Build(true,false,true,false,nitro:true,"F9",en));
        Check(flying.Contains("name=\"button_lb\">  Descend")&&flying.Contains("name=\"button_rb\">  Ascend")&&!flying.Contains("Fire turret")&&flying.Contains("Turret: automatic"),"flight hints replace manual turret with altitude controls and automatic fire");
        Check(flying.Contains("name=\"button_b\">  Land & exit"),"flight exit hint describes landing first");
        var menu=string.Join("\n",VehicleHintContent.Build(true,true,true,true,nitro:true,"F9",en));
        Check(menu.Contains("Close menu")&&menu.Contains("Categories / Content")&&menu.Contains("Select / edit")&&menu.Contains("Camera")&&!menu.Contains("Nitro")&&!menu.Contains("Fire turret"),"open menu hints describe pane focus and editing without driving actions");
        var keyboard=string.Join("\n",VehicleHintContent.Build(false,false,true,false,nitro:true,"M",en));
        Check(!keyboard.Contains("<sprite")&&keyboard.Contains("M  Mod menu")&&keyboard.Contains("Shift  Ascend")&&keyboard.Contains("Space  Descend"),"keyboard flight hints use actual keys and configured menu shortcut");
        Check(!string.Join("\n",VehicleHintContent.Build(true,false,false,false,nitro:false,"F9",de)).Contains("Nitro"),"H2 ground hints omit unavailable nitro");
        Check(string.Join("\n",VehicleHintContent.Build(true,true,true,false,nitro:false,"F9",de)).Contains("Nitro"),"H2 flight hints retain available nitro");
        Check(ControlRules.Axis(.1f,.16f)==0&&ControlRules.Axis(-.1f,.16f)==0,"stick drift is neutral in both directions");
        Check(ControlRules.Axis(1,.16f)==1&&ControlRules.Axis(-1,.16f)==-1,"full stick retains full steering range");
        Check(ControlRules.Axis(.58f,.16f)>.49f&&ControlRules.Axis(.58f,.16f)<.51f,"partial stick remains analog");
        Check(ControlRules.Axis(float.NaN,.16f)==0&&ControlRules.Axis(float.PositiveInfinity,.16f)==0,"invalid stick values cannot reach physics");
        ControlRules.Pedals(.4f,0,10,out float throttle,out float brake);
        Check(Math.Abs(throttle-.4f)<.001&&brake==0,"RT/R2 applies proportional forward throttle");
        ControlRules.Pedals(0,.3f,10,out throttle,out brake);
        Check(throttle==0&&Math.Abs(brake-.3f)<.001,"LT/L2 first applies proportional brakes");
        ControlRules.Pedals(0,.3f,0,out throttle,out brake);
        Check(throttle<0&&brake==0,"LT/L2 reverses after stopping");
        ControlRules.Pedals(.7f,0,-5,out throttle,out brake);
        Check(throttle==0&&Math.Abs(brake-.7f)<.001,"RT/R2 brakes reverse travel before forward drive");
        foreach(float pressure in new[]{.2f,1f}) {
            ControlRules.Pedals(pressure,0,-.9f,out throttle,out brake);
            Check(throttle==pressure&&brake==0,"controller forward hill start catches slow rollback at trigger pressure "+pressure);
            ControlRules.Pedals(0,pressure,.9f,out throttle,out brake);
            Check(throttle==-pressure&&brake==0,"controller reverse hill start catches slow forward roll at trigger pressure "+pressure);
            ControlRules.Pedals(pressure,0,-3,out throttle,out brake);
            Check(throttle==0&&brake==pressure,"controller direction braking stays proportional at trigger pressure "+pressure);
        }
        ControlRules.Pedals(.8f,.3f,3,out throttle,out brake);
        Check(throttle==0&&brake==.3f,"both triggers while rolling forward preserve reverse-trigger brake priority");
        ControlRules.Pedals(.8f,.3f,-3,out throttle,out brake);
        Check(throttle==0&&brake==.8f,"both triggers while reversing preserve gas-trigger brake priority");
        ControlRules.Pedals(.8f,.3f,0,out throttle,out brake);
        Check(Math.Abs(throttle-.5f)<.001f&&brake==0,"both triggers at rest retain their analogue difference");
        var gate=new ReleaseGate();gate.Block();
        Check(!gate.Allow(true)&&!gate.Allow(true),"held gas/nitro cannot resume after a menu or focus interruption");
        Check(gate.Allow(false)&&gate.Allow(true),"release permits fresh control input");
        Check(ControlRules.CycleMode(1,1,false)==2&&ControlRules.CycleMode(2,1,false)==3,"350Z cycles road drift and flight");
        Check(ControlRules.CycleMode(1,1,true)==3&&ControlRules.CycleMode(3,-1,true)==1,"H2 skips unavailable drift both ways");
        Check(ControlRules.CycleMode(3,1,false)==0&&ControlRules.CycleMode(0,-1,false)==3,"mode cycling wraps consistently");
        Check(ControlRules.AutomaticTurret(true,true,true,false),"flight always uses auto aim even if parked guard is disabled");
        Check(!ControlRules.AutomaticTurret(false,true,true,true),"ground driving remains manual");
        Check(ControlRules.AutomaticTurret(false,false,true,true),"existing parked guard remains available");
        Check(!ControlRules.AutomaticTurret(false,false,false,true),"fresh unoccupied cars do not fire");
        Check(ControlRules.MenuMove(0,-1,5)==4&&ControlRules.MenuMove(4,1,5)==0,"controller menu focus wraps");
        Check(ControlRules.MenuMove(0,1,0)==0,"empty or disabled menu is safe");
        for(int tab=0;tab<13;tab++)Check(ControlRules.MenuTabVisible(tab,false)==(tab==10||tab==11),"without a nearby car only garage and interface settings are visible: "+tab);
        Check(ControlRules.MenuNextTab(10,1,13,false)==11&&ControlRules.MenuNextTab(11,1,13,false)==10&&ControlRules.MenuNextTab(10,-1,13,false)==11,"no-car navigation cycles only visible entries in both directions");
        Check(ControlRules.MenuNextTab(11,1,13,true)==12&&ControlRules.MenuNextTab(12,1,13,true)==0&&ControlRules.MenuNextTab(0,-1,13,true)==12,"vehicle navigation includes exhaust and wraps without hardcoded tab counts");
        var focus=new MenuFocusState();
        Check(focus.Sidebar&&!focus.Editing,"menu begins in sidebar");
        Check(!focus.Confirm(false)&&!focus.Sidebar,"confirm enters selected category content without triggering first action");
        Check(focus.Confirm(false),"confirm on an ordinary content control invokes it");
        Check(!focus.Confirm(true)&&focus.Editing,"confirm starts editing a setting without changing it");
        Check(!focus.Back()&&!focus.Editing&&!focus.Sidebar,"back finishes editing while keeping content focus");
        Check(!focus.Back()&&focus.Sidebar&&focus.Back(),"back returns to sidebar then closes");
        focus.SelectPane(false);focus.Confirm(true);focus.SelectPane(true);
        Check(focus.Sidebar&&!focus.Editing,"LB immediately returns to sidebar and leaves editing");
        focus.SelectPane(false);focus.Confirm(true);focus.Confirm(true);
        Check(!focus.Sidebar&&!focus.Editing,"confirm again finishes editing without losing content focus");
        var bounds=new[]{new MenuControlBounds(100,200,32),new MenuControlBounds(300,200,32),new MenuControlBounds(100,250,32),new MenuControlBounds(100,300,32)};
        Check(ControlRules.MenuHorizontalNeighbor(0,1,bounds)==1&&ControlRules.MenuHorizontalNeighbor(1,-1,bounds)==0,"left/right navigate buttons sharing a row");
        Check(ControlRules.MenuHorizontalNeighbor(2,-1,bounds)==-1&&ControlRules.MenuHorizontalNeighbor(2,1,bounds)==-1,"row edges do not jump into unrelated controls");
        Check(ControlRules.MenuHorizontalNeighbor(5,-1,bounds)==-1&&ControlRules.MenuHorizontalNeighbor(0,-1,Array.Empty<MenuControlBounds>())==-1,"disappearing controls cannot break horizontal navigation");
        var scrolled=new[]{new MenuControlBounds(100,200,32),new MenuControlBounds(300,200,32,1)};
        Check(ControlRules.MenuHorizontalNeighbor(0,1,scrolled)==-1,"clipped song rows cannot become horizontal neighbors of controls outside their scroll region");
        Check((InterfaceText.Translate("Window tint   75 %",false)=="Window tint   75 %"&&InterfaceText.Translate("Window tint   75 %",true)=="Scheibentönung   75 %"),"English dynamic tuning label retains the numeric value");
        Check(InterfaceText.Translate("Fahrmodus",true)=="Fahrmodus","German mode preserves authored text");
        Check((InterfaceText.Translate("Trunk neon",false)=="Trunk neon"&&InterfaceText.Translate("Trunk neon",true)=="Kofferraum-Neon"),"specific labels translate before general fragments");
        Check((InterfaceText.Translate("X: Nitro · B: exit",false)=="X: Nitro · B: exit"&&InterfaceText.Translate("X: Nitro · B: exit",true)=="X: Nitro · B: aussteigen"),"Xbox hint keeps button names");
        Check((InterfaceText.Translate("Square: Nitro · Circle: exit",false)=="Square: Nitro · Circle: exit"&&InterfaceText.Translate("Square: Nitro · Circle: exit",true)=="Quadrat: Nitro · Kreis: aussteigen"),"PS5 hint uses matching button names");
        Check((InterfaceText.Translate("Green",false)=="Green"&&InterfaceText.Translate("Green",true)=="Grün")&&(InterfaceText.Translate("Road",false)=="Road"&&InterfaceText.Translate("Road",true)=="Straße"),"Unicode translation keys remain intact");
        Check((InterfaceText.Translate("Nissan 350Z",false)=="Nissan 350Z"&&InterfaceText.Translate("Nissan 350Z",true)=="Nissan 350Z"),"vehicle identifiers are not rewritten");
        Check((InterfaceText.Translate("The forest awakens – a horde attacks!",false)=="The forest awakens – a horde attacks!"&&InterfaceText.Translate("The forest awakens – a horde attacks!",true)=="Der Wald erwacht – eine Horde greift an!"),"English horde announcement");
        Check((InterfaceText.Translate("Face the horde",false)=="Face the horde"&&InterfaceText.Translate("Face the horde",true)=="Stelle dich der Horde")&&(InterfaceText.Translate("Numpad 9 · Begin the battle",false)=="Numpad 9 · Begin the battle"&&InterfaceText.Translate("Numpad 9 · Begin the battle",true)=="Numpad 9 · Kampf beginnen"),"English discovery hint retains the start key");
        Check((InterfaceText.Translate("Open storage · 12 / 24",false)=="Open storage · 12 / 24"&&InterfaceText.Translate("Open storage · 12 / 24",true)=="Stauraum öffnen · 12 / 24"),"cargo access label retains occupancy");
        Check((InterfaceText.Translate("Cargo remains in a chest at the vehicle's location.",false)=="Cargo remains in a chest at the vehicle's location."&&InterfaceText.Translate("Cargo remains in a chest at the vehicle's location.",true)=="Der Inhalt bleibt als Kiste am Fahrzeugstandort."),"cargo recovery notice is translated");
        foreach(var pact in new[]{HordePact.None,HordePact.Odin,HordePact.Thor,HordePact.Iron,HordePact.Frost,HordePact.Storm,HordePact.Valkyrie})
            foreach(var text in new[]{HordePacts.Name(pact),HordePacts.Benefit(pact),HordePacts.Cost(pact)}) {
                Check(InterfaceText.Translate(text,true)!=text,"pact text has a German translation: "+text);
                Check(InterfaceText.Translate(text,false)==text,"English source pact text stays intact: "+text);
            }
        Check((InterfaceText.Translate("Hunt complete: +500 points and +250 HP!",false)=="Hunt complete: +500 points and +250 HP!"&&InterfaceText.Translate("Hunt complete: +500 points and +250 HP!",true)=="Jagdauftrag erfüllt: +500 Punkte und +250 HP!"),"English hunt reward keeps exact values");
        Check((InterfaceText.Translate("The Forest Tyrant challenges you!",false)=="The Forest Tyrant challenges you!"&&InterfaceText.Translate("The Forest Tyrant challenges you!",true)=="Der Waldtyrann fordert dich heraus!"),"English boss announcement");
        Check(InterfaceText.Translate("Der Wald erwacht – eine Horde greift an!",true)=="Der Wald erwacht – eine Horde greift an!","German horde announcement");
        var waveMessages=new[]{"Etwas regt sich im Unterholz …","Der Wald erwacht!","Der Wald schickt seine Krieger!","Schwere Schritte erschüttern den Boden …","Der Zorn des Waldes ist entfesselt!","Der Wald verstummt. Alle fünf Wellen besiegt!","Nächste Welle in","Gegner übrig","Welle"};
        var englishMessages=new[]{"Something stirs in the undergrowth …","The forest awakens!","The forest sends its warriors!","Heavy footsteps shake the ground …","The wrath of the forest is unleashed!","The forest falls silent. All five waves defeated!","Next wave in","Enemies remaining","Wave"};
        for(int i=0;i<waveMessages.Length;i++) {
            Check(InterfaceText.Translate(englishMessages[i],false)==englishMessages[i],"wave announcement/HUD English "+i);
            Check(InterfaceText.Translate(englishMessages[i],true)==waveMessages[i],"wave announcement/HUD German "+i);
        }
        Check((InterfaceText.Translate("No clear ground for the horde. Find an open area.",false)=="No clear ground for the horde. Find an open area."&&InterfaceText.Translate("No clear ground for the horde. Find an open area.",true)=="Kein freier Boden für die Horde. Suche eine offene Fläche."),"English blocked horde placement");
        Check((InterfaceText.Translate("Horde mode: off-road driving only.",false)=="Horde mode: off-road driving only."&&InterfaceText.Translate("Horde mode: off-road driving only.",true)=="Hordenmodus: nur Gelände verfügbar."),"English horde mode lock");
        Check((InterfaceText.Translate("Explosive ammunition: 30 rounds!",false)=="Explosive ammunition: 30 rounds!"&&InterfaceText.Translate("Explosive ammunition: 30 rounds!",true)=="Explosivmunition: 30 Schuss!"),"English explosive pickup");
        Check((InterfaceText.Translate("The horde has been removed.",false)=="The horde has been removed."&&InterfaceText.Translate("The horde has been removed.",true)=="Die Horde wurde entfernt."),"reset message no longer says test horde");
        Check((InterfaceText.Translate("Auto turret: 15 seconds!",false)=="Auto turret: 15 seconds!"&&InterfaceText.Translate("Auto turret: 15 seconds!",true)=="Autoturret: 15 Sekunden!"),"English timed auto pickup");
        Check((InterfaceText.Translate("Shield restored: +150",false)=="Shield restored: +150"&&InterfaceText.Translate("Shield restored: +150",true)=="Schild aufgeladen: +150"),"English shield pickup");
        Check((InterfaceText.Translate("Repair: +250 HP",false)=="Repair: +250 HP"&&InterfaceText.Translate("Repair: +250 HP",true)=="Reparatur: +250 HP"),"English repair pickup");
        Check((InterfaceText.Translate("HORDE DEFEATED!",false)=="HORDE DEFEATED!"&&InterfaceText.Translate("HORDE DEFEATED!",true)=="HORDE BESIEGT!")&&(InterfaceText.Translate("NEW RECORD!",false)=="NEW RECORD!"&&InterfaceText.Translate("NEW RECORD!",true)=="NEUER REKORD!"),"English result and record");
        Console.WriteLine(checks+" controls checks passed. Controller hardware, IMGUI focus, native capture and flight landing require a game test.");
    }
}
