"""Install the local payload while Valheim is closed; back up and verify every file."""
import csv
import hashlib
import io
import json
import os
import re
import shutil
import subprocess
from datetime import datetime
from project import OUTPUT,valheim_dir
from pathlib import Path
from mod_files import payload_files, OPTIONAL, REQUIRED


def process_names():
    if os.name!='nt':
        result=subprocess.check_output(['tasklist','/FO','CSV','/NH'],text=True)
        return [row[0] for row in csv.reader(io.StringIO(result)) if row]
    # A process snapshot keeps the per-file guard cheap for thousands of bank
    # files. No process handles, memory access, signals or termination are used.
    import ctypes
    from ctypes import wintypes
    class Entry(ctypes.Structure):
        _fields_=[('dwSize',wintypes.DWORD),('cntUsage',wintypes.DWORD),('th32ProcessID',wintypes.DWORD),
                  ('th32DefaultHeapID',ctypes.c_size_t),('th32ModuleID',wintypes.DWORD),('cntThreads',wintypes.DWORD),
                  ('th32ParentProcessID',wintypes.DWORD),('pcPriClassBase',wintypes.LONG),('dwFlags',wintypes.DWORD),('szExeFile',wintypes.WCHAR*260)]
    api=ctypes.WinDLL('kernel32',use_last_error=True)
    api.CreateToolhelp32Snapshot.argtypes=[wintypes.DWORD,wintypes.DWORD];api.CreateToolhelp32Snapshot.restype=wintypes.HANDLE
    api.Process32FirstW.argtypes=[wintypes.HANDLE,ctypes.POINTER(Entry)];api.Process32NextW.argtypes=api.Process32FirstW.argtypes
    api.CloseHandle.argtypes=[wintypes.HANDLE]
    snapshot=api.CreateToolhelp32Snapshot(2,0)
    if snapshot==ctypes.c_void_p(-1).value:raise ctypes.WinError(ctypes.get_last_error())
    try:
        entry=Entry();entry.dwSize=ctypes.sizeof(Entry);names=[]
        more=api.Process32FirstW(snapshot,ctypes.byref(entry))
        if not more and ctypes.get_last_error()!=18:raise ctypes.WinError(ctypes.get_last_error())
        while more:
            names.append(entry.szExeFile);more=api.Process32NextW(snapshot,ctypes.byref(entry))
        if ctypes.get_last_error()!=18:raise ctypes.WinError(ctypes.get_last_error())
        return names
    finally:api.CloseHandle(snapshot)


def require_closed():
    if any(name.lower()=='valheim.exe' for name in process_names()):
        raise SystemExit('Valheim is running. Close it before installing the mod.')


def checksum(path):return hashlib.sha256(path.read_bytes()).hexdigest()


MANIFEST='nfvu2-installed-files.json'
# A manifest written before the SUV rename still names its old asset folders;
# those entries are exactly what the install has to back up and delete.
LEGACY_ASSET_NAMES={'hummer':{'.json'},'audio/hummer':{'.wav','.json'}}

def allowed_name(value):
    path=Path(value)
    if path.is_absolute() or '..' in path.parts:return False
    if path.as_posix() in REQUIRED:return True
    parts=path.parts
    if len(parts)==2 and re.fullmatch(r'mw-[a-z0-9_-]{1,29}',parts[0]) and parts[1]==parts[0]+'.json':return True
    if len(parts)==3 and parts[0]=='audio' and re.fullmatch(r'mwbank-[a-f0-9]{16}',parts[1]) and (parts[2]=='manifest.json' or re.fullmatch(r'(?:engine|coast)_[0-9]{1,5}\.wav',parts[2])):return True
    if LEGACY_ASSET_NAMES.get(path.parent.as_posix(),set())>= {path.suffix.lower()}:return True
    return any(path.parent.as_posix()==folder and path.suffix.lower() in extensions for _,folder,extensions in OPTIONAL)

def contained(root,name):
    target=(root/name).resolve()
    if not target.is_relative_to(root.resolve()):raise ValueError('Install path escapes its directory')
    return target


def prune_backups(folder):
    """Retain two dated snapshots; leave separately named backups untouched."""
    def linked(path):
        return path.is_symlink() or getattr(os.path,'isjunction',lambda p:False)(path)
    folder=Path(folder)
    if linked(folder):raise ValueError('Backup directory is a link')
    root=folder.resolve()
    candidates=sorted((p for p in folder.iterdir()
                       if re.fullmatch(r'\d{8}-\d{6}-\d{6}',p.name)
                       and not linked(p) and p.is_dir()),key=lambda p:p.name,reverse=True)
    stale=candidates[2:]
    # Validate all deletion targets before removing anything. Do not traverse
    # links even when they point to another directory inside the backup root.
    for backup in stale:
        if not backup.resolve().is_relative_to(root) or backup.resolve()==root:
            raise ValueError('Backup cleanup path escapes its directory')
        for current,dirs,files in os.walk(backup,followlinks=False):
            for name in dirs+files:
                path=Path(current)/name
                if linked(path) or not path.resolve().is_relative_to(backup.resolve()):
                    raise ValueError('Backup cleanup refuses linked contents')
    for backup in stale:shutil.rmtree(backup)
    return len(stale)

def install_payload(source,destination,backup,guard=require_closed):
    guard()
    source,destination,backup=Path(source),Path(destination),Path(backup)
    names=list(payload_files(source))
    old=[]
    manifest=contained(destination,MANIFEST)
    if manifest.exists():
        saved=json.loads(manifest.read_text(encoding='utf-8'))
        if not isinstance(saved,list) or not all(isinstance(n,str) and allowed_name(n) for n in saved):
            raise ValueError('Invalid installed file manifest')
        old=[Path(n) for n in saved]
    touched=set(names)|set(old)|{Path(MANIFEST)}
    for name in touched:
        contained(destination,name);contained(backup,name)
    backup.mkdir(parents=True,exist_ok=False)
    destination.mkdir(parents=True,exist_ok=True)
    for name in touched:
        installed=destination/name
        if installed.exists():
            saved=backup/name;saved.parent.mkdir(parents=True,exist_ok=True)
            shutil.copy2(installed,saved)
    try:
        for name in names:
            guard()
            original=source/name;installed=destination/name
            installed.parent.mkdir(parents=True,exist_ok=True)
            shutil.copy2(original,installed)
            if checksum(original)!=checksum(installed):raise RuntimeError('Checksum mismatch: '+str(name))
        for name in set(old)-set(names):
            guard()
            path=destination/name
            if path.exists():path.unlink()
        guard()
        manifest.write_text(json.dumps([p.as_posix() for p in names],indent=2),encoding='utf-8')
    except Exception:
        guard()
        for name in touched:
            installed=destination/name
            if (backup/name).exists():shutil.copy2(backup/name,installed)
            elif installed.exists():installed.unlink()
        raise
    if re.fullmatch(r'\d{8}-\d{6}-\d{6}',backup.name):prune_backups(backup.parent)
    return len(names)

LEGACY_PLUGIN='Rachel350Z'
LEGACY_CONFIG='local.rachel350z.prototype.cfg'
CURRENT_CONFIG='local.needforvalheim.prototype.cfg'
# The built-in SUV is addressed by its own id now, so its saved sections move too.
LEGACY_SECTION='[Hummer.'
CURRENT_SECTION='[h2.'
LEGACY_ASSET_FOLDERS=('hummer','audio/hummer')

def migrate_sections(config,backup):
    """Rename the SUV's saved sections in place; a section that already exists wins."""
    path=config/CURRENT_CONFIG
    if not path.is_file() or LEGACY_SECTION not in path.read_text(encoding='utf-8'):return 0
    text=path.read_text(encoding='utf-8')
    wanted=re.findall(re.escape(LEGACY_SECTION)+r'([^\]]+)\]',text)
    moved=0
    for name in wanted:
        if CURRENT_SECTION+name+']' in text:continue
        text=text.replace(LEGACY_SECTION+name+']',CURRENT_SECTION+name+']',1)
        moved+=1
    if not moved:return 0
    shutil.copy2(path,backup/'config-before-sections.cfg')
    require_closed()
    path.write_text(text,encoding='utf-8')
    return moved

def prune_legacy_assets(destination):
    """install_payload unlinks the tracked files; the emptied legacy folders stay."""
    removed=0
    for name in LEGACY_ASSET_FOLDERS:
        folder=destination/name
        if folder.is_dir() and not any(p for p in folder.rglob('*') if p.is_file()):
            require_closed()
            shutil.rmtree(folder)
            removed+=1
    return removed

def migrate_legacy(valheim,backup):
    """Carry saved settings to the renamed plugin id and drop the old plugin folder.

    BepInEx loads every dll under plugins, so a leftover Rachel350Z folder would
    run the old plugin next to the new one. The old cfg file is left in place.
    """
    valheim=Path(valheim)
    moved=0
    legacy=valheim/'BepInEx/plugins'/LEGACY_PLUGIN
    if legacy.is_dir():
        saved=backup/'legacy-plugins'/LEGACY_PLUGIN
        saved.parent.mkdir(parents=True,exist_ok=True)
        shutil.copytree(legacy,saved)
        require_closed()
        shutil.rmtree(legacy)
        moved=1
    copied=0
    config=valheim/'BepInEx/config'
    old=config/LEGACY_CONFIG
    if old.is_file():
        saved=backup/'legacy-config'
        saved.mkdir(parents=True,exist_ok=True)
        shutil.copy2(old,saved/LEGACY_CONFIG)
        if not (config/CURRENT_CONFIG).exists():
            shutil.copy2(old,config/CURRENT_CONFIG)
            copied=1
    return moved,copied

def main():
    backup=OUTPUT/'backups'/datetime.now().strftime('%Y%m%d-%H%M%S-%f')
    game=valheim_dir()
    destination=game/'BepInEx/plugins/NeedForValheim'
    count=install_payload(OUTPUT/'mod',destination,backup)
    moved,copied=migrate_legacy(game,backup)
    sections=migrate_sections(game/'BepInEx/config',backup)
    folders=prune_legacy_assets(destination)
    print('Installed and SHA-256 verified',count,'files. Backup:',backup)
    print('Legacy plugin folder removed:',moved,'| settings carried to',CURRENT_CONFIG+':',copied)
    print('SUV config sections renamed to',CURRENT_SECTION+':',sections,'| empty legacy asset folders removed:',folders)


if __name__=='__main__':main()
