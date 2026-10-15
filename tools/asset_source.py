"""Read NFS Underground 2 data from CD images or from an installed game folder.

`disc_root` may point at a folder containing `CD*/…bin` MODE1/2352 images, in
which case the original `COMPRESS.ZIP` and soundtrack archive are read from the
images, or at an installed game folder, which already holds those same files
unpacked. Both shapes are presented to the callers as one archive interface.
"""
import contextlib
import io
import zipfile
from pathlib import Path

from project import disc_root
from inspect_discs import Disc


class DiscFile(io.RawIOBase):
    def __init__(self, disc, rec):
        self.disc, self.rec, self.pos = disc, rec, 0

    def seekable(self): return True
    def readable(self): return True
    def tell(self): return self.pos

    def seek(self, offset, whence=0):
        self.pos = offset if whence == 0 else self.pos + offset if whence == 1 else self.rec['size'] + offset
        if self.pos < 0: raise ValueError('negative seek')
        return self.pos

    def read(self, n=-1):
        n = self.rec['size'] - self.pos if n < 0 else min(n, self.rec['size'] - self.pos)
        if n <= 0: return b''
        data = self.disc.read(self.rec['lba'] * 2048 + self.pos, n)
        self.pos += len(data)
        return data


class TreeEntry:
    """Zip-entry shaped view of one file in an installed game folder."""
    def __init__(self, root, path):
        self.path, self.filename = path, path.relative_to(root).as_posix()

    @property
    def file_size(self): return self.path.stat().st_size
    def is_dir(self): return False


class TreeArchive:
    """The subset of `zipfile.ZipFile` the extraction tools use, over a folder."""
    def __init__(self, root): self.root = root

    def infolist(self):
        return [TreeEntry(self.root, p) for p in sorted(self.root.rglob('*')) if p.is_file()]
    def namelist(self): return [e.filename for e in self.infolist()]
    def read(self, item): return item.path.read_bytes()


def installed():
    root = disc_root()
    return (root/'CARS').is_dir() and (root/'SOUND').is_dir()


def archives():
    """Yield `(label, archive)` pairs; each disc stays open until its archives are consumed."""
    root = disc_root()
    images = sorted(root.glob('CD*/*.bin'))
    if not images:
        if not installed():
            raise SystemExit('No CD images and no installed NFSU2 data in ' + str(root))
        yield (str(root), TreeArchive(root))
        return
    for image in images:
        disc = Disc(image)
        try:
            for rec in disc.walk():
                if rec['name'].upper() != 'COMPRESS.ZIP': continue
                yield (disc.label, zipfile.ZipFile(DiscFile(disc, rec)))
        finally:
            disc.f.close()


@contextlib.contextmanager
def source_file(name):
    """Yield a readable file for one source path, or None when no source has it."""
    key = name.upper()
    root = disc_root()
    for image in sorted(root.glob('CD*/*.bin')):
        disc = Disc(image)
        try:
            for rec in disc.walk():
                if rec['path'].upper() == key:
                    yield DiscFile(disc, rec)
                    return
        finally:
            disc.f.close()
    if installed():
        target = root/key
        if target.is_file():
            with target.open('rb') as handle:
                yield handle
                return
    yield None


def extract_matching(dest, wanted):
    """Copy each wanted source file into `dest` under its relative path.

    `wanted` receives the upper-cased relative path. Returns the set of copied
    upper-cased paths, so a caller can report what no source provided.
    """
    dest = Path(dest)
    copied = set()
    for _, archive in archives():
        for item in archive.infolist():
            if item.is_dir(): continue
            name = item.filename.replace('\\', '/')
            upper = name.upper()
            if upper in copied or not wanted(upper): continue
            target = (dest/name).resolve()
            if not target.is_relative_to(dest.resolve()): raise ValueError('Unsafe source path: ' + name)
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(archive.read(item))
            copied.add(upper)
    return copied
