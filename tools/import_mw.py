"""Complete local MW import: fleet, parts, vinyls, dependencies, verification, promotion.
Use --verify-only to audit existing staging; --promote also updates the local build inputs.
Original game packages are never included in the mod payload.
"""
import argparse,hashlib,json,re,shutil,subprocess,sys
from pathlib import Path
from project import BASE,LOCAL,mw_root
from mw_source import Source

def digest(path):
 with path.open('rb') as stream:return hashlib.file_digest(stream,'sha256').hexdigest()

def verify(root,source):
 manifest=json.loads((root/'manifest.json').read_text());profiles=json.loads((root/'cars.json').read_text())
 expected={'mw-'+row['car'].lower() for row in source.roster()};actual={p['id'] for p in profiles}
 if manifest['failures'] or expected!=actual or set(manifest['exported'])!=expected:raise ValueError('Incomplete installed MW roster')
 for entry in manifest['dependencies']:
  rel=Path(entry['path'])
  if rel.is_absolute() or '..' in rel.parts:raise ValueError('Unsafe dependency path')
  for path in (source.root/rel,root/'original'/rel):
   if not path.is_file() or path.stat().st_size!=entry['bytes'] or digest(path)!=entry['sha256']:raise ValueError('Changed/missing dependency: '+str(path))
 for profile in profiles:
  ident=profile['id'];provenance=json.loads((root/ident/'provenance.json').read_text());car=ident[3:].upper()
  if provenance['unresolvedTextures']:raise ValueError('Unresolved '+ident)
  if profile['assets'].get('decalFile') and not (root/profile['assets']['decalFile']).is_file():raise ValueError('Missing native decal geometry '+ident)
  if not (root/'decals/mw-logos.json').is_file():raise ValueError('Missing original decal artwork')
  if not (root/'styling'/(ident+'.json')).is_file():raise ValueError('Missing part catalog '+ident)
  vinyl=json.loads((root/'menu'/('vinyl-NFVU2_MW_'+car+'.json')).read_text())
  if vinyl['sourceGame']!='mw' or vinyl['preset']!='NFVU2_MW_'+car:raise ValueError('Wrong vinyl source')
  for folder in profile['assets']['engineStageFolders']:
   if not isinstance(folder,str) or not re.fullmatch(r'audio/mwbank-[a-f0-9]{16}',folder):raise ValueError('Unsafe MW engine folder')
   bank=root/folder;audio=json.loads((bank/'manifest.json').read_text())
   for entry in audio['files']:
    if digest(bank/entry['file'])!=entry['sha256']:raise ValueError('Changed engine loop '+ident)
   for entry in audio['sources']:
    if digest(source.root/entry['file'])!=entry['sha256']:raise ValueError('Changed original bank '+ident)
 return profiles

def promote(root,profiles):
 # Only exporter-owned files are replaced. U2 profiles/assets and installed mods are untouched.
 for profile in profiles:
  ident=profile['id'];target=LOCAL/ident;target.mkdir(parents=True,exist_ok=True)
  for name in (ident+'.json','profile.json','provenance.json'):shutil.copy2(root/ident/name,target/name)
  (LOCAL/'styling').mkdir(exist_ok=True);shutil.copy2(root/'styling'/(ident+'.json'),LOCAL/'styling'/(ident+'.json'))
  preset='NFVU2_MW_'+ident[3:].upper();shutil.copy2(root/'menu'/('vinyl-'+preset+'.json'),LOCAL/'menu'/('vinyl-'+preset+'.json'))
  for folder in profile['assets']['engineStageFolders']:
   bank=Path(folder).name;target=LOCAL/bank;target.mkdir(exist_ok=True)
   for path in (root/folder).glob('*'):
    if path.is_file() and path.suffix in ('.wav','.json'):shutil.copy2(path,target/path.name)
 (LOCAL/'decals').mkdir(exist_ok=True)
 for path in (root/'decals').glob('*.json'):shutil.copy2(path,LOCAL/'decals'/path.name)
 subprocess.run([sys.executable,str(BASE/'tools/build_catalog.py')],check=True)

def main():
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('--source',type=Path);p.add_argument('--resume',action='store_true');p.add_argument('--verify-only',action='store_true');p.add_argument('--promote',action='store_true');a=p.parse_args()
 source=Source(a.source or mw_root());root=LOCAL/'mw-export'
 if not a.verify_only:
  subprocess.run([sys.executable,str(BASE/'tools/export_mw_fleet.py'),'--source',str(source.root),*(['--resume'] if a.resume else [])],check=True)
  subprocess.run([sys.executable,str(BASE/'tools/export_mw_vinyls.py'),'--source',str(source.root)],check=True)
  subprocess.run([sys.executable,str(BASE/'tools/export_mw_decals.py'),'--source',str(source.root)],check=True)
 profiles=verify(root,source)
 subprocess.run([sys.executable,str(BASE/'tools/test.py'),'--skip-build','--suite','mw'],check=True)
 if a.promote:promote(root,profiles)
 print('Verified complete MW fleet:',len(profiles),'cars; promoted='+str(a.promote))
if __name__=='__main__':main()
