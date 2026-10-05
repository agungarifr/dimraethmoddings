import json, sys

path = sys.argv[1] if len(sys.argv) > 1 else "ars's World.json"
d = json.load(open(path, encoding='utf-8'))
sd = d['worldData']['StorageData']

def L(v):
    return len(v) if isinstance(v, list) else v

print(f"{'idx':>3} {'name':<26} {'size':>5} {'entries':>7} {'runes':>6} {'opened':>7} {'chestitems':>10}")
for i, s in enumerate(sd):
    ent = s.get('Entries'); run = s.get('Runes'); op = s.get('PlayersOpened'); ch = s.get('ChestItems')
    sz = s.get('StorageSize')
    if (sz or 0) != 12 and (sz or 0) != 18:
        print(f"{i:>3} {repr(s.get('StorageName'))[:26]:<26} {sz!s:>5} {L(ent)!s:>7} {L(run)!s:>6} {L(op)!s:>7} {L(ch)!s:>10}")
