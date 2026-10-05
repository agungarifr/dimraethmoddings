import json, sys

path = sys.argv[1] if len(sys.argv) > 1 else "ars's World.json"
d = json.load(open(path, encoding='utf-8'))
sd = d['worldData']['StorageData']

def norm(e):
    return e.get('Kind', 0), e.get('Item', 0), e.get('Amount', 0), e.get('RuneUUID', ''), e.get('PetID', '')

for i, s in enumerate(sd):
    ent = s.get('Entries') or []
    idx = [j for j, e in enumerate(ent) if isinstance(e, dict) and (e.get('Amount', 0) > 0 or e.get('Item', 0) != 0 or e.get('Kind', 0) != 0)]
    if not idx:
        continue
    name = s.get('StorageName')
    sz = s.get('StorageSize')
    print(f"[{i}] {name!r} size={sz} nentries={len(ent)} nonempty={len(idx)}")
    print("    slots:", idx)
    # show a few sample entries
    for j in idx[:4]:
        print(f"     slot {j}: {ent[j]}")
