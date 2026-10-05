import json, glob, os

for path in sorted(glob.glob("*.json")):
    try:
        d = json.load(open(path, encoding='utf-8'))
    except Exception as e:
        print(path, "load error", e); continue
    wd = d.get('worldData') if isinstance(d, dict) else None
    if not isinstance(wd, dict):
        continue
    sd = wd.get('StorageData') or wd.get('storageData')
    if not sd:
        continue
    anomalies = []
    for i, s in enumerate(sd):
        if not isinstance(s, dict):
            continue
        sz = s.get('StorageSize')
        ent = s.get('Entries')
        n = len(ent) if isinstance(ent, list) else None
        nonempty = 0
        beyond = 0
        if isinstance(ent, list):
            for j, e in enumerate(ent):
                if isinstance(e, dict) and (e.get('Amount', 0) > 0 or e.get('Item', 0) != 0 or e.get('Kind', 0) != 0):
                    nonempty += 1
                    if j >= 18:
                        beyond += 1
        if n is not None and sz is not None and n != sz:
            anomalies.append(f"  [{i}] {s.get('StorageName')!r} size={sz} nentries={n} nonempty={nonempty} beyond18={beyond}")
        elif beyond and not anomalies:
            pass
    if anomalies:
        print(f"=== {path}: {len(anomalies)} size!=count ===")
        for a in anomalies[:12]:
            print(a)
