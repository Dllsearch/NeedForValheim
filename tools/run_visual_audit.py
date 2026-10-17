"""Launch opt-in title-screen GUI/PhysX checks, or a manual test-game session.

The automated audit never opens a world. Manual sessions must use a newly created
test character/world; client profile storage can ignore the requested -savedir.
"""
import argparse,json,os,subprocess
from project import LOCAL,OUTPUT,valheim_dir
from install import require_closed

def main():
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--ui-only',action='store_true');parser.add_argument('--keep-open',action='store_true',help='leave the test game open for manual gameplay checks');parser.add_argument('--world-session',action='store_true',help='launch without the title-screen audit or resolution cycling');args=parser.parse_args()
    require_closed();game=valheim_dir();output=OUTPUT/'visual-runtime';output.mkdir(exist_ok=True)
    report=output/'report.json'
    if report.exists():report.replace(output/'previous-report.json')
    saves=LOCAL/'visual-audit-saves';saves.mkdir(exist_ok=True)
    env=dict(os.environ,NFVU2_VISUAL_AUDIT_REPORT=str(report),NFVU2_VISUAL_AUDIT_QUIT='0' if args.keep_open else '1')
    if args.ui_only:env['NFVU2_VISUAL_AUDIT_UI_ONLY']='1'
    if args.world_session:
        for key in ('NFVU2_VISUAL_AUDIT_REPORT','NFVU2_VISUAL_AUDIT_QUIT','NFVU2_VISUAL_AUDIT_UI_ONLY','NFVU2_MW_AUDIT_REPORT'):env.pop(key,None)
    appid=game/'steam_appid.txt'
    if appid.is_file():env['SteamAppId']=appid.read_text().strip()
    command=[str(game/'valheim.exe'),'-console','-screen-fullscreen','0','-screen-width','1280','-screen-height','720','-savedir',str(saves)]
    startup=subprocess.STARTUPINFO();startup.dwFlags|=subprocess.STARTF_USESHOWWINDOW;startup.wShowWindow=1
    process=subprocess.Popen(command,cwd=game,env=env,startupinfo=startup)
    (output/'process.json').write_text(json.dumps(dict(pid=process.pid,executable=command[0],requestedSaves=str(saves),manualWorldSession=args.world_session,report=None if args.world_session else str(report)),indent=2))
    print('Valheim visual audit PID',process.pid,'report',report)

if __name__=='__main__':main()
