import json
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'tools'))
import install
from mod_files import sync_optional_assets

class InstallTests(unittest.TestCase):
    def test_process_guard_refuses_running_game_and_fails_closed_on_query_error(self):
        with patch.object(install,'process_names',return_value=['other.exe','Valheim.exe']):
            with self.assertRaises(SystemExit):install.require_closed()
        with patch.object(install,'process_names',return_value=['other.exe']):install.require_closed()
        with patch.object(install,'process_names',side_effect=PermissionError('unavailable')):
            with self.assertRaises(PermissionError):install.require_closed()

    def test_generated_mw_manifest_paths_are_valid_without_allowing_source_files(self):
        for name in ('mw-a3/mw-a3.json','audio/mwbank-0123456789abcdef/engine_1200.wav','audio/mwbank-0123456789abcdef/coast_1200.wav','audio/mwbank-0123456789abcdef/manifest.json'):
            self.assertTrue(install.allowed_name(name),name)
        for name in ('mw-a3/TEXTURES.BIN','mw-a3/profile.json','audio/mwbank-0123456789abcdef/private.txt','../mw-a3/mw-a3.json'):
            self.assertFalse(install.allowed_name(name),name)

    def test_successful_install_retains_two_newest_backups_and_named_backup(self):
        with tempfile.TemporaryDirectory() as temp:
            root=Path(temp);source,dest,_=self.fixture(root)
            backups=root/'backups';backups.mkdir()
            for name in ['20261007-000000-000001','20261008-000000-000001','flatgrass-20261007']:
                (backups/name).mkdir();(backups/name/'snapshot').write_bytes(b'old')
            newest=backups/'20261009-000000-000001'
            install.install_payload(source,dest,newest,lambda:None)
            self.assertEqual({p.name for p in backups.iterdir()},
                             {'20261008-000000-000001',newest.name,'flatgrass-20261007'})
            self.assertEqual((newest/'NeedForValheim.dll').read_bytes(),b'old')

    def test_failed_install_does_not_prune_backups(self):
        with tempfile.TemporaryDirectory() as temp:
            root=Path(temp);source,dest,_=self.fixture(root);backups=root/'backups';backups.mkdir()
            old=['20261007-000000-000001','20261008-000000-000001']
            for name in old:(backups/name).mkdir()
            original=install.checksum
            def corrupt(path):return 'bad' if Path(path)==dest/'radio/song.wav' else original(path)
            with patch.object(install,'checksum',corrupt):
                with self.assertRaises(RuntimeError):
                    install.install_payload(source,dest,backups/'20261009-000000-000001',lambda:None)
            for name in old:self.assertTrue((backups/name).exists())

    def test_backup_cleanup_refuses_linked_contents(self):
        with tempfile.TemporaryDirectory() as temp:
            root=Path(temp);backups=root/'backups';backups.mkdir()
            for day in [7,8,9]:(backups/f'2026100{day}-000000-000001').mkdir()
            nested=backups/'20261007-000000-000001/nested';nested.mkdir()
            (nested/'keep').write_bytes(b'keep')
            with patch.object(install.os.path,'isjunction',lambda p:Path(p)==nested,create=True):
                with self.assertRaises(ValueError):install.prune_backups(backups)
            self.assertEqual((nested/'keep').read_bytes(),b'keep')

    def fixture(self,root):
        source=root/'build';dest=root/'installed'
        source.mkdir();dest.mkdir()
        for name in install.REQUIRED:
            (source/name).write_bytes(b'[]' if name=='cars.json' else b'new');(dest/name).write_bytes(b'old')
        (source/'radio').mkdir();(source/'radio/song.wav').write_bytes(b'song')
        return source,dest,root/'backup'
    def test_payload_checksums_backup_and_stale_owned_only(self):
        with tempfile.TemporaryDirectory() as temp:
            source,dest,backup=self.fixture(Path(temp))
            (dest/'radio').mkdir()
            (dest/'radio/old.wav').write_bytes(b'old song')
            (dest/'radio/personal.ogg').write_bytes(b'keep')
            (dest/install.MANIFEST).write_text(json.dumps(['radio/old.wav']))
            self.assertEqual(install.install_payload(source,dest,backup,lambda:None),len(install.REQUIRED)+1)
            self.assertEqual((backup/'radio/old.wav').read_bytes(),b'old song')
            self.assertFalse((dest/'radio/old.wav').exists())
            self.assertEqual((dest/'radio/personal.ogg').read_bytes(),b'keep')
            for name in install.payload_files(source):
                self.assertEqual(install.checksum(source/name),install.checksum(dest/name))
    def test_copy_failure_restores_old_and_removes_partial_new(self):
        with tempfile.TemporaryDirectory() as temp:
            source,dest,backup=self.fixture(Path(temp))
            original=install.checksum
            def corrupt(path):
                if Path(path)==dest/'radio/song.wav':return 'bad checksum'
                return original(path)
            with patch.object(install,'checksum',corrupt):
                with self.assertRaises(RuntimeError):install.install_payload(source,dest,backup,lambda:None)
            self.assertEqual((dest/'NeedForValheim.dll').read_bytes(),b'old')
            self.assertFalse((dest/'radio/song.wav').exists())
    def test_effects_are_synced_installed_and_backed_up(self):
        with tempfile.TemporaryDirectory() as temp:
            root=Path(temp);source,dest,backup=self.fixture(root)
            local=root/'local';effects=local/'audio-effects';effects.mkdir(parents=True)
            (effects/'nitro.wav').write_bytes(b'nitro');(effects/'purge.wav').write_bytes(b'purge')
            (effects/'source.abk').write_bytes(b'original bank stays local')
            (dest/'audio/effects').mkdir(parents=True)
            (dest/'audio/effects/nitro.wav').write_bytes(b'old effect')
            sync_optional_assets(local,source)
            install.install_payload(source,dest,backup,lambda:None)
            self.assertEqual((backup/'audio/effects/nitro.wav').read_bytes(),b'old effect')
            self.assertEqual((dest/'audio/effects/nitro.wav').read_bytes(),b'nitro')
            self.assertEqual((dest/'audio/effects/purge.wav').read_bytes(),b'purge')
            self.assertFalse((dest/'audio/effects/source.abk').exists())
    def test_reject_manifest_escape_before_mutation(self):
        with tempfile.TemporaryDirectory() as temp:
            source,dest,backup=self.fixture(Path(temp))
            (dest/install.MANIFEST).write_text(json.dumps(['../outside.wav']))
            with self.assertRaises(ValueError):install.install_payload(source,dest,backup,lambda:None)
            self.assertFalse(backup.exists())
    def test_radio_payload_follows_playlist_and_retains_original_streams(self):
        with tempfile.TemporaryDirectory() as temp:
            root=Path(temp);local=root/'local';output=root/'build'
            radio=local/'radio';radio.mkdir(parents=True)
            (radio/'song.wav').write_bytes(b'music')
            (radio/'ambient.wav').write_bytes(b'not a song')
            (radio/'tracks.json').write_text(json.dumps([{'file':'song.wav','title':'Artist - Song'}]))
            (output/'radio').mkdir(parents=True);(output/'radio/ambient.wav').write_bytes(b'stale')
            sync_optional_assets(local,output)
            self.assertTrue((radio/'ambient.wav').exists())
            self.assertFalse((output/'radio/ambient.wav').exists())
            self.assertEqual((output/'radio/song.wav').read_bytes(),b'music')
    def test_radio_missing_manifest_song_fails_before_install(self):
        with tempfile.TemporaryDirectory() as temp:
            source,dest,backup=self.fixture(Path(temp))
            (source/'radio/tracks.json').write_text(json.dumps([{'file':'missing.wav'}]))
            with self.assertRaises(ValueError):install.install_payload(source,dest,backup,lambda:None)
            self.assertFalse(backup.exists())

if __name__=='__main__':unittest.main()
