"""Explicit allowlist for locally generated mod payloads (never source archives)."""
from pathlib import Path
import shutil
import json

REQUIRED=('NeedForValheim.dll','body.json','cars.json')
# Every imported car ships its model folder plus its own engine loops.
CARS=('skyline','rx7','supra','miata','240sx','eclipse','mustanggt','3000gt','a3','celica',
      'civic','corolla','corsa','escalade','focus','g35','golf','gto','imprezawrx','is300',
      'lancerevo8','navigator','peugot','peugot106','rsx','rx8','sentra','tiburon')
OPTIONAL=(('radio', 'radio', {'.wav','.ogg','.json'}),
          ('radio-mw', 'radio-mw', {'.ogg','.json'}),
          ('styling', 'styling', {'.json'}),
          ('decals', 'decals', {'.json'}),
          ('menu', 'menu', {'.wav','.json'}),
          ('audio-turbo', 'audio/turbo', {'.wav','.json'}),
          ('audio-effects', 'audio/effects', {'.wav','.json'}),
          ('hud', 'hud', {'.json'}),
          ('h2', 'h2', {'.json'}),
          ('interior', 'interior', {'.json'}),
          ('audio-h2', 'audio/h2', {'.wav','.json'}),
          ('turret', 'turret', {'.json','.wav'}))
OPTIONAL+=tuple((car, car, {'.json'}) for car in CARS)
OPTIONAL+=tuple((f'audio-{car}', f'audio/{car}', {'.wav','.json'}) for car in CARS)

def optional_assets(catalog):
    """MW imports add only their generated visual model, never source binaries."""
    result=list(OPTIONAL)
    catalog=Path(catalog)
    if catalog.is_file():
        entries=json.loads(catalog.read_text(encoding='utf-8-sig'))
        if not isinstance(entries,list) or any(not isinstance(e,dict) for e in entries):
            raise ValueError('cars.json must contain a list of vehicle profiles')
        for entry in entries:
            if entry.get('sourceGame')!='mw':continue
            ident=entry.get('id','')
            if not ident.startswith('mw-') or len(ident)>32 or any(c not in 'abcdefghijklmnopqrstuvwxyz0123456789-_' for c in ident):
                raise ValueError('Unsafe MW catalog id: '+ident)
            if entry.get('assets',{}).get('modelFile')!=f'{ident}/{ident}.json':
                raise ValueError('MW model must use its own catalog-id folder: '+ident)
            result.append((ident,ident,{'.json'}))
            stages=entry.get('assets',{}).get('engineStageFolders',[])
            for folder in stages:
                bank=folder.removeprefix('audio/') if isinstance(folder,str) else ''
                if not bank.startswith('mwbank-') or len(bank)!=23 or any(c not in '0123456789abcdef' for c in bank[7:]):raise ValueError('Unsafe MW engine folder')
                asset=(bank,folder,{'.wav','.json'})
                if asset not in result:result.append(asset)
    return result

def asset_files(folder,extensions):
    folder=Path(folder)
    if folder.name.startswith('mw-'):
        model=folder/(folder.name+'.json')
        if not model.is_file() or model.is_symlink():raise FileNotFoundError('Missing generated MW model: '+str(model))
        return [model]
    manifest=folder/'tracks.json'
    if folder.name in ('radio','radio-ogg','radio-mw') and manifest.is_file():
        entries=json.loads(manifest.read_text(encoding='utf-8-sig'))
        if not isinstance(entries,list):raise ValueError('Radio manifest must be a track list')
        selected=[manifest];seen=set()
        for entry in entries:
            name=entry.get('file') if isinstance(entry,dict) else None
            if not isinstance(name,str) or not name or Path(name).name!=name or '/' in name or '\\' in name or ':' in name or Path(name).suffix.lower() not in ('.wav','.ogg'):
                raise ValueError('Invalid radio filename in manifest')
            path=folder/name
            if name.lower() in seen or path.is_symlink() or not path.is_file():
                raise ValueError('Duplicate or missing radio file: '+name)
            selected.append(path);seen.add(name.lower())
        return selected
    return sorted(p for p in folder.glob('*') if p.is_file() and not p.is_symlink() and p.suffix.lower() in extensions)

def payload_files(root):
    root=Path(root)
    for name in REQUIRED:
        path=root/name
        if not path.is_file():raise FileNotFoundError('Build the mod first: '+name)
        yield Path(name)
    for _,folder,extensions in optional_assets(root/'cars.json'):
        for path in asset_files(root/folder,extensions):
            yield path.relative_to(root)

def sync_optional_assets(local,output):
    output=Path(output).resolve()
    for source_name,folder,extensions in optional_assets(Path(local)/'cars/cars.json'):
        source=Path(local)/source_name
        if source_name=='radio':
            from prepare_radio import compressed_source
            source=compressed_source(local)
        target=(output/folder).resolve()
        if not target.is_relative_to(output):raise ValueError('Asset target escaped build directory')
        # Remove only this generator's allowlisted stale files in its fixed folder.
        target.mkdir(parents=True,exist_ok=True)
        wanted={p.name:p for p in asset_files(source,extensions)}
        for path in target.iterdir():
            if path.is_file() and path.suffix.lower() in extensions and path.name not in wanted:path.unlink()
        for name,path in wanted.items():shutil.copy2(path,target/name)
