"""Original stock engine GIN, decoded locally; phase-aligned loops for Unity playback."""
import argparse
import hashlib
import json
import struct
import subprocess
import wave
from project import LOCAL, vgmstream_path

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--car', default='h2', help='car id; selects .local/audio-<car>')
    parser.add_argument('--gin', default='GIN_Hummer.gin', help='engine bank under SOUND/ENGINE')
    parser.add_argument('--bands', default='1000,1500,2000,2500,3000,3600,4200,4782',
                        help='rpm loop points; defaults to the H2 mapping from EngineData 38.ini')
    parser.add_argument('--engine', type=int, default=38,
                        help='EngineData index the bank is cited from; CarDataMapping names it per car')
    args = parser.parse_args()
    import numpy as np

    car = args.car.lower()
    bands = [int(value) for value in args.bands.split(',')]

    source = LOCAL / f'nfs-assets/SOUND/ENGINE/{args.gin}'
    decoded = LOCAL / f'audio-decoded/{car}.wav'
    decoded.parent.mkdir(parents=True, exist_ok=True)
    subprocess.run([str(vgmstream_path()), '-i', '-o', str(decoded), str(source)], check=True, stdout=subprocess.DEVNULL)
    data = source.read_bytes()
    rpm_range = struct.unpack_from('<ff', data, 8)
    na, nb = struct.unpack_from('<II', data, 16)
    a = struct.unpack_from('<' + 'I' * (na + 1), data, 32)
    cuts = np.array(struct.unpack_from('<' + 'I' * (nb + 1), data, 32 + (na + 1) * 4))
    with wave.open(str(decoded), 'rb') as w:
        if w.getnchannels() != 1 or w.getsampwidth() != 2:
            raise ValueError('Expected mono PCM16')
        rate = w.getframerate()
        samples = np.frombuffer(w.readframes(w.getnframes()), dtype='<i2').astype(float) / 32768
    out = LOCAL / f'audio-{car}'
    out.mkdir(parents=True, exist_ok=True)
    files = []
    for rpm in bands:
        center = np.interp(rpm, np.linspace(*rpm_range, len(a)), a)
        idx = int(np.argmin(abs(cuts - center)))
        start = max(0, min(idx - 6, len(cuts) - 13))
        length = round(120 * rate / rpm)
        cycles = []
        for k in range(start, start + 12):
            chunk = samples[cuts[k]:cuts[k + 1]]
            cycles.append(np.interp(np.arange(length) * len(chunk) / length, np.arange(len(chunk) + 1),
                                    np.append(chunk, samples[cuts[k + 1]])))
        loop = np.concatenate(cycles)
        t = np.linspace(0, 1, length)
        loop = np.concatenate((loop[length:-length], loop[-length:] * (1 - t) + loop[:length] * t))
        loop -= loop.mean()
        rms = float(np.sqrt(np.mean(loop * loop)))
        peak = float(np.max(abs(loop)))
        if rms < .0001:
            raise ValueError(f'Silent {car} sample')
        loop *= min(.18 / rms, .82 / peak)
        name = f'engine_{rpm}.wav'
        with wave.open(str(out / name), 'wb') as w:
            w.setparams((1, 2, rate, len(loop), 'NONE', 'not compressed'))
            w.writeframes(np.rint(loop * 32767).astype('<i2').tobytes())
        files.append(dict(file=name, rpm=rpm, samples=len(loop), sampleRate=rate,
                          sha256=hashlib.sha256((out / name).read_bytes()).hexdigest()))
    manifest = dict(source=f'SOUND/ENGINE/{args.gin}', sourceSha256=hashlib.sha256(data).hexdigest(),
                    rpmRange=rpm_range, files=files,
                    reference=f'https://github.com/nlgxzef/NFSU2CarSoundTuner/blob/master/'
                              f'CarSoundData/EngineData/{args.engine}.ini',
                    provenance=f'Stock {args.gin} bank decoded with vgmstream; loop points {bands} span the '
                               f'bank\'s own {rpm_range[0]:.0f}-{rpm_range[1]:.0f} RPM range. Unity uses this '
                               'original bank for load and quieter coast; no separate deceleration recording '
                               'is claimed. Gear timings are adapted for Valheim.')
    (out / 'manifest.json').write_text(json.dumps(manifest, indent=2), encoding='utf-8')
    print('Prepared', len(files), f'original {car} engine loops; source RPM range:', rpm_range)


if __name__ == '__main__':
    main()
