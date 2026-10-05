import json, sys

path = sys.argv[1] if len(sys.argv) > 1 else "ars.json"
d = json.load(open(path, encoding='utf-8'))
def walk(o, keys=('Inventory','inventory','Entries','entries','PetID','petID'), depth=0, pathn=''):
    if depth > 6: return
    if isinstance(o, dict):
        for k, v in o.items():
            if k in ('Inventory','inventory','PetInventory','petInventory','Entries','entries') and isinstance(v, list):
                nonempty = [e for e in v if isinstance(e, dict) and (e.get('Amount',0)>0 or e.get('Item',0)!=0 or e.get('Kind',0)!=0 or e.get('PetID'))]
                pets = [e for e in nonempty if isinstance(e,dict) and e.get('PetID')]
                print(f"{pathn}.{k}: total={len(v)} nonempty={len(nonempty)} withPetID={len(pets)}")
                for e in pets[:10]:
                    print('   pet entry:', e)
            walk(v, keys, depth+1, pathn+'.'+str(k))
    elif isinstance(o, list):
        for i, v in enumerate(o[:50]):
            walk(v, keys, depth+1, pathn+f'[{i}]')
walk(d)
