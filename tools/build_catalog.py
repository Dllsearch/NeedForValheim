"""Collect every imported car's profile into the cars.json shipped next to the plugin.

Each importer writes `.local/<car>/profile.json` with one catalog entry; this
merges them, newest ids last, and refuses a file that is not a list of entries.
"""
import json
from project import LOCAL


def main():
    entries = []
    for path in sorted(LOCAL.glob('*/profile.json')):
        data = json.loads(path.read_text(encoding='utf-8'))
        if not isinstance(data, list) or not all(isinstance(entry, dict) and entry.get('id') for entry in data):
            raise ValueError(f'{path} must contain a list of catalog entries with ids')
        entries.extend(data)
    if len({entry['id'] for entry in entries}) != len(entries):
        raise ValueError('Two imported cars share a catalog id')
    if len({entry['id'] for entry in entries} | {'350z','h2'}) > 128:
        raise ValueError('Catalog exceeds the runtime limit of 128 cars')
    if any(entry.get('sourceGame','u2') not in ('u2','mw') for entry in entries):
        raise ValueError('Unsupported sourceGame (expected u2 or mw)')
    out = LOCAL / 'cars'
    out.mkdir(parents=True, exist_ok=True)
    (out / 'cars.json').write_text(json.dumps(entries, indent=2), encoding='utf-8')
    print('Catalog:', len(entries), 'imported cars;', ', '.join(entry['id'] for entry in entries))


if __name__ == '__main__':
    main()
