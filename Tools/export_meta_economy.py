"""Export approved design data to Unity's field/array JSON format; --check detects drift."""
import json
import sys
from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = json.loads((root / 'docs/design-data/경제_설계_기준값.json').read_text(encoding='utf-8'))
assert source['status'] == 'user_approved'
ranks = ['N', 'R', 'SR', 'SSR', 'UR']
levels = []
level = 1
for rank in range(5):
    assert len(source['cardCosts'][rank]) == len(source['stoneCosts'][rank]) == source['rankUpgradeCounts'][rank]
    for copies, stones in zip(source['cardCosts'][rank], source['stoneCosts'][rank]):
        levels.append(dict(rank=rank, fromLevel=level, copies=copies, stones=stones))
        level += 1
keys = ['evolutionCosts', 'chests', 'heroUnlockShards', 'heroDuplicateShards',
        'ownedHeroShardStoneConversion', 'maxedCardStoneConversion', 'levelStatIncrement']
data = {key: source[key] for key in keys}
cards = []
for card, cap in source['caps'].items():
    race = 'Human' if card.startswith('H') else 'Orc' if card.startswith('O') else 'Shared'
    policy = 'duration' if card in ['rally', 'mining'] else 'health' if card in ['mine', 'fence'] else 'damage' if card == 'fire' else 'healthAndDamage'
    cards.append(dict(id=card, race=race, growthStatPolicy=policy, maxRank=ranks.index(cap['maxRank']), maxLevel=cap['maxLevel']))
data.update(version=source['version'], levels=levels, cards=cards,
            normalWeights=source['rarityWeights']['normal'], eliteWeights=source['rarityWeights']['elite'])
target = root / 'Assets/Resources/meta-economy.json'
if '--check' in sys.argv:
    assert json.loads(target.read_text(encoding='utf-8')) == data, 'Runtime economy differs from approved design. Run export_meta_economy.py.'
    print('APPROVED_ECONOMY_MATCHES_RUNTIME')
else:
    target.write_text(json.dumps(data, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
