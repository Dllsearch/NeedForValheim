"""Independent vinyl validation; never writes another agent's artifacts/mod."""
import subprocess,shutil
from pathlib import Path
from project import BASE,LOCAL,valheim_dir
from build_support import compile_cs,test_refs,runtime_config
def main():
    refs,tfm,version=test_refs();out=BASE/'artifacts/vinyl-checks';out.mkdir(exist_ok=True)
    jsonlib=valheim_dir()/'valheim_Data/Managed/Newtonsoft.Json.dll';shutil.copy2(jsonlib,out/jsonlib.name)
    exe=out/'vinyl-tests.dll';compile_cs(exe,[BASE/'src/VehicleVinylData.cs',BASE/'tests/vinyl/Program.cs'],refs+[jsonlib]);runtime_config(exe,tfm,version)
    subprocess.run(['dotnet',str(exe),str(LOCAL/'menu'),str(out/'miata-paint.rgba')],check=True)
if __name__=='__main__':main()
