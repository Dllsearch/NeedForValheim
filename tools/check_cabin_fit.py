"""Focused geometry/transition/driver-controls checks, without loading Unity."""
import shutil
import subprocess
from project import BASE, OUTPUT, valheim_dir
from build_support import compile_cs, test_refs, runtime_config


def main():
    out = OUTPUT / 'tests/cabinfit'
    out.mkdir(parents=True, exist_ok=True)
    refs, tfm, version = test_refs()
    jsondll = out / 'Newtonsoft.Json.dll'
    shutil.copy2(valheim_dir() / 'valheim_Data/Managed/Newtonsoft.Json.dll', jsondll)
    sources = [BASE / 'tests/cabinfit/Program.cs']
    sources += [BASE / 'src' / name for name in (
        'CarProfile.cs', 'ModelFile.cs', 'TurretState.cs', 'ShowroomState.cs',
        'CabinTransition.cs', 'CabinAppearance.cs', 'CabinFit.cs', 'CabinControlPose.cs')]
    dll = out / 'CabinFitTests.dll'
    compile_cs(dll, sources, refs + [jsondll])
    runtime_config(dll, tfm, version)
    subprocess.run(['dotnet', str(dll), str(OUTPUT / 'mod'), str(out / 'fits.csv')], check=True)


if __name__ == '__main__':
    main()
