import json, sys

path = sys.argv[1] if len(sys.argv) > 1 else "ars's World.json"
d = json.load(open(path, encoding='utf-8'))
wd = d['worldData']
print('top keys:', list(d.keys()))
print('worldData keys:', list(wd.keys()))
sd = None
key = None
for k in wd:
    if k.lower() == 'storagedata':
        sd = wd[k]; key = k
print('storageData field:', key, 'count:', len(sd) if sd else None)
if not sd:
    sys.exit()
for i, s in enumerate(sd):
    name = s.get('StorageName') or s.get('Name')
    sz = s.get('StorageSize')
    ent = s.get('Entries')
    n = len(ent) if isinstance(ent, list) else None
    nonempty = 0
    if isinstance(ent, list):
        for e in ent:
            if isinstance(e, dict) and (e.get('Amount', 0) > 0 or e.get('Item', 0) != 0 or e.get('Kind', 0) != 0):
                nonempty += 1
    print(i, repr(name)[:44], 'size=', sz, 'entries=', n, 'nonempty=', nonempty)
    if i == 0:
        print('   sample keys:', list(s.keys()))
