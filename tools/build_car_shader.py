"""Build the optional vehicle shader bundle with an explicitly chosen editor."""
import argparse
import hashlib
import json
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--unity', required=True, type=Path, help='Installed Unity.exe')
    parser.add_argument('--render-preview', action='store_true', help='Load the bundle and render all three finishes using the graphics device')
    args = parser.parse_args()
    editor = args.unity.resolve(strict=True)
    project = ROOT / 'tools/car_shader'
    output = ROOT / 'artifacts/car-shader'
    output.mkdir(parents=True, exist_ok=True)
    # Avoid importing default templates, sample assets and unrelated packages.
    packages = project / 'Packages'
    packages.mkdir(exist_ok=True)
    manifest = packages / 'manifest.json'
    package_data = json.loads(manifest.read_text(encoding='utf-8')) if manifest.exists() else {}
    dependencies = package_data.setdefault('dependencies', {})
    for module in ('assetbundle', 'imageconversion', 'physics'):
        dependencies['com.unity.modules.' + module] = '1.0.0'
    manifest.write_text(json.dumps(package_data, indent=2) + '\n', encoding='utf-8')
    settings = project / 'ProjectSettings'
    settings.mkdir(exist_ok=True)
    version = settings / 'ProjectVersion.txt'
    if not version.exists():
        version.write_text('m_EditorVersion: 6000.0.75f1\n', encoding='utf-8')
    log = output / 'build.log'
    # Do not report an old bundle as evidence for a failed current invocation.
    success = output / 'verified-build.json'
    success.unlink(missing_ok=True)
    command = [str(editor), '-batchmode', '-quit',
        '-projectPath', str(project), '-buildTarget', 'Win64',
        '-executeMethod', 'BuildVehicleShader.Build',
        '-nfvu2-output', str(output), '-logFile', str(log)]
    command += ['-force-d3d11', '-nfvu2-preview'] if args.render_preview else ['-nographics']
    result = subprocess.run(command, check=False)
    if result.returncode:
        raise SystemExit(f'Unity failed ({result.returncode}); inspect {log}')
    bundle = output / 'nfvu2-car-shaders'
    if not bundle.is_file() or not bundle.stat().st_size:
        raise SystemExit(f'Unity did not produce the shader bundle; inspect {log}')
    with bundle.open('rb') as stream:
        digest = hashlib.file_digest(stream, 'sha256').hexdigest()
    record = dict(bundle=bundle.name, sha256=digest, bytes=bundle.stat().st_size,
        shader='NFVU2/CarPaint', target='StandaloneWindows64',
        validation=('Editor bundle load and three rendered finishes; game acceptance pending' if args.render_preview else 'Editor bundle build only; game loading and visual acceptance pending'))
    success.write_text(json.dumps(record, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(record, indent=2))


if __name__ == '__main__':
    main()
