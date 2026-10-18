using System;
using System.Collections.Generic;
using NeedForValheim;
class PaintTests {
 static void Main(){int bases=15;var seen=new HashSet<int>();
 for(int family=0;family<bases;family++) {
  if(PaintPaletteIndex.Id(family,3,bases)!=family)throw new Exception("Legacy colour ID changed");
  for(int shade=0;shade<7;shade++) {
   int id=PaintPaletteIndex.Id(family,shade,bases);
   if(PaintPaletteIndex.Family(id,bases)!=family||family>0&&PaintPaletteIndex.Shade(id,bases)!=shade)throw new Exception("Shade selection does not round-trip");
   if(family>0&&!seen.Add(id))throw new Exception("Shade ID collision");
  }
 }
 if(seen.Count!=98||PaintPaletteIndex.Count(bases)!=99)throw new Exception("Palette capacity mismatch");
 if(PaintPaletteIndex.Family(-100,bases)!=0||PaintPaletteIndex.Family(10000,bases)!=14)throw new Exception("Invalid saved selection is not bounded");
 Console.WriteLine("Legacy IDs preserved; all 98 body-colour shades round-trip uniquely, Original stays ID 0. No Unity rendering tested.");
 }
}
