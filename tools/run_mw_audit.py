"""Launch an isolated Valheim MW audit with separate save data; no existing world is opened."""
import json,os,subprocess
from pathlib import Path
from project import LOCAL,OUTPUT,valheim_dir
from install import require_closed

def main():
 require_closed();game=valheim_dir();output=OUTPUT/'mw-runtime';output.mkdir(exist_ok=True)
 report=output/'report.json'
 if report.exists():report.replace(output/'previous-report.json')
 saves=LOCAL/'mw-audit-saves';saves.mkdir(exist_ok=True)
 env=dict(os.environ);env['NFVU2_MW_AUDIT_REPORT']=str(report)
 appid=game/'steam_appid.txt'
 if appid.is_file():env['SteamAppId']=appid.read_text().strip()
 command=[str(game/'valheim.exe'),'-console','-screen-fullscreen','0','-screen-width','1280','-screen-height','720','-savedir',str(saves)]
 startup=subprocess.STARTUPINFO();startup.dwFlags|=subprocess.STARTF_USESHOWWINDOW;startup.wShowWindow=1
 process=subprocess.Popen(command,cwd=game,env=env,startupinfo=startup)
 (output/'process.json').write_text(json.dumps(dict(pid=process.pid,executable=command[0],saves=str(saves),report=str(report)),indent=2))
 print('Valheim audit PID',process.pid,'report',report)
if __name__=='__main__':main()
