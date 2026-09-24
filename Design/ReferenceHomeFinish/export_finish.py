import sys
from pathlib import Path
sys.path.insert(0,str(Path.cwd()/'Design/ReferenceGameplayImport/Media/Tools'))
import export_particles as e
ids=[]
for k,o in e.objects.items():
 if o.type.name=='GameObject' and o.peek_name() in ('RemainingMovesParticles','RemainingMovesHitParticles'):
  ids.append(k);print(k,o.peek_name())
e.main(ids,'NativeFinishingParticles.json')
