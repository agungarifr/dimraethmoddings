import json, sys

path = sys.argv[1] if len(sys.argv) > 1 else "ars's World.json"
d = json.load(open(path, encoding='utf-8'))
sd = d['worldData']['StorageData']
for i, s in enumerate(sd):
    ent = s.get('Entries'); run = s.get('Runes'); ch = s.get('ChestItems')
    ne = len(ent) if isinstance(ent, list) else None
    nr = len(run) if isinstance(run, list) else None
    nc = len(ch) if isinstance(ch, list) else None
    sz = s.get('StorageSize')
    if (nr or 0) > 0 or (nc or 0) > 0 or (sz or 0) != 18 or (ne or 0) != 18:
        print(i, repr(s.get('StorageName'))[:40], 'size', sz, 'entries', ne, 'runes', nr, 'chestitems', nc)
